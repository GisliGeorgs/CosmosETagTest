using Microsoft.EntityFrameworkCore;

namespace CosmosEtag;

public class Db : DbContext
{
    public DbSet<UseETagEntity> UseEtagEntities => Set<UseETagEntity>();
    public DbSet<IsETagEntity> IsETagEntities => Set<IsETagEntity>();
    public DbSet<NoETagEntity> NoETagEntities => Set<NoETagEntity>();

    public Db(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var useEntity = modelBuilder.Entity<UseETagEntity>()
            .ToContainer(nameof(UseETagEntity));
        useEntity.UseETagConcurrency();
        useEntity.HasPartitionKey(x => x.PartitionKey);
        useEntity.HasKey(x => x.Id);

        var isEntity = modelBuilder.Entity<IsETagEntity>()
            .ToContainer(nameof(IsETagEntity));
        isEntity.Property(x => x._etag).IsETagConcurrency();
        isEntity.HasPartitionKey(x => x.PartitionKey);
        isEntity.HasKey(x => x.Id);

        var noEntity = modelBuilder.Entity<NoETagEntity>()
            .ToContainer(nameof(NoETagEntity));
        noEntity.HasPartitionKey(x => x.PartitionKey);
        noEntity.HasKey(x => x.Id);

        base.OnModelCreating(modelBuilder);
    }
}

public abstract class Entity
{
    public string PartitionKey { get; set; } = string.Empty;
    public int Id { get; set; }

    public string PropertyToChange { get; set; } = string.Empty;
}
public class UseETagEntity : Entity
{
}

public class IsETagEntity : Entity
{
    public string _etag { get; set; } = string.Empty;
}

public class NoETagEntity : Entity
{
}
