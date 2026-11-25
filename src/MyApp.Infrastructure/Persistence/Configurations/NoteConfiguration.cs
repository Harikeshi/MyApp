using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.Notes;
using MyApp.Infrastructure.Identity;

namespace MyApp.Infrastructure.Persistence.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> b)
    {
        b.Property(n => n.Title).HasMaxLength(Note.MaxTitleLength);
        b.Property(n => n.Tag).HasConversion<string>().HasMaxLength(10);   // "Task" / "Faq"

        b.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(n => new { n.UserId, n.UpdatedAt });
    }
}
