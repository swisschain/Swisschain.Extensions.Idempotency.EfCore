using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    internal class IdGeneratorRepository<TDbContext> : IIdGeneratorRepository
        where TDbContext : DbContext, IDbContextWithIdGenerator
    {
        private static readonly object _dbSchemaCachelock = new object();
        private static DbSchema _dbSchemaCache;

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

            var dbSchema = GetDbSchema(context);
            var query = $@"
                with new_value as 
                (
                    insert into {defaultSchema}.{dbSchema.TableName} (""{dbSchema.IdempotencyIdColumnName}"", ""{dbSchema.ValueColumnName}"") values (@idempotencyId, @nextValue)
                    on conflict(""{dbSchema.IdempotencyIdColumnName}"") do nothing
                    returning ""{dbSchema.ValueColumnName}""
                )
                select ""{dbSchema.ValueColumnName}"" from new_value
                union
                select ""{dbSchema.ValueColumnName}"" from {defaultSchema}.{dbSchema.TableName} where ""{dbSchema.IdempotencyIdColumnName}"" = @idempotencyId";

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

            command.Parameters.Add(idempotencyParameter);
            command.Parameters.Add(valueParameter);

            await using var reader = await command.ExecuteReaderAsync();

            if (!reader.HasRows)
            {
                throw new InvalidOperationException("Expected data not found");
            }

            await reader.ReadAsync();

            return reader.GetInt64(0);
        }

        private static DbSchema GetDbSchema(TDbContext context)
        {
            // Double-checked locking

            if (_dbSchemaCache != null)
            {
                return _dbSchemaCache;
            }

            lock (_dbSchemaCachelock)
            {
                if (_dbSchemaCache != null)
                {
                    return _dbSchemaCache;
                }

                var entityType = context.Model.FindEntityType(typeof(IdGeneratorEntity));
                var tableName = entityType.GetTableName();
                var schema = entityType.GetSchema();
                var storeObjectIdentifier = StoreObjectIdentifier.Table(tableName, schema);
                var idempotencyIdColumnName = entityType.FindProperty(nameof(IdGeneratorEntity.IdempotencyId)).GetColumnName(storeObjectIdentifier);
                var valueColumnName = entityType.FindProperty(nameof(IdGeneratorEntity.Value)).GetColumnName(storeObjectIdentifier);

                _dbSchemaCache = new DbSchema
                {
                    TableName = tableName,
                    IdempotencyIdColumnName = idempotencyIdColumnName,
                    ValueColumnName = valueColumnName
                };

                return _dbSchemaCache;
            }
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

            return (long)cmd.ExecuteScalar();
        }

        private class DbSchema
        {
            public string TableName { get; init; }
            public string IdempotencyIdColumnName { get; init; }
            public string ValueColumnName { get; init; }
        }
    }
}
