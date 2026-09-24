using DMP.BL.Models.Enumerations;

namespace API.Web.Models.WsMessages;

public class WsPaymentUpdateMessage
{
    public PaymentStatus PaymentStatus { get; set; }
    public decimal SentAmount { get; set; }
    public int OrderId { get; set; }
}
