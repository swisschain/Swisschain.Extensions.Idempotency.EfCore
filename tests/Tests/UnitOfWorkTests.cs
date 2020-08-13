using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Swisschain.Extensions.Idempotency;
using Swisschain.Extensions.Idempotency.EfCore;
using Swisschain.Extensions.Testing;
using Tests.Sdk;
using Tests.Sdk.DbContexts;
using Xunit;

namespace Tests
{
    public class UnitOfWorkTests : IClassFixture<TestsFixture>, IAsyncLifetime

    {
        private readonly PostgresFixture _fixture;

        public UnitOfWorkTests(TestsFixture fixture)
        {
            _fixture = fixture;
        }
        
        [Fact]
        public async Task TestPositiveCase()
        {
            var services = new ServiceCollection();
            var outboxDispatcher = new InMemoryOutboxDispatcher();
            var domainEvent = new TestDomainEvent {Id = 1};
            var domainCommand = new TestDomainCommand {Id = 2};
            var domainResponse = new TestDomainResponse {Id = 3};

            services.AddLogging();
            services.AddIdempotency<TestUnitOfWork>(x =>
            {
                x.PersistWithEfCore(s => CreateDbContext());
                x.Services.AddSingleton<IOutboxDispatcher>(outboxDispatcher);
            });

            await using var serviceProvider = services.BuildServiceProvider();

            var dbContext = CreateDbContext();

            await dbContext.Database.MigrateAsync();

            var unitOfWorkManager = serviceProvider.GetRequiredService<IUnitOfWorkManager<TestUnitOfWork>>();

            await using var unitOfWork = await unitOfWorkManager.Begin("a");

            if (!unitOfWork.Outbox.IsClosed)
            {
                await unitOfWork.TestEntities.Add(1, "one");

                unitOfWork.Outbox.Publish(domainEvent);
                unitOfWork.Outbox.Send(domainCommand);
                unitOfWork.Outbox.Return(domainResponse);

                await unitOfWork.Commit();
            }

            await unitOfWork.EnsureOutboxDispatched();

            var testEntitiesRepository = new TestEntitiesRepository(dbContext);

            var readName = await testEntitiesRepository.GetNameOrDefault(1);

            readName.ShouldBe("one");

            outboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().ShouldHaveSingleItem();
            outboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().Single().Id.ShouldBe(1);

            outboxDispatcher.SentCommands.OfType<TestDomainCommand>().ShouldHaveSingleItem();
            outboxDispatcher.SentCommands.OfType<TestDomainCommand>().Single().Id.ShouldBe(2);

            unitOfWork.Outbox.GetResponse<TestDomainResponse>().ShouldNotBeNull();
            unitOfWork.Outbox.GetResponse<TestDomainResponse>().Id.ShouldBe(3);
        }

        public Task InitializeAsync()
        {
            return _fixture.CreateTestDb();
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            return _fixture.DropTestDb();
        }

        private TestDbContextWithDefaultOptions CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContextWithDefaultOptions>();

            optionsBuilder
                .UseNpgsql(_fixture.GetConnectionString(),
                    builder =>
                        builder.MigrationsHistoryTable(
                            "__Migrations",
                            TestDbContextWithDefaultOptions.Schema));

            return new TestDbContextWithDefaultOptions(optionsBuilder.Options);
        }

        private class TestDomainEvent
        {
            public long Id { get; set; }
        }

        private class TestDomainCommand
        {
            public long Id { get; set; }
        }

        private class TestDomainResponse
        {
            public long Id { get; set; }
        }
    }
}
