using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Finances;

public class AccountBalance
{
    public Cryptocurrency Cryptocurrency { get; set; }
    public decimal Balance { get; set; }
}
