using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models;

public class EmailConfirmationDAL
{
    public long EmailConfirmationId { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = null!;
    public bool Used { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public EmailConfirmationType Type { get; set; }
}
