using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Swisschain.Extensions.Idempotency.EfCore.Serialization;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    internal class OutboxReadRepository<TDbContext> : IOutboxReadRepository
        where TDbContext : DbContext, IDbContextWithOutbox
    {
        private readonly ILogger<OutboxReadRepository<TDbContext>> _logger;
        private readonly Func<TDbContext> _dbContextFactory;
        private readonly OutboxDeserializerOptions _deserializerOptions;

        public OutboxReadRepository(ILogger<OutboxReadRepository<TDbContext>> logger, 
            Func<TDbContext> dbContextFactory,
            OutboxDeserializerOptions deserializerOptions)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
            _deserializerOptions = deserializerOptions;
        }

        public async Task<Outbox> GetOrDefault(string idempotencyId)
        {
            await using var context = _dbContextFactory.Invoke();

            var entity = await context.Outbox.FindAsync(idempotencyId);

            return entity != null
                ? MapFromEntity(entity)
                : null;
        }
        
        private Outbox MapFromEntity(OutboxEntity entity)
        {
            var response = DeserializeObject(entity.Response);
            var commands = ((string[]) DeserializeObject(entity.Commands))?.Select(DeserializeObject);
            var events = ((string[]) DeserializeObject(entity.Events))?.Select(DeserializeObject);

            return  Outbox.Restore(
                entity.IdempotencyId,
                entity.IsDispatched,
                response,
                commands,
                events);
        }

        private object DeserializeObject(string value)
        {
            if (value == null)
            {
                return null;
            }

            var envelope = JsonConvert.DeserializeObject<ObjectEnvelope>(value);
            var type = FindType(envelope.Type);

            return JsonConvert.DeserializeObject(envelope.Body, type, new ProtoMessageJsonConverter());
        }

        private Type FindType(string fullName)
        {
            return TypesCache.Instance.GetOrAdd(fullName,
                x =>
                {
                    foreach (var assembly in _deserializerOptions.Assemblies)
                    {
                        try
                        {
                            var type = assembly.GetType(fullName, false);

                            if (type != null)
                            {
                                return type;
                            }
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            _logger.LogWarning(ex,
                                "Failed to load assembly {@assembly} while finding the type {@type}",
                                assembly.GetName().FullName,
                                fullName);

                            throw new InvalidOperationException($"Failed to load assembly {assembly} while finding the type {fullName}",
                                ex);
                        }
                    }

                    _logger.LogWarning("Type {@type} has not been found", fullName);

                    throw new InvalidOperationException($"Type {fullName} not found");
                });
        }
    }
}
