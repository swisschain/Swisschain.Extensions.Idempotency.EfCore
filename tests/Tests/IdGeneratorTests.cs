using System.Threading.Tasks;
using Shouldly;
using Tests.Sdk;
using Tests.Sdk.Persistence;
using Xunit;

namespace Tests
{
    public class IdGeneratorTests : PersistenceTests
    {
        public IdGeneratorTests(PersistenceFixture fixture) : 
            base(fixture)
        {
        }

        [Fact]
        public async Task TestPositiveCase()
        {
            // Arrange / Act

            var id = await IdGenerator.GetId("a", IdGenerators.SomeSequence);

            // Assert

            id.ShouldBe(1);
        }

        [Fact]
        public async Task TestCustomOptions()
        {
            // Arrange / Act

            var id1 = await IdGenerator.GetId("a", IdGenerators.StartedFrom100Sequence);
            var id2 = await IdGenerator.GetId("b", IdGenerators.StartedFrom100Sequence);

            // Assert

            id1.ShouldBe(100);
            id2.ShouldBe(110);
        }

        [Fact]
        public async Task TestDuplication()
        {
            // Arrange / Act

            var id1 = await IdGenerator.GetId("a", IdGenerators.SomeSequence);
            var id2 = await IdGenerator.GetId("a", IdGenerators.SomeSequence);

            // Assert

            id1.ShouldBe(1);
            id2.ShouldBe(id1);
        }

        [Fact]
        public async Task TestOneGeneratorDoesNotAffectAnother()
        {
            // Arrange / Act

            var id1 = await IdGenerator.GetId("a1", IdGenerators.SomeSequence);
            var id2 = await IdGenerator.GetId("a2", IdGenerators.StartedFrom100Sequence);

            // Assert

            id1.ShouldBe(1);
            id2.ShouldBe(100);
        }

        [Fact]
        public async Task TestOneGeneratorDoesNotAffectAnotherWithDuplication()
        {
            // Arrange / Act

            var id11 = await IdGenerator.GetId("a1", IdGenerators.SomeSequence);
            var id21 = await IdGenerator.GetId("a2", IdGenerators.StartedFrom100Sequence);

            var id12 = await IdGenerator.GetId("a1", IdGenerators.SomeSequence);
            var id22 = await IdGenerator.GetId("a2", IdGenerators.StartedFrom100Sequence);

            // Assert

            id11.ShouldBe(1);
            id21.ShouldBe(100);

            id12.ShouldBe(id11);
            id22.ShouldBe(id22);
        }

        [Fact]
        public async Task TestThatUnitOfWorkDoesNotAffectGenerator()
        {
            // Arrange / Act

            long id1;

            await using (var unitOfWork = await UnitOfWorkManager.Begin("a"))
            {
                id1 = await IdGenerator.GetId("a", IdGenerators.SomeSequence);

                await unitOfWork.Rollback();
            }
            
            var id2 = await IdGenerator.GetId("a", IdGenerators.SomeSequence);

            // Assert

            id1.ShouldBe(1);
            id2.ShouldBe(id1);
        }
    }
}
