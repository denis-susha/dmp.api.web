namespace DMP.BL.Models.Mail;

public class FinishModeration
{
    public string LogoUrl { get; set; } = null!;
    public string Year { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string? MessageForSeller { get; set; }
    public string ProductLink { get; set; } = null!;
    public string UserName { get; set; } = null!;
}
