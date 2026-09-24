namespace DMP.BL.Models;

public class UserFeature
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Dictionary<string, string>? Features { get; set; }
}
