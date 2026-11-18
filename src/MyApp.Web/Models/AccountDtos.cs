using System.ComponentModel.DataAnnotations;
using MyApp.Application.Accounts;

namespace MyApp.Web.Models;

public record RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = "";

    [Required]
    public string Password { get; init; } = "";

    [Required(ErrorMessage = "Введите имя пользователя")]
    [StringLength(DisplayNameRules.MaxLength, MinimumLength = DisplayNameRules.MinLength, ErrorMessage = DisplayNameRules.ErrorMessage)]
    [RegularExpression(DisplayNameRules.Pattern, ErrorMessage = DisplayNameRules.PatternMessage)]
    public string DisplayName { get; init; } = "";
}

public record UpdateNameRequest
{
    [Required(ErrorMessage = "Введите имя пользователя")]
    [StringLength(DisplayNameRules.MaxLength, MinimumLength = DisplayNameRules.MinLength, ErrorMessage = DisplayNameRules.ErrorMessage)]
    [RegularExpression(DisplayNameRules.Pattern, ErrorMessage = DisplayNameRules.PatternMessage)]
    public string DisplayName { get; init; } = "";
}

public record DeleteAccountRequest
{
    [Required(ErrorMessage = "Введите пароль")]
    public string Password { get; init; } = "";
}

public record ForgotRequest(string Email);
