using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

        public async Task RiseTimeoutException()
        {
            await using var connection = (NpgsqlConnection)_dbContext.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }


            await using var command = connection.CreateCommand();

            command.CommandTimeout = 1;
            command.CommandText = "SELECT pg_sleep(2)";

            await command.ExecuteNonQueryAsync();
        }
    }
}
