using Xunit;

namespace Tests.Sdk
{
    [CollectionDefinition(nameof(PersistenceTests))]
    public sealed class PersistenceTestsCollectionDefinition : ICollectionFixture<PersistenceFixture>
    {
    }
}