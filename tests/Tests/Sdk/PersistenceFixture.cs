using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Swisschain.Extensions.Testing;
using Tests.Sdk.Persistence;

namespace Tests.Sdk
{
    public class PersistenceFixture : PostgresFixture
    {
        public PersistenceFixture() :
            base("idempotency-tests-pg")
        {
        }

        public TestDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

            optionsBuilder
                .UseNpgsql(GetConnectionString(),
                    builder =>
                        builder.MigrationsHistoryTable(
                            "__Migrations",
                            TestDbContext.Schema));

            return new TestDbContext(optionsBuilder.Options);
        }

        protected override Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
