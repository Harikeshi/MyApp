using Microsoft.AspNetCore.Identity;

namespace MyApp.Web.Data;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
}