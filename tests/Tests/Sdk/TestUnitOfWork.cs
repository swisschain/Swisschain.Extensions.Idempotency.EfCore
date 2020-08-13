using System;
using Swisschain.Extensions.Idempotency.EfCore;
using Tests.Sdk.DbContexts;

namespace Tests.Sdk
{
    public class TestUnitOfWork : UnitOfWorkBase<TestDbContextWithDefaultOptions>, IDisposable
    {
        public TestEntitiesRepository TestEntities { get; private set; }

        protected override void ProvisionRepositories(TestDbContextWithDefaultOptions dbContext)
        {
            TestEntities = new TestEntitiesRepository(dbContext);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
