using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Tests.Sdk;
using Xunit;

namespace Tests
{
    public class UnitOfWorkTests : PersistenceTests
    {
        public UnitOfWorkTests(PersistenceFixture fixture) : 
            base(fixture)
        {
        }
        
        [Fact]
        public async Task TestPositiveCase()
        {
            // Arrange / Act

            await using var unitOfWork = await UnitOfWorkManager.Begin("a");

            unitOfWork.Outbox.IsClosed.ShouldBeFalse();

            await unitOfWork.TestEntities.Add(1, "one");

            unitOfWork.Outbox.Publish(new TestDomainEvent {Id = 1});
            unitOfWork.Outbox.Send(new TestDomainCommand {Id = 2});
            unitOfWork.Outbox.Return(new TestDomainResponse {Id = 3});

            await unitOfWork.Commit();

            await unitOfWork.EnsureOutboxDispatched();

            // Assert

            var readName = await EntitiesRepository.GetNameOrDefault(1);

            readName.ShouldBe("one");

            OutboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().ShouldHaveSingleItem();
            OutboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().Single().Id.ShouldBe(1);

            OutboxDispatcher.SentCommands.OfType<TestDomainCommand>().ShouldHaveSingleItem();
            OutboxDispatcher.SentCommands.OfType<TestDomainCommand>().Single().Id.ShouldBe(2);

            unitOfWork.Outbox.GetResponse<TestDomainResponse>().ShouldNotBeNull();
            unitOfWork.Outbox.GetResponse<TestDomainResponse>().Id.ShouldBe(3);
        }

        [Fact]
        public async Task TestTransientErrorBeforeCommit()
        {
            // Arrange

            async Task<TestDomainResponse> Act(bool error, int attempt)
            {
                await using var unitOfWork = await UnitOfWorkManager.Begin("a");

                if (!unitOfWork.Outbox.IsClosed)
                {
                    await unitOfWork.TestEntities.Add(1, attempt.ToString());

                    unitOfWork.Outbox.Publish(new TestDomainEvent {Id = attempt});
                    unitOfWork.Outbox.Send(new TestDomainCommand {Id = attempt * 10});
                    unitOfWork.Outbox.Return(new TestDomainResponse {Id = attempt * 100});

                    if (error)
                    {
                        throw new InvalidOperationException("Some transient error");
                    }

                    await unitOfWork.Commit();
                }

                await unitOfWork.EnsureOutboxDispatched();

                return unitOfWork.Outbox.GetResponse<TestDomainResponse>();
            }

            // Act 1
            
            await Act(error: true, attempt: 1).ShouldThrowAsync<InvalidOperationException>();

            // Assert 1

            var readName1 = await EntitiesRepository.GetNameOrDefault(1);

            readName1.ShouldBeNull();

            OutboxDispatcher.PublishedEvents.ShouldBeEmpty();
            OutboxDispatcher.SentCommands.ShouldBeEmpty();

            // Act 2

            OutboxDispatcher.Clear();

            var response2 = await Act(error: false, attempt: 2);

            // Assert 2

            var readName2 = await EntitiesRepository.GetNameOrDefault(1);
            
            readName2.ShouldBe("2");

            OutboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().ShouldHaveSingleItem();
            OutboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().Single().Id.ShouldBe(2);

            OutboxDispatcher.SentCommands.OfType<TestDomainCommand>().ShouldHaveSingleItem();
            OutboxDispatcher.SentCommands.OfType<TestDomainCommand>().Single().Id.ShouldBe(2 * 10);

            response2.Id.ShouldBe(2 * 100);
        }
        
        [Fact]
        public async Task TestTransientErrorAfterCommit()
        {
            // Arrange

            async Task<TestDomainResponse> Act(int attempt)
            {
                await using var unitOfWork = await UnitOfWorkManager.Begin("a");

                if (!unitOfWork.Outbox.IsClosed)
                {
                    await unitOfWork.TestEntities.Add(1, attempt.ToString());

                    unitOfWork.Outbox.Publish(new TestDomainEvent {Id = attempt});
                    unitOfWork.Outbox.Send(new TestDomainCommand {Id = attempt * 10});
                    unitOfWork.Outbox.Return(new TestDomainResponse {Id = attempt * 100});

                    await unitOfWork.Commit();

                    throw new InvalidOperationException("Some transient error");
                }

                await unitOfWork.EnsureOutboxDispatched();

                return unitOfWork.Outbox.GetResponse<TestDomainResponse>();
            }

            // Act 1
            
            await Act(attempt: 1).ShouldThrowAsync<InvalidOperationException>();

            // Assert 1

            var readName1 = await EntitiesRepository.GetNameOrDefault(1);

            readName1.ShouldBe("1");

            OutboxDispatcher.PublishedEvents.ShouldBeEmpty();
            OutboxDispatcher.SentCommands.ShouldBeEmpty();
            
            // Act 2

            OutboxDispatcher.Clear();

            var response2 = await Act(attempt: 2);

            // Assert 2

            var readName2 = await EntitiesRepository.GetNameOrDefault(1);
            
            readName2.ShouldBe("1");

            OutboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().ShouldHaveSingleItem();
            OutboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().Single().Id.ShouldBe(1);

            OutboxDispatcher.SentCommands.OfType<TestDomainCommand>().ShouldHaveSingleItem();
            OutboxDispatcher.SentCommands.OfType<TestDomainCommand>().Single().Id.ShouldBe(1 * 10);

            response2.Id.ShouldBe(1 * 100);
        }

        [Fact]
        public async Task TestDuplication()
        {
            // Arrange

            async Task<TestDomainResponse> Act(int attempt)
            {
                await using var unitOfWork = await UnitOfWorkManager.Begin("a");

                if (!unitOfWork.Outbox.IsClosed)
                {
                    await unitOfWork.TestEntities.Add(1, attempt.ToString());

                    unitOfWork.Outbox.Publish(new TestDomainEvent {Id = attempt});
                    unitOfWork.Outbox.Send(new TestDomainCommand {Id = attempt * 10});
                    unitOfWork.Outbox.Return(new TestDomainResponse {Id = attempt * 100});

                    await unitOfWork.Commit();
                }

                await unitOfWork.EnsureOutboxDispatched();

                return unitOfWork.Outbox.GetResponse<TestDomainResponse>();
            }

            // Act 1

            var response1 = await Act(attempt: 1);

            // Assert 1

            var readName1 = await EntitiesRepository.GetNameOrDefault(1);

            readName1.ShouldBe("1");

            OutboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().ShouldHaveSingleItem();
            OutboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().Single().Id.ShouldBe(1);

            OutboxDispatcher.SentCommands.OfType<TestDomainCommand>().ShouldHaveSingleItem();
            OutboxDispatcher.SentCommands.OfType<TestDomainCommand>().Single().Id.ShouldBe(1 * 10);

            response1.Id.ShouldBe(1 * 100);

            // Act 2

            OutboxDispatcher.Clear();

            var response2 = await Act(attempt: 2);

            // Assert 2

            var readName2 = await EntitiesRepository.GetNameOrDefault(1);
            
            readName2.ShouldBe("1");

            OutboxDispatcher.PublishedEvents.ShouldBeEmpty();
            OutboxDispatcher.SentCommands.ShouldBeEmpty();

            response2.Id.ShouldBe(1 * 100);
        }

        [Fact]
        public async Task TestMissedCommitWithDispatching()
        {
            // Arrange / Act / Assert

            await using var unitOfWork = await UnitOfWorkManager.Begin("a");

            unitOfWork.Outbox.IsClosed.ShouldBeFalse();

            await unitOfWork.TestEntities.Add(1, "one");

            unitOfWork.Outbox.Publish(new TestDomainEvent {Id = 1});
            unitOfWork.Outbox.Send(new TestDomainCommand {Id = 2});
            unitOfWork.Outbox.Return(new TestDomainResponse {Id = 3});

            await unitOfWork.EnsureOutboxDispatched().ShouldThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task TestMissedCommit()
        {
            // Arrange / Act 1

            await using (var unitOfWork = await UnitOfWorkManager.Begin("a"))
            {
                unitOfWork.Outbox.IsClosed.ShouldBeFalse();

                await unitOfWork.TestEntities.Add(1, "one");

                unitOfWork.Outbox.Publish(new TestDomainEvent {Id = 1});
                unitOfWork.Outbox.Send(new TestDomainCommand {Id = 10});
                unitOfWork.Outbox.Return(new TestDomainResponse {Id = 100});
            }
            
            // Assert 1

            var readName1 = await EntitiesRepository.GetNameOrDefault(1);

            readName1.ShouldBeNull();

            OutboxDispatcher.PublishedEvents.ShouldBeEmpty();
            OutboxDispatcher.SentCommands.ShouldBeEmpty();

            // Arrange / Act 1

            TestDomainResponse response2;

            await using (var unitOfWork = await UnitOfWorkManager.Begin("a"))
            {
                unitOfWork.Outbox.IsClosed.ShouldBeFalse();

                await unitOfWork.TestEntities.Add(1, "two");

                unitOfWork.Outbox.Publish(new TestDomainEvent {Id = 2});
                unitOfWork.Outbox.Send(new TestDomainCommand {Id = 20});
                unitOfWork.Outbox.Return(new TestDomainResponse {Id = 200});

                await unitOfWork.Commit();

                await unitOfWork.EnsureOutboxDispatched();

                response2 = unitOfWork.Outbox.GetResponse<TestDomainResponse>();
            }
            
            // Assert 1

            var readName2 = await EntitiesRepository.GetNameOrDefault(1);

            readName2.ShouldBe("two");

            OutboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().ShouldHaveSingleItem();
            OutboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().Single().Id.ShouldBe(2);

            OutboxDispatcher.SentCommands.OfType<TestDomainCommand>().ShouldHaveSingleItem();
            OutboxDispatcher.SentCommands.OfType<TestDomainCommand>().Single().Id.ShouldBe(20);

            response2.Id.ShouldBe(200);
        }

        [Fact]
        public async Task TestMissedRollback()
        {
            // Arrange / Act 1

            await using (var unitOfWork = await UnitOfWorkManager.Begin("a"))
            {
                unitOfWork.Outbox.IsClosed.ShouldBeFalse();

                await unitOfWork.TestEntities.Add(1, "one");

                unitOfWork.Outbox.Publish(new TestDomainEvent {Id = 1});
                unitOfWork.Outbox.Send(new TestDomainCommand {Id = 10});
                unitOfWork.Outbox.Return(new TestDomainResponse {Id = 100});

                await unitOfWork.Rollback();
            }
            
            // Assert 1

            var readName1 = await EntitiesRepository.GetNameOrDefault(1);

            readName1.ShouldBeNull();

            OutboxDispatcher.PublishedEvents.ShouldBeEmpty();
            OutboxDispatcher.SentCommands.ShouldBeEmpty();

            // Arrange / Act 1

            TestDomainResponse response2;

            await using (var unitOfWork = await UnitOfWorkManager.Begin("a"))
            {
                unitOfWork.Outbox.IsClosed.ShouldBeFalse();

                await unitOfWork.TestEntities.Add(1, "two");

                unitOfWork.Outbox.Publish(new TestDomainEvent {Id = 2});
                unitOfWork.Outbox.Send(new TestDomainCommand {Id = 20});
                unitOfWork.Outbox.Return(new TestDomainResponse {Id = 200});

                await unitOfWork.Commit();

                await unitOfWork.EnsureOutboxDispatched();

                response2 = unitOfWork.Outbox.GetResponse<TestDomainResponse>();
            }
            
            // Assert 1

            var readName2 = await EntitiesRepository.GetNameOrDefault(1);

            readName2.ShouldBe("two");

            OutboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().ShouldHaveSingleItem();
            OutboxDispatcher.PublishedEvents.OfType<TestDomainEvent>().Single().Id.ShouldBe(2);

            OutboxDispatcher.SentCommands.OfType<TestDomainCommand>().ShouldHaveSingleItem();
            OutboxDispatcher.SentCommands.OfType<TestDomainCommand>().Single().Id.ShouldBe(20);

            response2.Id.ShouldBe(200);
        }

        [Fact]
        public async Task CanBeDisposedAfterTimeoutException()
        {
            // Arrange/Act/Assert

            var unitOfWork = await UnitOfWorkManager.Begin("a");

            try
            {
                await Should.ThrowAsync<Npgsql.NpgsqlException>(async () => await unitOfWork.TestEntities.RiseTimeoutException());                
            }
            finally
            {
                await unitOfWork.DisposeAsync();
            }
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
