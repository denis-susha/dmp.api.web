namespace DMP.BL.Models.Favorites;

public class FavoritesList
{
    public int TotalCount { get; set; }
    public List<Favorites> Favorites { get; set; } = null!;
}
