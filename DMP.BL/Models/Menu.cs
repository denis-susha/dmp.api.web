namespace DMP.BL.Models;

public class Menu
{
    public short MenuId { get; set; }

    public IEnumerable<MenuCategory>? Categories { get; set; }
}
