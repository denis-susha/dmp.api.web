namespace DMP.BL.Models.User;

public class UserSettings
{
    public UserProfileSettings UserProfileSettings { get; set; } = null!;
    public string Email { get; set; } = null!;
}
