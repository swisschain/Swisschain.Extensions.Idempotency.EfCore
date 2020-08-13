using Microsoft.EntityFrameworkCore;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    public interface IDbContextWithIdGenerator
    {
        // Setter is required for EF
        DbSet<IdGeneratorEntity> IsGenerator { get; set; }
    }
}
