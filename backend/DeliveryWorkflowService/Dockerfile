# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["DeliveryWorkflowService.csproj", "./"]
RUN dotnet restore "DeliveryWorkflowService.csproj"

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish "DeliveryWorkflowService.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Application listens on port 8080 inside container
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DeliveryWorkflowService.dll"]
