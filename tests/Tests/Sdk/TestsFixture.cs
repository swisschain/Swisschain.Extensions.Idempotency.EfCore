using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Swisschain.Extensions.Idempotency;
using Swisschain.Extensions.Idempotency.EfCore;
using Swisschain.Extensions.Testing;
using Tests.Sdk.DbContexts;

namespace Tests.Sdk
{
    public class TestsFixture : PostgresFixture
    {
        private readonly ServiceProvider _serviceProvider;

        public TestsFixture() :
            base("idempotency-tests-pg")
        {
            OutboxDispatcher = new InMemoryOutboxDispatcher();

            var services = new ServiceCollection();
            
            services.AddLogging();
            services.AddIdempotency<TestUnitOfWork>(x =>
            {
                x.PersistWithEfCore(s => CreateDbContext());
                x.Services.AddSingleton<IOutboxDispatcher>(OutboxDispatcher);
            });

            _serviceProvider = services.BuildServiceProvider();

            UnitOfWorkManager = _serviceProvider.GetRequiredService<IUnitOfWorkManager<TestUnitOfWork>>();
        }

        public InMemoryOutboxDispatcher OutboxDispatcher { get; }
        public IUnitOfWorkManager<TestUnitOfWork> UnitOfWorkManager { get; }
        public TestEntitiesRepository TestEntitiesRepository { get; private set; }
        public IOutboxReadRepository OutboxReadRepository { get; private set; }
        public IIdGeneratorRepository IdGeneratorRepository { get; private set; }

        protected override async Task InitializeAsync()
        {
            await CreateTestDb();

            var dbContext = CreateDbContext();

            await dbContext.Database.MigrateAsync();

            TestEntitiesRepository = new TestEntitiesRepository(dbContext);
            OutboxReadRepository = new OutboxReadRepository<TestDbContextWithDefaultOptions>(
                NullLogger<OutboxReadRepository<TestDbContextWithDefaultOptions>>.Instance,
                () => dbContext);
            IdGeneratorRepository = new IdGeneratorRepository<TestDbContextWithDefaultOptions>(() => dbContext);
        }

        protected override async Task DisposeAsync()
        {
            await DropTestDb();

            await _serviceProvider.DisposeAsync();
        }

        private TestDbContextWithDefaultOptions CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContextWithDefaultOptions>();

            optionsBuilder
                .UseNpgsql(GetConnectionString(),
                    builder =>
                        builder.MigrationsHistoryTable(
                            "__Migrations",
                            TestDbContextWithDefaultOptions.Schema));

            return new TestDbContextWithDefaultOptions(optionsBuilder.Options);
        }
    }
}
