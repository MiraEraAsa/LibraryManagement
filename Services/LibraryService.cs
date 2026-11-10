using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Services;

// Здесь собраны методы из заданий 1–25.
public class LibraryService
{
    private readonly LibraryContext _db;
    public const decimal FinePerDay = 100m;
    public static int TotalBooks { get; private set; }

    public LibraryService(LibraryContext db)
    {
        _db = db;
    }

    // 1. DisplayInfo() — вывод информации о книге (данные возвращаются представлению).
    public string DisplayInfo(Book book) => $"{book.Title} — {book.Author}, {book.Year}";

    // 2. Метод без параметров.
    public string ShowWelcomeMessage() => "Добро пожаловать в библиотеку!";

    // 3. Метод с одним параметром.
    public async Task<bool> SetTitle(int id, string title)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null || string.IsNullOrWhiteSpace(title)) return false;
        book.Title = title.Trim();
        await _db.SaveChangesAsync();
        return true;
    }

    // 4. Метод с несколькими параметрами.
    public async Task AddBook(string title, string author, int year)
    {
        await _db.Books.AddAsync(new Book { Title = title.Trim(), Author = author.Trim(), Year = year });
        await _db.SaveChangesAsync();
        TotalBooks++;
    }

    // 5. Вызов метода для объектов Book выполняется при отображении списка.
    // 6. Количество книг.
    public Task<int> GetBookCount() => _db.Books.CountAsync();

    // 7. Проверка доступности.
    public async Task<bool> IsBookAvailable(string title)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Title == title);
        return book != null && !book.IsBorrowed;
    }

    // 8. Возраст книги.
    public int CalculateBookAge(int year) => Math.Max(0, DateTime.Now.Year - year);

    // 9. Поиск книги.
    public Task<Book?> FindBook(string title) =>
        _db.Books.FirstOrDefaultAsync(b => b.Title.ToLower() == title.Trim().ToLower());

    // 10. Проверка читателя.
    public Task<bool> IsReaderRegistered(string name) =>
        _db.Readers.AnyAsync(r => r.Name.ToLower() == name.Trim().ToLower()
            || (r.Surname != null && (r.Name + " " + r.Surname).ToLower() == name.Trim().ToLower()));

    // 11. Статический метод.
    public static string ShowLibraryName() => "Городская библиотека";

    // 12. Нестатический метод.
    public Task<List<Book>> ShowBooks() => _db.Books.OrderBy(b => b.Title).ToListAsync();

    // 13. Штраф за просрочку: 100 условных единиц за каждый день после 14 дней.
    public static decimal CalculateFine(int overdueDays) =>
        Math.Max(0, overdueDays) * FinePerDay;

    // 14. Статический и нестатический методы.
    public static int GetMaxBooks(int first, int second) => Math.Max(first, second);
    public async Task<int> GetAvailableBooks() => await _db.Books.CountAsync(b => !b.IsBorrowed);

    // 15. Статический счётчик обновляется после добавления книги; реальное общее число берётся из БД.
    public async Task<int> GetTotalBooks() => await _db.Books.CountAsync();

    // 16. Перегрузка SearchBook().
    public Task<List<Book>> SearchBook(string title) =>
        _db.Books.Where(b => b.Title.ToLower().Contains(title.Trim().ToLower())).ToListAsync();
    public Task<List<Book>> SearchBook(string author, bool byAuthor) =>
        _db.Books.Where(b => b.Author.ToLower().Contains(author.Trim().ToLower())).ToListAsync();
    public Task<List<Book>> SearchBook(int year) =>
        _db.Books.Where(b => b.Year == year).ToListAsync();

    // 17. Перегрузка AddReader().
    public Task AddReader(string name) => AddReaderCore(name, null, null);
    public Task AddReader(string name, string surname) => AddReaderCore(name, surname, null);
    public Task AddReader(string name, string phone, bool byPhone) => AddReaderCore(name, null, phone);

    private async Task AddReaderCore(string name, string? surname, string? phone)
    {
        await _db.Readers.AddAsync(new Reader { Name = name.Trim(), Surname = surname?.Trim(), Phone = phone?.Trim() });
        await _db.SaveChangesAsync();
    }

    // 18. Возврат книги с проверкой статуса.
    public async Task<bool> ReturnBook(string title)
    {
        var book = await FindBook(title);
        if (book == null || !book.IsBorrowed) return false;
        book.IsBorrowed = false;
        book.BorrowedBy = null;
        book.BorrowedAt = null;
        await _db.SaveChangesAsync();
        return true;
    }

    // 19. Рекурсивный подсчёт списка.
    public int CountBooksRecursive(IReadOnlyList<Book> books, int index = 0)
    {
        if (index >= books.Count) return 0;
        return 1 + CountBooksRecursive(books, index + 1);
    }

    // 20. Сортировка по году.
    public Task<List<Book>> SortBooksByYear() => _db.Books.OrderBy(b => b.Year).ToListAsync();

    // 21. Выдача книги читателю.
    public async Task<(bool Success, string Message)> BorrowBook(string title, string readerName)
    {
        var book = await FindBook(title);
        if (book == null) return (false, "Книга не найдена.");
        if (book.IsBorrowed) return (false, "Эта книга уже выдана.");
        if (!await IsReaderRegistered(readerName)) return (false, "Читатель не зарегистрирован.");
        book.IsBorrowed = true;
        book.BorrowedBy = readerName.Trim();
        book.BorrowedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return (true, "Книга успешно выдана.");
    }

    // 22. Возврат с расчётом штрафа: бесплатный срок — 14 дней.
    public async Task<(bool Success, decimal Fine, string Message)> ReturnBookWithFine(string title)
    {
        var book = await FindBook(title);
        if (book == null) return (false, 0, "Книга не найдена.");
        if (!book.IsBorrowed || book.BorrowedAt == null) return (false, 0, "Книга не была выдана.");
        int days = (DateTime.Now - book.BorrowedAt.Value).Days;
        decimal fine = CalculateFine(Math.Max(0, days - 14));
        book.IsBorrowed = false;
        book.BorrowedBy = null;
        book.BorrowedAt = null;
        await _db.SaveChangesAsync();
        return (true, fine, $"Книга возвращена. Штраф: {fine}.");
    }

    // 23. Статистика.
    public async Task<(int Total, int Available, int Borrowed, int Readers)> ShowStatistics()
    {
        int total = await _db.Books.CountAsync();
        int borrowed = await _db.Books.CountAsync(b => b.IsBorrowed);
        int readers = await _db.Readers.CountAsync();
        return (total, total - borrowed, borrowed, readers);
    }
}
