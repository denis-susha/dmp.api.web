using DMP.BL.Models.Enumerations;

namespace DMP.BL.Models.WorkerNotification;

public class PaymentUpdateRequest
{
    public Guid UserId { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal SentAmount { get; set; }
    public int OrderId { get; set; }
}
