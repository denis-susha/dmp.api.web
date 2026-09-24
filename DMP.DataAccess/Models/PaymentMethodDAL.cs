namespace DMP.DataAccess.Models;

public class PaymentMethodDAL
{
    public int PaymentMethodId { get; set; }
    public int OrderId { get; set; }
    public string BcPaymentMethodId { get; set; } = null!;
    public Guid UserId { get; set; }
}
