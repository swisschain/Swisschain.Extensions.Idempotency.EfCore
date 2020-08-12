using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    internal sealed class UnitOfWorkFactory<TUnitOfWork, TDbContext> : IUnitOfWorkFactory<TUnitOfWork>
        where TUnitOfWork : UnitOfWorkBase<TDbContext>, new()
        where TDbContext : DbContext, IDbContextWithOutbox, IDbContextWithIdGenerator 
    {
        private readonly Func<TDbContext> _dbContextFactory;

        public UnitOfWorkFactory(Func<TDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<TUnitOfWork> Create(Outbox outbox)
        {
            var dbContext = _dbContextFactory.Invoke();
            var unitOfWork = new TUnitOfWork();

            await unitOfWork.Init(dbContext, outbox);

            return unitOfWork;
        }
    }
}
