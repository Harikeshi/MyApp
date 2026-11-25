using Microsoft.EntityFrameworkCore;
using MyApp.Application.Abstractions;
using MyApp.Infrastructure.Persistence;

namespace MyApp.Infrastructure.Identity;

public class UserDirectory(AppDbContext db) : IUserDirectory
{
    private const string NoName = "Пользователь";

    public async Task<IReadOnlyDictionary<string, string>> GetNamesAsync(
        IReadOnlyCollection<string> ids, CancellationToken ct)
    {
        var rows = await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync(ct);

        return rows.ToDictionary(
            u => u.Id,
            u => string.IsNullOrWhiteSpace(u.DisplayName) ? NoName : u.DisplayName);
    }

    public async Task<IReadOnlyList<UserInfo>> SearchAsync(string query, int take, CancellationToken ct)
    {
        var q = query.ToLowerInvariant();
        var email = query.ToUpperInvariant();          // Identity хранит NormalizedEmail в верхнем регистре

        var rows = await db.Users.AsNoTracking()
            .Where(u => u.DisplayName.ToLower().Contains(q) || u.NormalizedEmail == email)
            .OrderBy(u => u.DisplayName)
            .Take(take)
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync(ct);

        return rows
            .Select(u => new UserInfo(u.Id, string.IsNullOrWhiteSpace(u.DisplayName) ? NoName : u.DisplayName))
            .ToList();
    }

    public async Task<IReadOnlySet<string>> ExistingIdsAsync(IReadOnlyCollection<string> ids, CancellationToken ct) =>
        (await db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).Select(u => u.Id).ToListAsync(ct)).ToHashSet();
}
