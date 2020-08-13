# Swisschain.Extensions.Idempotency.EfCore
Entity Framework Core implementations of the Idempotency extensions

## Install nuget package

`Install-Package swisschain.extensions.idempotency.EfCore`

## Initialization

Add `PersistWithEfCore` while configuring `IdempotencyConfigurationBuilder` inside `services.AddIdempotency` call and return an instance of your `DbContext` implementation:

```c#
services.AddIdempotency<UnitOfWork>(x =>
{
    x.PersistWithEfCore(s => s.GetRequiredService<DatabaseContext>());
});
```

Implement `IDbContextWithOutbox` and `IDbContextWithIdGenerator` by your inheritor of the `DbContext`:

```c#
public class DatabaseContext : DbContext, IDbContextWithOutbox, IDbContextWithIdGenerator
{
    public DatabaseContext(DbContextOptions options) :
        base(options)
    {
    }

    // Do not update these collections directly. Swisschain.Extensions.Idempotency will manage everything for you
    public DbSet<OutboxEntity> Outbox { get; set; }
    public DbSet<IdGeneratorEntity> IsGenerator { get; set; }
}
```

Configure `DbContext` model builder:

```c#
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.BuildIdempotency(x =>
    {
        x.OutboxTableName = "Outbox"; // It's optional. Default table name is "outbox"
        x.IdGeneratorTableName = "IdGenerator"; // It's optional. Default table name is "id_generator"
        
        // Register ID generators, that you want to use in your application:
        // Each generator is a named sequence which you can use to get unique ID.
        
        x.AddIdGenerator("id_generator_transfers");
        x.AddIdGenerator("id_generator_orders");
        
        // You can specify custom start number and increment size for the generator:
        
        x.AddIdGenerator("id_generator_withdrawals", startNumber: 100000, incrementSize: 10);
    });
}
        
```

Generate new EF migration.