using System.ComponentModel.DataAnnotations;
using MyApp.Domain.Notes;

namespace MyApp.Web.Models;

public record NoteRequest
{
    [Required, StringLength(Note.MaxTitleLength, MinimumLength = 1)]
    public string Title { get; init; } = "";

    [EnumDataType(typeof(NoteTag))]
    public NoteTag Tag { get; init; } = NoteTag.Task;

    /// <summary>HTML записи (текст с цветом, картинки data:-ссылками).</summary>
    [StringLength(Note.MaxContentLength)]
    public string Content { get; init; } = "";
}
