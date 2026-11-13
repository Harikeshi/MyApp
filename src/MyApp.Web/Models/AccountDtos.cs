using System.ComponentModel.DataAnnotations;

namespace MyApp.Web.Models;

public record RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = "";

    [Required]
    public string Password { get; init; } = "";

    [Required(ErrorMessage = "Введите имя пользователя")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Имя должно содержать от 2 до 30 символов")]
    [RegularExpression(@"^[\p{L}\p{N}][\p{L}\p{N} ._-]*$",
        ErrorMessage = "В имени допустимы буквы, цифры, пробел и символы . _ -")]
    public string DisplayName { get; init; } = "";
}

public record ForgotRequest(string Email);
