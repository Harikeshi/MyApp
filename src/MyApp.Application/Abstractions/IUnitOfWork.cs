namespace MyApp.Application.Abstractions;

/// <summary>Фиксация изменений. Реализует DbContext в Infrastructure; Application про EF Core не знает.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
