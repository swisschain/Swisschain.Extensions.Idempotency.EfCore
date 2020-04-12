# Swisschain.Extensions.Idempotency.EfCore
Entity Framework Core implementations of the Idempotency extensions

## Install nuget package

`Install-Package swisschain.extensions.idempotency.EfCore`

## Initialization

Add `PersistWithEfCore` when configuring `OutboxConfigurationBuilder` inside `services.AddOutbox` call and return an instance of your `DbContext` implementation:

```c#
services.AddOutbox(c =>
{
    c.PersistWithEfCore(s =>
    {
        var optionsBuilder = s.GetRequiredService<DbContextOptionsBuilder<DatabaseContext>>();

        return new DatabaseContext(optionsBuilder.Options);
    });
});
```

Implement `IDbContextWithOutbox` by your inheritor of the `DbContext`:

```c#
public class DatabaseContext : DbContext, IDbContextWithOutbox
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) :
        base(options)
    {
    }

    // Do not update this collection directly. Outbox will manage everything for you
    public DbSet<OutboxEntity> Outbox { get; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add outbox to the model builder. You can override table name (`outbox` by default) and initial value for the aggregate ID sequence generator (2 by default):
        modelBuilder.BuildOutbox();
    }
}
```

Generate EF migration `Add-Migration Initial` in the Package Manager Console