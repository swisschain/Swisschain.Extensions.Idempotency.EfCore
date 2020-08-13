using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Swisschain.Extensions.Idempotency;
using Tests.Sdk;
using Xunit;

namespace Tests
{
    public class UnitOfWorkTests : IClassFixture<TestsFixture>
    {
        private readonly TestsFixture _fixture;

        public UnitOfWorkTests(TestsFixture fixture)
        {
            _fixture = fixture;
        }
        
        [Fact]
        public async Task TestPositiveCase()
        {
            // Arrange

            var domainEvent = new TestDomainEvent {Id = 1};
            var domainCommand = new TestDomainCommand {Id = 2};
            var domainResponse = new TestDomainResponse {Id = 3};

            // Act

            await using var unitOfWork = await _fixture.UnitOfWorkManager.Begin("a");

            if (!unitOfWork.Outbox.IsClosed)
            {
                await unitOfWork.TestEntities.Add(1, "one");

                unitOfWork.Outbox.Publish(domainEvent);
                unitOfWork.Outbox.Send(domainCommand);
                unitOfWork.Outbox.Return(domainResponse);

                await unitOfWork.Commit();
            }

            await unitOfWork.EnsureOutboxDispatched();

            // Assert

            var readName = await _fixture.TestEntitiesRepository.GetNameOrDefault(1);

            readName.ShouldBe("one");

            _fixture.OutboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().ShouldHaveSingleItem();
            _fixture.OutboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().Single().Id.ShouldBe(1);

            _fixture.OutboxDispatcher.SentCommands.OfType<TestDomainCommand>().ShouldHaveSingleItem();
            _fixture.OutboxDispatcher.SentCommands.OfType<TestDomainCommand>().Single().Id.ShouldBe(2);

            unitOfWork.Outbox.GetResponse<TestDomainResponse>().ShouldNotBeNull();
            unitOfWork.Outbox.GetResponse<TestDomainResponse>().Id.ShouldBe(3);
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
