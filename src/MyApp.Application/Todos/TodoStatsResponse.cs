namespace MyApp.Application.Todos;

public record TodoStatsResponse(
    int Total,
    int Done,
    int Open,
    int Overdue,
    int HighPriorityOpen,
    int NormalPriorityOpen,
    int LowPriorityOpen)
{
    public static TodoStatsResponse Empty { get; } = new(0, 0, 0, 0, 0, 0, 0);
}
