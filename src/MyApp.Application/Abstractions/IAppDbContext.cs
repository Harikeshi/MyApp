using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Todos;

namespace MyApp.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<TodoItem> Todos { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
