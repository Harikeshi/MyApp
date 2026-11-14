namespace MyApp.Application.Common;

/// <summary>Запрошенная страница. Значения из запроса приводятся к допустимым через <see cref="Of"/>.</summary>
public readonly record struct PageRequest(int Page, int PageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxPage = 1_000_000;        // защита от переполнения в (Page - 1) * PageSize
    public int Skip => (Page - 1) * PageSize;
    public static PageRequest Of(int? page, int? pageSize) => new(
        Math.Clamp(page ?? 1, 1, MaxPage),
        Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));
}
