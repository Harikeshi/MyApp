namespace MyApp.Application.Accounts;

public static class DisplayNameRules
{
    public const int MinLength = 2;
    public const int MaxLength = 30;
    public const string ErrorMessage = "Имя должно содержать от 2 до 30 символов";

    /// <summary>Допустимые символы: буква или цифра в начале, дальше ещё пробел и . _ -</summary>
    public const string Pattern = @"^\s*[\p{L}\p{N}][\p{L}\p{N} ._-]*\s*$";
    public const string PatternMessage = "В имени допустимы буквы, цифры, пробел и символы . _ -";

    public static bool TryNormalize(string? raw, out string name)
    {
        name = raw?.Trim() ?? "";
        return name.Length is >= MinLength and <= MaxLength;
    }
}
