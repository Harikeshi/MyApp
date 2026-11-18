using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyApp.Application.Abstractions;
using MyApp.Domain.Todos;
using MyApp.Infrastructure.Identity;
using MyApp.Domain.News;

namespace MyApp.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IUnitOfWork
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();
    public DbSet<NewsItem> News => Set<NewsItem>();
    public DbSet<NewsComment> NewsComments => Set<NewsComment>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
