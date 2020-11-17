using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Swisschain.Extensions.Idempotency;
using Swisschain.Extensions.Idempotency.EfCore;
using Tests.Sdk.InMemoryMocks;
using Tests.Sdk.Persistence.TestEntities;
using Xunit;

namespace Tests.Sdk
{
    [Collection(nameof(PersistenceTests))]
    public class PersistenceTests : IClassFixture<PersistenceFixture>, IAsyncLifetime
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly List<DbContext> _dbContexts;
        
        protected PersistenceTests(PersistenceFixture fixture)
        {
            Fixture = fixture;

            OutboxDispatcher = new InMemoryOutboxDispatcher();

            var services = new ServiceCollection();
            
            services.AddLogging();
            services.AddIdempotency<TestUnitOfWork>(x =>
            {
                x.PersistWithEfCore(
                    s => Fixture.CreateDbContext(),
                    o =>
                    {
                        o.OutboxDeserializer.AddAssembly(typeof(PersistenceTests).Assembly);
                    });
                x.Services.AddSingleton<IOutboxDispatcher>(OutboxDispatcher);
            });

            _serviceProvider = services.BuildServiceProvider();

            UnitOfWorkManager = _serviceProvider.GetRequiredService<IUnitOfWorkManager<TestUnitOfWork>>();
            IdGenerator = _serviceProvider.GetRequiredService<IIdGenerator>();
            
            _dbContexts = new List<DbContext>();
        }

        public PersistenceFixture Fixture { get; }
        public InMemoryOutboxDispatcher OutboxDispatcher { get; }
        public IUnitOfWorkManager<TestUnitOfWork> UnitOfWorkManager { get; }
        public IIdGenerator IdGenerator { get; }
        public TestEntitiesRepository EntitiesRepository { get; set; }

        public async Task InitializeAsync()
        {
            var dbContext = Fixture.CreateDbContext();

            _dbContexts.Add(dbContext);

            EntitiesRepository = new TestEntitiesRepository(dbContext);

            await Fixture.CreateTestDb();

            await dbContext.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            foreach (var dbContext in _dbContexts)
            {
                await dbContext.DisposeAsync();
            }
            
            await Fixture.DropTestDb();
            await _serviceProvider.DisposeAsync();
        }
    }
}
