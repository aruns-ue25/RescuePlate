using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RequestWorkflowService.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration (PostgreSQL - RescuePlateDB via Npgsql EF Core)
var connectionString = builder.Configuration.GetConnectionString("PostgreSQLConnection")
    ?? "Host=localhost;Port=5432;Database=RescuePlateDB;Username=postgres;Password=postgres";

builder.Services.AddDbContext<RequestDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// 2. Standard ASP.NET Core Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<RequestDbContext>(
        name: "PostgreSQL",
        tags: new[] { "db", "data" });

// 3. JWT Authentication & Authorization Configuration
var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "RescuePlate_Super_Secret_Key_For_Jwt_Authentication_2026_Sprint1_RescueFood";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "RescuePlate.UserService";
var audience = builder.Configuration["Jwt:Audience"] ?? "RescuePlate.Client";
var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:5173";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DonorOnly", policy => policy.RequireRole("DONOR"));
    options.AddPolicy("OrganizationOnly", policy => policy.RequireRole("ORGANIZATION"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
});

// 4. CORS Policy for Frontend Integration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendUrl, "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 5. Inter-Service Communication Setup (UserService & DonationService HttpClients)
builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:UserServiceUrl"] ?? "http://localhost:5000");
});
builder.Services.AddHttpClient("DonationService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:DonationServiceUrl"] ?? "http://localhost:5001");
});

// 6. Controllers & JSON Serializer Options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// 7. Swagger / OpenAPI Documentation Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RescuePlate Request Workflow Service API",
        Version = "v1",
        Description = "ASP.NET Core 10 Microservice for managing food redistribution requests and workflows (Sprint 3)"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 8. Automatic Database Initialization & Schema Check (RescuePlate Microservice Convention)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RequestDbContext>();
    try
    {
        dbContext.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""FoodRequests"" (
                ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                ""DonationId"" integer NOT NULL,
                ""DonationTitle"" character varying(200) NOT NULL DEFAULT '',
                ""OrganizationId"" character varying(100) NOT NULL,
                ""OrganizationName"" character varying(150) NOT NULL,
                ""DonorId"" character varying(100) NOT NULL,
                ""RequestedQuantity"" integer NOT NULL,
                ""AcceptedQuantity"" integer,
                ""Unit"" character varying(50) NOT NULL DEFAULT 'portions',
                ""Status"" character varying(50) NOT NULL,
                ""Notes"" character varying(500),
                ""RejectionReason"" character varying(500),
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""UpdatedAt"" timestamp with time zone
            );
            ALTER TABLE ""FoodRequests"" ADD COLUMN IF NOT EXISTS ""DonationTitle"" character varying(200) NOT NULL DEFAULT '';
            ALTER TABLE ""FoodRequests"" ADD COLUMN IF NOT EXISTS ""AcceptedQuantity"" integer;
            ALTER TABLE ""FoodRequests"" ADD COLUMN IF NOT EXISTS ""Unit"" character varying(50) NOT NULL DEFAULT 'portions';
            ALTER TABLE ""FoodRequests"" ADD COLUMN IF NOT EXISTS ""RejectionReason"" character varying(500);

            CREATE INDEX IF NOT EXISTS ""IX_FoodRequests_DonationId"" ON ""FoodRequests"" (""DonationId"");
            CREATE INDEX IF NOT EXISTS ""IX_FoodRequests_OrganizationId"" ON ""FoodRequests"" (""OrganizationId"");
            CREATE INDEX IF NOT EXISTS ""IX_FoodRequests_DonorId"" ON ""FoodRequests"" (""DonorId"");
            CREATE INDEX IF NOT EXISTS ""IX_FoodRequests_Status"" ON ""FoodRequests"" (""Status"");
        ");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not initialize FoodRequests table schema");
    }
}

// 9. Middleware Pipeline Setup
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RescuePlate Request Workflow Service API v1");
    });
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Standard ASP.NET Core Health Checks Endpoint
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();

public partial class Program { }
