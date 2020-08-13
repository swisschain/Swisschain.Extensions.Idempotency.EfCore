using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    public abstract class UnitOfWorkBase<TDbContext> : UnitOfWorkBase
        where TDbContext : DbContext, IDbContextWithOutbox, IDbContextWithIdGenerator 
    {
        private TDbContext _dbContext;
        private IDbContextTransaction _transaction;
        
        protected abstract void ProvisionRepositories(TDbContext dbContext);

        public async Task Init(IOutboxDispatcher defaultOutboxDispatcher, TDbContext dbContext, Outbox outbox)
        {
            _dbContext = dbContext;
            _transaction = await dbContext.Database.BeginTransactionAsync();

            var outboxWriteRepository = new OutboxWriteRepository<TDbContext>(dbContext);

            ProvisionRepositories(dbContext);

            await Init(defaultOutboxDispatcher, outboxWriteRepository, outbox);
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
