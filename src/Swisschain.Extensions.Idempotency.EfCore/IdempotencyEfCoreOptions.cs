using System;
using Microsoft.EntityFrameworkCore;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    public sealed class IdempotencyEfCoreOptions<TDbContext, TUnitOfWork>
        where TDbContext : DbContext, IDbContextWithOutbox, IDbContextWithIdGenerator
        where TUnitOfWork : UnitOfWorkBase<TDbContext>
    {
        internal IdempotencyEfCoreOptions()
        {
            OutboxDeserializer = new OutboxDeserializerOptions();
        }

        public OutboxDeserializerOptions OutboxDeserializer { get; }
        public Func<IServiceProvider, TUnitOfWork> UnitOfWorkFactory { get; }
    }
}
