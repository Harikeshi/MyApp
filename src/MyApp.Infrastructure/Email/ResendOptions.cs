using System.ComponentModel.DataAnnotations;

namespace MyApp.Infrastructure.Email;

public class ResendOptions
{
    [Required]
    public string ApiKey { get; set; } = "";

    [Required, EmailAddress]
    public string FromEmail { get; set; } = "";

    public string FromName { get; set; } = "";
}
