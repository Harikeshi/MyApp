using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.Todos;
using MyApp.Infrastructure.Identity;

namespace MyApp.Infrastructure.Persistence.Configurations;

public class TodoConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> b)
    {
        // Связи с пользователем и проектом описаны здесь: Domain про Identity ничего не знает.
        b.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // удалили пользователя-исполнителя: задача остаётся, но без исполнителя
        b.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        // удалили проект: его задачи удаляются вместе с ним
        b.HasOne<Project>().WithMany()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(t => t.UserId);
        b.HasIndex(t => t.AssigneeId);
        b.HasIndex(t => t.ProjectId);
    }
}
