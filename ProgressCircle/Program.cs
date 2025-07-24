using Microsoft.EntityFrameworkCore;
using ProgressCircle.DataAccess;
using ProgressCircle.Models;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=Data/progress.db"));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

// GET /diagrams[?name=optional]
app.MapGet("/diagrams", async (AppDbContext db, string? name) =>
{
    try
    {
        var query = db.Diagrams.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(d => d.Name.ToLower().Contains(name.ToLower()));

        var results = await query
            .Select(d => new
            {
                d.Name,
                d.Description,
                d.TargetMinutes,
                d.AccumulatedMinutes,
                d.LastUpdatedUtc
            })
            .ToListAsync();

        return Results.Ok(results);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Unexpected error: {ex.Message}");
    }
});

// POST /diagrams
app.MapPost("/diagrams", async (AppDbContext db, Diagram input) =>
{
    try
    {
        // Simple validation
        if (string.IsNullOrWhiteSpace(input.Name) ||
            string.IsNullOrWhiteSpace(input.Description) ||
            input.TargetMinutes <= 0)
        {
            return Results.BadRequest("Name, Description and TargetMinutes are required, and TargetMinutes must be greater than 0.");
        }

        // Simple AccessKey generation
        string GenerateAccessKey(int length = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        return Convert.ToBase64String(bytes);
    }

    var diagram = new Diagram
    {
        Name = input.Name,
        Description = input.Description,
        TargetMinutes = input.TargetMinutes,
        AccumulatedMinutes = 0,
        LastUpdatedUtc = DateTime.UtcNow,
        AccessKey = GenerateAccessKey()
    };

    db.Diagrams.Add(diagram);
    await db.SaveChangesAsync();

    return Results.Created($"/diagrams", new
    {
        diagram.Name,
        diagram.Description,
        diagram.TargetMinutes,
        diagram.AccumulatedMinutes,
        diagram.LastUpdatedUtc,
        AccessKey = diagram.AccessKey
    });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Internal server error: {ex.Message}");
    }
});

// PUT /diagrams
app.MapPut("/diagrams", async (HttpContext context, AppDbContext db) =>
{
    try
    {
        var request = await context.Request.ReadFromJsonAsync<Diagram>();
        if (request == null)
            return Results.BadRequest("Invalid JSON");

        if(string.IsNullOrWhiteSpace(request.AccessKey))
            return Results.BadRequest("Need AccessKey");

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Name is required");

        if (request.AccumulatedMinutes == 0)
            return Results.BadRequest("AccumulatedMinutes must be non-zero");

        var diagram = await db.Diagrams.FirstOrDefaultAsync(d =>
            d.Name.ToLower() == request.Name.ToLower() && // string.Equals(d.Name, request.Name, StringComparison.OrdinalIgnoreCase)
            d.AccessKey == request.AccessKey);

        if (diagram == null)
            return Results.NotFound("Diagram not found or invalid access key");

        var newMinutes = diagram.AccumulatedMinutes + request.AccumulatedMinutes;
        if (newMinutes < 0)
            return Results.BadRequest("AccumulatedMinutes cannot be negative");

        diagram.AccumulatedMinutes = newMinutes;
        diagram.LastUpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            diagram.Name,
            diagram.AccumulatedMinutes,
            diagram.LastUpdatedUtc
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Internal server error: {ex.Message}");
    }
});

// DELETE /diagrams
app.MapDelete("/diagrams", async (HttpContext context, AppDbContext db) =>
{
    try
    {
        var request = await context.Request.ReadFromJsonAsync<Diagram>();
        if (request == null)
            return Results.BadRequest("Invalid JSON");

        if (string.IsNullOrWhiteSpace(request.AccessKey))
            return Results.BadRequest("Need AccessKey");

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Name is required");

        var diagram = await db.Diagrams.FirstOrDefaultAsync(d =>
            d.Name.ToLower() == request.Name.ToLower() &&
            d.AccessKey == request.AccessKey);

        if (diagram == null)
            return Results.NotFound("Diagram not found or invalid access key");

        db.Diagrams.Remove(diagram);
        await db.SaveChangesAsync();

        return Results.Ok("Diagram deleted successfully");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Internal server error: {ex.Message}");
    }
});

app.Run();
