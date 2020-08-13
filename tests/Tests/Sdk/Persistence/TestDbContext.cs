using Microsoft.EntityFrameworkCore;
using Swisschain.Extensions.Idempotency.EfCore;
using Tests.Sdk.Persistence.TestEntities;

namespace Tests.Sdk.Persistence
{
    public class TestDbContext : DbContext, IDbContextWithOutbox, IDbContextWithIdGenerator
    {
        public const string Schema = "tests";

        public TestDbContext(DbContextOptions options) :
            base(options)
        {
        }

        public DbSet<TestEntity> TestEntities { get; set; }
        public DbSet<OutboxEntity> Outbox { get; set; }
        public DbSet<IdGeneratorEntity> IsGenerator { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);

            modelBuilder.BuildIdempotency(x =>
            {
                x.AddIdGenerator(IdGenerators.TestEntities);
                x.AddIdGenerator(IdGenerators.SomeSequence);
                x.AddIdGenerator(IdGenerators.StartedFrom100Sequence, 100, 10);
            });

            modelBuilder.Entity<TestEntity>()
                .ToTable("test_entities")
                .HasKey(x => x.Id);
            
            base.OnModelCreating(modelBuilder);
        }
    }
}
