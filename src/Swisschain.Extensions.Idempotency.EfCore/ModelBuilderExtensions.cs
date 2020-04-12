using Microsoft.EntityFrameworkCore;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder BuildOutbox(this ModelBuilder modelBuilder, 
            string tableName = default, 
            long startAggregateIdFrom = 2)
        {
            modelBuilder.Entity<OutboxEntity>()
                .ToTable(tableName ?? "outbox")
                .HasKey(x => x.RequestId);

            modelBuilder.Entity<OutboxEntity>()
                .Property(x => x.AggregateId)
                .HasIdentityOptions(startAggregateIdFrom)
                .ValueGeneratedOnAdd();

            return modelBuilder;
        }
    }
}
