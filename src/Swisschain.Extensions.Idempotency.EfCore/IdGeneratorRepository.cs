using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    internal class IdGeneratorRepository<TDbContext> : IIdGeneratorRepository
        where TDbContext : DbContext, IDbContextWithIdGenerator
    {
        private readonly Func<TDbContext> _dbContextFactory;

        public IdGeneratorRepository(Func<TDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<long> GetId(string idempotencyId, string generatorName)
        {
            await using var context = _dbContextFactory.Invoke();

            var defaultSchema = context.Model.GetDefaultSchema();
            var nextValue = await GetNextValue(defaultSchema, generatorName, context);

            // TODO: Cache it
            var tableName = context.Model.FindEntityType(typeof(IdGeneratorEntity)).GetTableName();
            var query = $@"
                with new_value as 
                (
                    insert into {defaultSchema}.{tableName} (""IdempotencyId"", ""Value"") values (@idempotencyId, @nextValue)
                    on conflict(""IdempotencyId"") do nothing
                    returning ""Value""
                )
                select ""Value"" from new_value
                union
                select ""Value"" from {defaultSchema}.{tableName} where ""IdempotencyId"" = @idempotencyId";

            var connection = context.Database.GetDbConnection();

            await using var command = connection.CreateCommand();

            command.CommandText = query;
            
            var idempotencyParameter = command.CreateParameter();
            idempotencyParameter.ParameterName = "@idempotencyId";
            idempotencyParameter.DbType = DbType.String;
            idempotencyParameter.Value = idempotencyId;

            var valueParameter = command.CreateParameter();
            valueParameter.ParameterName = "@nextValue";
            valueParameter.DbType = DbType.Int64;
            valueParameter.Value = nextValue;

            await using var reader = await command.ExecuteReaderAsync();

            if (!reader.HasRows)
            {
                throw new InvalidOperationException("Expected data not found");
            }

            return reader.GetInt64(0);
        }

        private static async Task<long> GetNextValue(string defaultSchema, string generatorName, TDbContext context)
        {
            await using var cmd = context.Database.GetDbConnection().CreateCommand();

            var effectiveGeneratorName = generatorName.Contains('.')
                ? generatorName
                : $"{defaultSchema}.{generatorName}";

            cmd.CommandText = $"select nextval('{effectiveGeneratorName}')";

            if (cmd.Connection.State != ConnectionState.Open)
            {
                cmd.Connection.Open();
            }

            return (long) cmd.ExecuteScalar();
        }
    }
}
