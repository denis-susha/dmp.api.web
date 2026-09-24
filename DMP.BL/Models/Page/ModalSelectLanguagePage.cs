namespace DMP.BL.Models.Page;

public class ModalSelectLanguagePage
{
    public string Header { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string ButtonText { get; set; } = null!;
    public List<LanguageOption> LanguageOptions { get; set; } = null!;
}

public class LanguageOption
{
    public string Code { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
}
