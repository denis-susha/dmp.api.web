namespace DMP.BL.Models.Favorites;

public class Favorites
{
    public int FavoritesId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}
