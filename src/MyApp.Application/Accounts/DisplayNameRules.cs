namespace MyApp.Application.Accounts;

public static class DisplayNameRules
{
    public const int MinLength = 2;
    public const int MaxLength = 30;
    public const string ErrorMessage = "Имя должно содержать от 2 до 30 символов";

    public static bool TryNormalize(string? raw, out string name)
    {
        name = raw?.Trim() ?? "";
        return name.Length is >= MinLength and <= MaxLength;
    }
}
