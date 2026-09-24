namespace DMP.BL.Helpers;

public static class EmailHelper
{
    /// <summary>
    /// Emails are stored and compared in lower case. Lookups still lower the stored value as well,
    /// because accounts registered before normalization may contain upper-case letters.
    /// </summary>
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
