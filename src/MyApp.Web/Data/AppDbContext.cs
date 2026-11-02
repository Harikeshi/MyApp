using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;
using MyApp.Web.Models;

namespace MyApp.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);   // обязательно: настраивает таблицы Identity

        builder.Entity<TodoItem>(e =>
        {
            e.HasOne<ApplicationUser>().WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);   // удалили пользователя, удалились его задачи
            e.HasIndex(t => t.UserId);
        });
    }
}