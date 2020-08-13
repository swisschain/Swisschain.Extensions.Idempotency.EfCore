using Microsoft.EntityFrameworkCore;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    public interface IDbContextWithOutbox
    {
        // Setter is required for EF
        DbSet<OutboxEntity> Outbox { get; set; }
    }
}
