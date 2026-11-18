using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.News;
using MyApp.Infrastructure.Identity;

namespace MyApp.Infrastructure.Persistence.Configurations;

public class NewsConfiguration : IEntityTypeConfiguration<NewsItem>
{
    public void Configure(EntityTypeBuilder<NewsItem> b)
    {
        b.HasIndex(x => new { x.SourceId, x.ExternalId }).IsUnique();
        b.HasIndex(x => new { x.PublishedAt, x.Id });
    }
}

public class NewsCommentConfiguration : IEntityTypeConfiguration<NewsComment>
{
    public void Configure(EntityTypeBuilder<NewsComment> b)
    {
        b.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        b.HasIndex(x => new { x.NewsItemId, x.CreatedAt });
    }
}
