namespace DMP.BL.Models.User;

public class UserWelcomeInfo
{
    public bool EmailIsConfirmed { get; set; }
    public bool StoreIsConfigured { get; set; }
    public bool HasProduct { get; set; }
    public int DoneStepsCount { get; set; }
}
