using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Finances;

public class Account
{
    public int AccountId { get; set; }
    public Cryptocurrency Cryptocurrency { get; set; }
    public decimal AccountBalance { get; set; }
    public decimal Balance { get; set; }
}
