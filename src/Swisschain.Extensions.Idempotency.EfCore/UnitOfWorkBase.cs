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

        /// <summary>
        /// Initializes transactional unit of work.
        /// Used internally by Swisschain.Idempotency infrastructure
        /// </summary>
        public async Task Init(IOutboxDispatcher defaultOutboxDispatcher, TDbContext dbContext, Outbox outbox)
        {
            _dbContext = dbContext;
            _transaction = await dbContext.Database.BeginTransactionAsync();

            var outboxWriteRepository = new OutboxWriteRepository<TDbContext>(dbContext);

            ProvisionRepositories(dbContext);

            await Init(defaultOutboxDispatcher, outboxWriteRepository, outbox);
        }

        /// <summary>
        /// Initializes non-transactional unit of work.
        /// Used internally by Swisschain.Idempotency infrastructure
        /// </summary>
        public Task Init(TDbContext dbContext)
        {
            _dbContext = dbContext;

            ProvisionRepositories(dbContext);

            return Task.CompletedTask;
        }

        protected override Task CommitImpl()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("UnitOfWork is either not initialized or its non-transactional");
            }

            return _transaction.CommitAsync();
        }

        protected override Task RollbackImpl()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("UnitOfWork is either not initialized or its non-transactional");
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
