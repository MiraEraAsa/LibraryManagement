using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class Book
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = "";

    [Required, StringLength(100)]
    public string Author { get; set; } = "";

    [Range(1000, 2100)]
    public int Year { get; set; }

    [StringLength(2000)]
    public string Description { get; set; } = "";

    public string? CoverImageUrl { get; set; }

    [Url]
    public string? YouTubeUrl { get; set; }

    public bool IsBorrowed { get; set; }

    public string? BorrowedBy { get; set; }

    public DateTime? BorrowedAt { get; set; }
}
