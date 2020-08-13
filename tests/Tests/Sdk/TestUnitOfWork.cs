using System;
using Swisschain.Extensions.Idempotency.EfCore;
using Tests.Sdk.Persistence;
using Tests.Sdk.Persistence.TestEntities;

namespace Tests.Sdk
{
    public class TestUnitOfWork : UnitOfWorkBase<TestDbContext>
    {
        public TestEntitiesRepository TestEntities { get; private set; }

        protected override void ProvisionRepositories(TestDbContext dbContext)
        {
            TestEntities = new TestEntitiesRepository(dbContext);
        }
    }
}
