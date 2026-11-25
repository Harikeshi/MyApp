using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.Todos;
using MyApp.Infrastructure.Identity;

namespace MyApp.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> b)
    {
        b.Property(p => p.Name).HasMaxLength(Project.MaxNameLength);

        b.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(p => p.Members).WithOne()
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(p => p.Members).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasIndex(p => p.OwnerId);
    }
}

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> b)
    {
        b.HasKey(m => new { m.ProjectId, m.UserId });

        b.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(m => m.UserId);              // «в каких проектах я ответственный»
    }
}
