using System.Threading.Tasks;
using Tests.Sdk.DbContexts;

namespace Tests.Sdk
{
    public class TestEntitiesRepository
    {
        private readonly TestDbContextBase _dbContext;

        public TestEntitiesRepository(TestDbContextBase dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Add(long id, string name)
        {
            _dbContext.TestEntities.Add(new TestEntity
            {
                Id = id,
                Name = name
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task Update(long id, string name)
        {
            _dbContext.TestEntities.Update(new TestEntity
            {
                Id = id,
                Name = name
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task<string> GetNameOrDefault(long id)
        {
            var entity = await _dbContext.TestEntities.FindAsync(id);

            return entity?.Name;
        }
    }
}
