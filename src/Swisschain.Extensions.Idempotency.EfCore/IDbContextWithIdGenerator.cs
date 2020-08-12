using Microsoft.EntityFrameworkCore;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    public interface IDbContextWithIdGenerator
    {
        DbSet<IdGeneratorEntity> IsGenerator { get; }
    }
}
