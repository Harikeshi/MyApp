using System.Security.Claims;
using MyApp.Application.Abstractions;

namespace MyApp.Web.Services;

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string Id =>
        accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Нет текущего пользователя");
}