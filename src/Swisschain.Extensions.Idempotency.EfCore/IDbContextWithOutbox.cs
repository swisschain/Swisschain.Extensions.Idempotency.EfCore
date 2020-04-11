using Microsoft.EntityFrameworkCore;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    public interface IDbContextWithOutbox
    {
        DbSet<OutboxEntity> Outbox { get; }
    }
}