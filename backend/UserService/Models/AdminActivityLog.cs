using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserService.Models;

[Table("AdminActivityLogs")]
public class AdminActivityLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    public Guid? PerformedByUserId { get; set; }

    [MaxLength(256)]
    public string PerformedByEmail { get; set; } = string.Empty;

    public Guid? TargetUserId { get; set; }

    [MaxLength(50)]
    public string ClientIp { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Details { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
