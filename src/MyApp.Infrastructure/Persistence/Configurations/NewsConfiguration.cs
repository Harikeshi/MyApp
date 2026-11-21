using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.News;
using MyApp.Infrastructure.Identity;

namespace MyApp.Infrastructure.Persistence.Configurations;

public class NewsSourceConfiguration : IEntityTypeConfiguration<NewsSource>
{
    public void Configure(EntityTypeBuilder<NewsSource> b)
    {
        b.Property(x => x.Name).HasMaxLength(100);
        b.Property(x => x.FeedUrl).HasMaxLength(1000);
        b.Property(x => x.Topic).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ETag).HasMaxLength(300);
        b.Property(x => x.LastError).HasMaxLength(500);

        b.HasIndex(x => x.FeedUrl).IsUnique();
    }
}

public class NewsConfiguration : IEntityTypeConfiguration<NewsItem>
{
    public void Configure(EntityTypeBuilder<NewsItem> b)
    {
        b.Property(x => x.ExternalId).HasMaxLength(450);
        b.Property(x => x.Title).HasMaxLength(500);
        b.Property(x => x.Url).HasMaxLength(2000);
        b.Property(x => x.Summary).HasMaxLength(5000);
        b.Property(x => x.ImageUrl).HasMaxLength(2000);
        b.Property(x => x.Topic).HasConversion<string>().HasMaxLength(20);

        b.HasOne(x => x.Source).WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.SourceId, x.ExternalId }).IsUnique();       // защита от дублей
        b.HasIndex(x => new { x.PublishedAt, x.Id }).IsDescending(true, true);               // лента «Все»
        b.HasIndex(x => new { x.Topic, x.PublishedAt, x.Id }).IsDescending(false, true, true); // лента по теме
    }
}

public class NewsCommentConfiguration : IEntityTypeConfiguration<NewsComment>
{
    public void Configure(EntityTypeBuilder<NewsComment> b)
    {
        b.Property(x => x.Text).HasMaxLength(2000);

        b.HasOne<NewsItem>().WithMany()
            .HasForeignKey(x => x.NewsItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // При удалении аккаунта комментарии удаляются вместе с ним.
        b.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.NewsItemId, x.CreatedAt });
    }
}

public class NewsCommentReportConfiguration : IEntityTypeConfiguration<NewsCommentReport>
{
    public void Configure(EntityTypeBuilder<NewsCommentReport> b)
    {
        b.HasOne<NewsComment>().WithMany()
            .HasForeignKey(x => x.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.CommentId, x.UserId }).IsUnique();   // одна жалоба от пользователя
    }
}

public class NewsTranslationConfiguration : IEntityTypeConfiguration<NewsTranslation>
{
    public void Configure(EntityTypeBuilder<NewsTranslation> b)
    {
        b.Property(x => x.Lang).HasMaxLength(8);
        b.Property(x => x.Title).HasMaxLength(1000);
        b.Property(x => x.Summary).HasMaxLength(5000);
        b.Property(x => x.Model).HasMaxLength(100);

        b.HasOne<NewsItem>().WithMany()
            .HasForeignKey(x => x.NewsItemId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.NewsItemId, x.Lang }).IsUnique();   // один перевод на язык
    }
}
