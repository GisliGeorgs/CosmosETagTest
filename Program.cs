using CosmosEtag;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Hello, World!");

var services = new ServiceCollection();

services.AddCosmos<Db>(
    "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "ETagThing");

var serviceProvider = services.BuildServiceProvider();

Console.WriteLine("Setting up");

using (var scope = serviceProvider.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Db>();
    await db.Database.EnsureDeletedAsync().ConfigureAwait(false);
    await db.Database.EnsureCreatedAsync().ConfigureAwait(false);

    db.UseEtagEntities.Add(new()
    {
        Id = 1,
        PartitionKey = "Use",
        PropertyToChange = "Unmodified"
    });

    db.IsETagEntities.Add(new()
    {
        Id = 2,
        PartitionKey = "Is",
        PropertyToChange = "Unmodified"
    });

    db.NoETagEntities.Add(new()
    {
        Id = 3,
        PartitionKey = "No",
        PropertyToChange = "Unmodified"
    });

    await db.SaveChangesAsync().ConfigureAwait(false);
}

Console.WriteLine("Set up, starting");

// On a SaveChangesAsync breakpoint, edit the relevant entity
using (var scope = serviceProvider.CreateScope())
{
    Console.WriteLine("UseETag - should throw, but doesn't");
    var scopeDb = scope.ServiceProvider.GetRequiredService<Db>();

    var useEntity = await scopeDb.UseEtagEntities
        .WithPartitionKey("Use")
        .FirstAsync(x => x.Id == 1)
        .ConfigureAwait(false);

    useEntity.PropertyToChange = "Modified in scope";

    await ModifyInDifferentScope<UseETagEntity>("Use", 1).ConfigureAwait(false);

    // Expected behaviour: Throw
    // Actual: Doesn't throw
    var threw = false;
    try
    {
        await scopeDb.SaveChangesAsync().ConfigureAwait(false);
    }
    catch (DbUpdateConcurrencyException)
    {
        threw = true;
    }

    Console.WriteLine($"UseETag threw: {threw}");
}

using (var scope = serviceProvider.CreateScope())
{
    Console.WriteLine("IsETag - should throw, does");
    var scopeDb = scope.ServiceProvider.GetRequiredService<Db>();

    var isEntity = await scopeDb.IsETagEntities
        .WithPartitionKey("Is")
        .FirstAsync(x => x.Id == 2)
        .ConfigureAwait(false);

    isEntity.PropertyToChange = "Modified in scope";

    await ModifyInDifferentScope<IsETagEntity>("Is", 2).ConfigureAwait(false);

    // Expected behaviour: Throws
    // Actual: Throws
    var threw = false;
    try
    {
        await scopeDb.SaveChangesAsync().ConfigureAwait(false);
    }
    catch (DbUpdateConcurrencyException)
    {
        threw = true;
    }

    Console.WriteLine($"IsETag threw: {threw}");
}

using (var scope = serviceProvider.CreateScope())
{
    Console.WriteLine("NoETag - shouldn't throw, doesn't");
    var scopeDb = scope.ServiceProvider.GetRequiredService<Db>();

    var isEntity = await scopeDb.NoETagEntities
        .WithPartitionKey("No")
        .FirstAsync(x => x.Id == 3)
        .ConfigureAwait(false);

    isEntity.PropertyToChange = "Modified in scope";

    await ModifyInDifferentScope<NoETagEntity>("No", 3).ConfigureAwait(false);

    // Optional
    // Breakpoint 3 - Before continuing, edit NoETagEntity with id 3
    // Expected behaviour: Doesn't throw
    // Actual: Doesn't throw
    var threw = false;
    try
    {
        await scopeDb.SaveChangesAsync().ConfigureAwait(false);
    }
    catch (DbUpdateConcurrencyException)
    {
        threw = true;
    }

    Console.WriteLine($"NoETag threw: {threw}");
}

async Task ModifyInDifferentScope<T>(string partitionKey, int id) where T : Entity
{
    var differentScope = serviceProvider.CreateScope();

    var differentScopeDb = differentScope.ServiceProvider.GetRequiredService<Db>();

    var entity = await differentScopeDb.Set<T>()
        .WithPartitionKey(partitionKey)
        .FirstAsync(x => x.Id == id)
        .ConfigureAwait(false);

    entity.PropertyToChange = "Modified in different scope";

    await differentScopeDb.SaveChangesAsync().ConfigureAwait(false);
}
