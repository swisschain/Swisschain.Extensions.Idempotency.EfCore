using System;
using System.Threading.Tasks;
using Shouldly;
using Tests.Sdk;
using Xunit;

namespace Tests
{
    public class NonTransactionalUnitOfWorkTests : PersistenceTests
    {
        public NonTransactionalUnitOfWorkTests(PersistenceFixture fixture) : 
            base(fixture)
        {
        }

        [Fact]
        public async Task TestPositiveCase()
        {
            // Arrange / Act

            await using var unitOfWork = await UnitOfWorkManager.Begin();
            await unitOfWork.TestEntities.Add(1, "one");

            // Assert

            var readName = await EntitiesRepository.GetNameOrDefault(1);

            readName.ShouldBe("one");
        }

        [Fact]
        public async Task TestThatOutboxCantBeAccessed()
        {
            // Arrange / Act

            await using var unitOfWork = await UnitOfWorkManager.Begin();

            // Assert

            Should.Throw<InvalidOperationException>(() => unitOfWork.Outbox);
            Should.Throw<InvalidOperationException>(() => unitOfWork.OutboxWriteRepository);
            
            await unitOfWork.Commit().ShouldThrowAsync<InvalidOperationException>();
            await unitOfWork.Rollback().ShouldThrowAsync<InvalidOperationException>();
            await unitOfWork.EnsureOutboxDispatched().ShouldThrowAsync<InvalidOperationException>();
        }
    }
}
