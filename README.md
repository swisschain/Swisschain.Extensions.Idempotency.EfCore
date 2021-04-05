# Swisschain.Extensions.Idempotency.EfCore
Entity Framework Core implementations of the Idempotency extensions

## Install nuget package

`Install-Package swisschain.extensions.idempotency.EfCore`

## Initialization

Derive your unit of work from `Swisschain.Extensions.Idempotency.EfCore.UnitOfWorkBase<TDbContext>` class, passing your `DbContext` inheritor as a generic parameter:

```c#
public class TestUnitOfWork : UnitOfWorkBase<TestDbContext>
{
    public ITransfersRepository Transfers { get; private set; }
    public IOrdersRepository Orders { get; private set; }

    protected override void ProvisionRepositories(DatabaseContext dbContext)
    {
        Transfers = new TransfersRepository(dbContext);
        Orders = new OrdersRepository(dbContext);
    }
}
```

Repositories included into the unit of work should accept `DbContext` instance to the constructor and use it for all DB queries. It's recommended to split your repository to two independent classes,
if you need both unit-of-work-scoped and out-of-the-unit-of-work queries to the DB to avoid confusing.

Add `PersistWithEfCore` while configuring `IdempotencyConfigurationBuilder` inside `services.AddIdempotency` call and return an instance of your `DbContext` implementation:

```c#
services.AddIdempotency<UnitOfWork>(x =>
{
    x.PersistWithEfCore(
        s => s.GetRequiredService<DatabaseContext>(),
        o => 
        {
            o.OutboxDeserializer.AddAssembly(typeof(OrderCancelled).Assembly);
        });
});
```

Use `OutboxDeserializerOptions.OutboxDeserializer.AddAssembly` method to register non primitive types which can be persisted in the outbox.

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
