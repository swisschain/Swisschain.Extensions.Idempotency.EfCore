using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    public class UnitOfWorkBase<TDbContext> : UnitOfWorkBase
        where TDbContext : DbContext, IDbContextWithOutbox, IDbContextWithIdGenerator 
    {
        private TDbContext _dbContext;
        private IDbContextTransaction _transaction;
        
        public UnitOfWorkBase(IOutboxDispatcher defaultOutboxDispatcher) : 
            base(defaultOutboxDispatcher)
        {
        }

        public async Task Init(TDbContext dbContext, Outbox outbox)
        {
            var outboxWriteRepository = new OutboxWriteRepository<TDbContext>(dbContext);

            _dbContext = dbContext;
            _transaction = await dbContext.Database.BeginTransactionAsync();

            await Init(outboxWriteRepository, outbox);
        }

        protected override Task CommitImpl()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("UnitOfWork is not initialized");
            }

            return _transaction.CommitAsync();
        }

        protected override Task RollbackImpl()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("UnitOfWork is not initialized");
            }

            return _transaction.RollbackAsync();
        }

        protected override async ValueTask DisposeAsync(bool disposing)
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }

            if (_dbContext != null)
            {
                await _dbContext.DisposeAsync();
            }
        }
    }
}
