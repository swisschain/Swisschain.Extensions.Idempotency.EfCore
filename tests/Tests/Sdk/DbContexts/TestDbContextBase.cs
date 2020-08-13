using Microsoft.EntityFrameworkCore;
using Swisschain.Extensions.Idempotency.EfCore;

namespace Tests.Sdk.DbContexts
{
    public class TestDbContextBase : DbContext, IDbContextWithOutbox, IDbContextWithIdGenerator
    {
        protected TestDbContextBase(DbContextOptions options) :
            base(options)
        {
        }

        public DbSet<TestEntity> TestEntities { get; set; }
        public DbSet<OutboxEntity> Outbox { get; set; }
        public DbSet<IdGeneratorEntity> IsGenerator { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>()
                .ToTable("test_entities")
                .HasKey(x => x.Id);

            base.OnModelCreating(modelBuilder);
        }
    }
}
