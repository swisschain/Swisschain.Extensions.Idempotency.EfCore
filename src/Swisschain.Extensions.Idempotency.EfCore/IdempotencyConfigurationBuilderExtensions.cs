using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    public static class IdempotencyConfigurationBuilderExtensions
    {
        public static IdempotencyConfigurationBuilder<TUnitOfWork> PersistWithEfCore<TUnitOfWork, TDbContext>(
            this IdempotencyConfigurationBuilder<TUnitOfWork> builder,
            Func<IServiceProvider, TDbContext> dbContextFactory)
            where TDbContext : DbContext, IDbContextWithOutbox, IDbContextWithIdGenerator
            where TUnitOfWork : UnitOfWorkBase<TDbContext>
        {
            return builder.PersistWithEfCore(dbContextFactory, null);
        }

        public static IdempotencyConfigurationBuilder<TUnitOfWork> PersistWithEfCore<TUnitOfWork, TDbContext>(
            this IdempotencyConfigurationBuilder<TUnitOfWork> builder,
            Func<IServiceProvider, TDbContext> dbContextFactory,
            Action<IdempotencyEfCoreOptions<TDbContext, TUnitOfWork>> optionsBuilder)
            where TDbContext : DbContext, IDbContextWithOutbox, IDbContextWithIdGenerator
            where TUnitOfWork : UnitOfWorkBase<TDbContext>
        {
            var options = new IdempotencyEfCoreOptions<TDbContext, TUnitOfWork>();

            optionsBuilder?.Invoke(options);

            Func<IServiceProvider, TUnitOfWork> unitOfWorkFactory;

            if (options.UnitOfWorkFactory == null)
            {
                builder.Services.AddTransient<TUnitOfWork>();

                unitOfWorkFactory = s => s.GetRequiredService<TUnitOfWork>();
            }
            else
            {
                unitOfWorkFactory = options.UnitOfWorkFactory;
            }

            builder.Services.AddTransient<IUnitOfWorkFactory<TUnitOfWork>>(c =>
                new UnitOfWorkFactory<TUnitOfWork, TDbContext>(
                    c.GetRequiredService<IOutboxDispatcher>(),
                    () => dbContextFactory.Invoke(c),
                    () => unitOfWorkFactory(c)));

            builder.Services.AddTransient<IOutboxReadRepository>(c =>
                new OutboxReadRepository<TDbContext>(
                    c.GetRequiredService<ILogger<OutboxReadRepository<TDbContext>>>(),
                    () => dbContextFactory.Invoke(c),
                    options.OutboxDeserializer));
            
            builder.Services.AddTransient<IIdGeneratorRepository, IdGeneratorRepository<TDbContext>>(c =>
                new IdGeneratorRepository<TDbContext>(
                    () => dbContextFactory.Invoke(c)));

            return builder;
        }
    }
}
