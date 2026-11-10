using LibraryManagement.Data;
using LibraryManagement.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("LibraryConnection")));
builder.Services.AddScoped<LibraryService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    db.Database.EnsureCreated();

    // Добавляем новые поля в уже существующую SQLite-базу без удаления старых данных.
    await EnsureColumn(db, "Books", "Description", "TEXT NOT NULL DEFAULT ''");
    await EnsureColumn(db, "Books", "CoverImageUrl", "TEXT NULL");
    await EnsureColumn(db, "Books", "YouTubeUrl", "TEXT NULL");
    await EnsureColumn(db, "Readers", "BirthDate", "TEXT NULL");
    await EnsureColumn(db, "Readers", "InstitutionType", "TEXT NULL");
    await EnsureColumn(db, "Readers", "Institution", "TEXT NULL");
    await EnsureColumn(db, "Readers", "IIN", "TEXT NULL");
    await EnsureColumn(db, "Readers", "Email", "TEXT NULL");
    await EnsureColumn(db, "Readers", "Gender", "TEXT NULL");
}

static async Task EnsureColumn(LibraryContext db, string table, string column, string definition)
{
    var connection = db.Database.GetDbConnection();
    bool wasClosed = connection.State == System.Data.ConnectionState.Closed;
    if (wasClosed) await connection.OpenAsync();

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info('{table}')";
        await using var reader = await command.ExecuteReaderAsync();
        bool exists = false;
        while (await reader.ReadAsync())
        {
            if (reader.GetString(1).Equals(column, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }
        await reader.CloseAsync();

        if (!exists)
            await db.Database.ExecuteSqlRawAsync($"ALTER TABLE {table} ADD COLUMN {column} {definition}");
    }
    finally
    {
        if (wasClosed) await connection.CloseAsync();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Library}/{action=Index}/{id?}");

app.Run();
