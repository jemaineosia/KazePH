using KazePH.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KazePH.Tests.Helpers;

/// <summary>
/// Creates isolated in-memory <see cref="KazeDbContext"/> instances for unit tests.
/// Each call uses a unique database name so tests never share state.
/// </summary>
public static class DbContextHelper
{
    public static KazeDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<KazeDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new KazeDbContext(options);
    }
}
