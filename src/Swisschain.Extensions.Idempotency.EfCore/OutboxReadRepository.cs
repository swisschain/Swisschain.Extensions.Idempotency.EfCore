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

        public OutboxReadRepository(ILogger<OutboxReadRepository<TDbContext>> logger, 
            Func<TDbContext> dbContextFactory)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
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

            if (type == null)
            {
                _logger.LogWarning("Type {@type} has not been found", envelope.Type);

                throw new InvalidOperationException($"Type {envelope.Type} not found");
            }

            return JsonConvert.DeserializeObject(envelope.Body, type, new ProtoMessageJsonConverter());
        }

        private Type FindType(string fullName)
        {
            // TODO: Cache
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        _logger.LogWarning(ex, "Failed to load assembly {@assembly} has not been found", a.GetName().FullName);

                        return Enumerable.Empty<Type>();
                    }
                })
                .FirstOrDefault(t => t.FullName != null && t.FullName.Equals(fullName)) ?? Type.GetType(fullName);
        }
    }
}
