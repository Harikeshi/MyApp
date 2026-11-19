namespace MyApp.Infrastructure.News;

public class NewsOptions
{
    public TimeSpan Interval { get; set; } = new TimeSpan(0, 10, 10);
}