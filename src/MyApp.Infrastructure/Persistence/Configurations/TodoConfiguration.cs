using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.Todos;
using MyApp.Infrastructure.Identity;

namespace MyApp.Infrastructure.Persistence.Configurations;

public class TodoConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> b)
    {
        // Связь с пользователем описана здесь: Domain про Identity ничего не знает.
        b.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(t => t.UserId);
    }
}