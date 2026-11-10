using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class Reader
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Name { get; set; } = "";

    [StringLength(60)]
    public string? Surname { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    [StringLength(20)]
    public string? InstitutionType { get; set; }

    [StringLength(150)]
    public string? Institution { get; set; }

    // Учебное поле: используйте только тестовый номер, не реальные ИИН.
    [RegularExpression(@"^$|^\d{12}$", ErrorMessage = "Введите 12 цифр или оставьте поле пустым.")]
    public string? IIN { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.Now;

    public string FullName => string.Join(" ", new[] { Name, Surname }.Where(x => !string.IsNullOrWhiteSpace(x)));
}
