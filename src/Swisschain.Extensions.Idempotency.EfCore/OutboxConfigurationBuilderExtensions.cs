using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    public static class OutboxConfigurationBuilderExtensions
    {
        public static OutboxConfigurationBuilder PersistWithEfCore<TDbContext>(this OutboxConfigurationBuilder builder,
            Func<IServiceProvider, TDbContext> dbContextFactory)

            where TDbContext : DbContext, IDbContextWithOutbox
        {
            builder.Services.AddTransient<IOutboxRepository, OutboxRepository<TDbContext>>();
            builder.Services.AddSingleton<Func<TDbContext>>(s => () => dbContextFactory.Invoke(s));

            return builder;
        }
    }
}
