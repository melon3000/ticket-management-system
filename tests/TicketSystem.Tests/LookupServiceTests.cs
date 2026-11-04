using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Data;
using TicketSystem.Api.Repositories;
using TicketSystem.Api.Services;

namespace TicketSystem.Tests;

public class LookupServiceTests
{
    private static (SqliteConnection Connection, LookupService Service) CreateService()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        SeedData.Initialize(db);
        return (connection, new LookupService(new CategoryRepository(db), new PriorityRepository(db)));
    }

    [Fact]
    public async Task GetCategories_ReturnsSeededCategoriesSortedByName()
    {
        var (connection, service) = CreateService();
        using var _ = connection;

        var categories = await service.GetCategoriesAsync();

        Assert.Equal(5, categories.Count);
        Assert.Equal(categories.Select(c => c.Name).OrderBy(n => n, StringComparer.Ordinal).ToList(),
            categories.Select(c => c.Name).ToList());
    }

    [Fact]
    public async Task GetPriorities_ReturnsPrioritiesFromLowestToHighestLevel()
    {
        var (connection, service) = CreateService();
        using var _ = connection;

        var priorities = await service.GetPrioritiesAsync();

        Assert.Equal(new[] { 1, 2, 3, 4 }, priorities.Select(p => p.Level).ToArray());
        Assert.Equal(72, priorities[0].DeadlineHours);
    }
}
