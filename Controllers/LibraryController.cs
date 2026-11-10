using LibraryManagement.Models;
using LibraryManagement.Services;
using LibraryManagement.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LibraryManagement.Controllers;

public class LibraryController : Controller
{
    private readonly LibraryService _library;
    private readonly LibraryContext _db;

    public LibraryController(LibraryService library, LibraryContext db)
    {
        _library = library;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Welcome = _library.ShowWelcomeMessage();
        ViewBag.LibraryName = LibraryService.ShowLibraryName();
        return View(await _library.ShowBooks());
    }

    public async Task<IActionResult> Readers()
    {
        return View(await _db.Readers.OrderBy(r => r.Name).ThenBy(r => r.Surname).ToListAsync());
    }

    public async Task<IActionResult> ReaderProfile(int id)
    {
        var reader = await _db.Readers.FindAsync(id);
        if (reader == null) return NotFound();
        return View(reader);
    }

    [HttpGet]
    public IActionResult AddBook() => View(new Book { Year = DateTime.Now.Year });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBook(Book book, IFormFile? coverImage)
    {
        if (coverImage != null && coverImage.Length > 5 * 1024 * 1024)
            ModelState.AddModelError("CoverImageUrl", "Размер изображения не должен превышать 5 МБ.");

        if (!ModelState.IsValid) return View(book);

        if (coverImage != null && coverImage.Length > 0)
        {
            var extension = Path.GetExtension(coverImage.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(extension))
            {
                ModelState.AddModelError("CoverImageUrl", "Разрешены только JPG, PNG и WEBP.");
                return View(book);
            }

            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "covers");
            Directory.CreateDirectory(uploadFolder);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadFolder, fileName);
            await using (var stream = System.IO.File.Create(filePath))
                await coverImage.CopyToAsync(stream);

            book.CoverImageUrl = $"/uploads/covers/{fileName}";
        }

        await _library.AddBook(book.Title, book.Author, book.Year);
        var savedBook = await _db.Books.OrderByDescending(b => b.Id).FirstAsync();
        savedBook.Description = book.Description ?? "";
        savedBook.CoverImageUrl = book.CoverImageUrl;
        savedBook.YouTubeUrl = book.YouTubeUrl;
        await _db.SaveChangesAsync();

        TempData["Message"] = "Книга успешно добавлена!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null) return NotFound();

        ViewBag.YouTubeEmbedUrl = GetYouTubeEmbedUrl(book.YouTubeUrl);
        return View(book);
    }

    private static string? GetYouTubeEmbedUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var match = Regex.Match(url,
            @"(?:youtu\.be/|youtube\.com/(?:watch\?v=|embed/|shorts/))([A-Za-z0-9_-]{11})",
            RegexOptions.IgnoreCase);
        return match.Success ? $"https://www.youtube-nocookie.com/embed/{match.Groups[1].Value}" : null;
    }

    [HttpGet]
    public IActionResult AddReader() => View(new Reader());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReader(Reader reader)
    {
        if (!ModelState.IsValid) return View(reader);

        reader.RegisteredAt = DateTime.Now;
        await _db.Readers.AddAsync(reader);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Личный кабинет читателя создан!";
        return RedirectToAction(nameof(ReaderProfile), new { id = reader.Id });
    }

    public async Task<IActionResult> Search(string? title, string? author, int? year)
    {
        List<Book> result;
        if (!string.IsNullOrWhiteSpace(author)) result = await _library.SearchBook(author, true);
        else if (year.HasValue) result = await _library.SearchBook(year.Value);
        else if (!string.IsNullOrWhiteSpace(title)) result = await _library.SearchBook(title);
        else result = new List<Book>();
        return View("Index", result);
    }

    [HttpGet]
    public IActionResult Borrow() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrow(string title, string readerName)
    {
        var result = await _library.BorrowBook(title, readerName);
        TempData["Message"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Return() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(string title)
    {
        var result = await _library.ReturnBookWithFine(title);
        TempData["Message"] = result.Message;
        if (result.Success) TempData["Fine"] = $"Есептелген штраф: {result.Fine}";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Statistics()
    {
        var stats = await _library.ShowStatistics();
        return View(stats);
    }
}
