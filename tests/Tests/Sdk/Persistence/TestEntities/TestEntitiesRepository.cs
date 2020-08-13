using System.Threading.Tasks;

namespace Tests.Sdk.Persistence.TestEntities
{
    public class TestEntitiesRepository
    {
        private readonly TestDbContext _dbContext;

        public TestEntitiesRepository(TestDbContext dbContext)
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
