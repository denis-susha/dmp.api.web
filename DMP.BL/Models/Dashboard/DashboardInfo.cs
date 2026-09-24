namespace DMP.BL.Models.Dashboard;

public class DashboardInfo
{
    public decimal LastDaySalesAmount { get; set; }
    public int LastDaySalesCount { get; set; }
    public decimal TotalSalesAmount { get; set; }

    public ICollection<TopSellProduct>? TopSellProducts { get; set; }
    public ICollection<DateSaleStat>? DateSaleStats { get; set; }
}
