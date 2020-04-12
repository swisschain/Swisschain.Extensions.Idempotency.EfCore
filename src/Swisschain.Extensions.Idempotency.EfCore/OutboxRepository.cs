using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Npgsql;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    internal class OutboxRepository<TDbContext> : IOutboxRepository 
        where TDbContext : DbContext, IDbContextWithOutbox
    {
        private readonly ILogger<OutboxRepository<TDbContext>> _logger;
        private readonly Func<TDbContext> _dbContextFactory;

        public OutboxRepository(ILogger<OutboxRepository<TDbContext>> logger,
            Func<TDbContext> dbContextFactory)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Outbox> Open(string requestId, Func<Task<long>> aggregateIdFactory)
        {
            await using var context = _dbContextFactory.Invoke();

            var newEntity = new OutboxEntity
            {
                AggregateId = await aggregateIdFactory(),
                RequestId = requestId
            };

            context.Outbox.Add(newEntity);

            try
            {
                await context.SaveChangesAsync();

                return MapFromEntity(newEntity);
            }
            catch (DbUpdateException e) when (e.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                context.Outbox.Local.Clear();

                var entity = await context
                    .Outbox
                    .FirstAsync(x => x.RequestId == requestId);

                return MapFromEntity(entity);
            }
        }

        public async Task Save(Outbox outbox, OutboxPersistingReason reason)
        {
            if (outbox.IsStored && reason == OutboxPersistingReason.Storing)
            {
                _logger.LogWarning("Outbox already has been stored {@context}", outbox);

                throw new InvalidOperationException($"Outbox {outbox.RequestId} already has been stored");
            }

            if (outbox.IsDispatched && reason == OutboxPersistingReason.Dispatching)
            {
                _logger.LogWarning("Outbox already has been shipped {@context}", outbox);

                throw new InvalidOperationException($"Outbox {outbox.RequestId} already has been shipped");
            }
            
            await using var context = _dbContextFactory.Invoke();

            var entity = MapToEntity(outbox, reason);

            context.Outbox.Update(entity);

            await context.SaveChangesAsync();
        }

        private static OutboxEntity MapToEntity(Outbox outbox, OutboxPersistingReason reason)
        {
            var response = SerializeObject(outbox.Response);
            var commands = SerializeObject(outbox
                .Commands
                ?.Select(SerializeObject)
                .ToArray());
            var events = SerializeObject(outbox
                .Events
                ?.Select(SerializeObject)
                .ToArray());

            return new OutboxEntity
            {
                RequestId = outbox.RequestId,
                AggregateId = outbox.AggregateId,
                IsStored = outbox.IsStored || reason == OutboxPersistingReason.Storing,
                IsDispatched = outbox.IsDispatched|| reason == OutboxPersistingReason.Dispatching,
                Response = response,
                Commands = commands,
                Events = events,
                
            };
        }

        private Outbox MapFromEntity(OutboxEntity entity)
        {
            var response = DeserializeObject(entity.Response);
            var commands = ((string[]) DeserializeObject(entity.Commands))?.Select(DeserializeObject);
            var events = ((string[]) DeserializeObject(entity.Events))?.Select(DeserializeObject);

            return  Outbox.Restore(entity.RequestId,
                entity.AggregateId,
                entity.IsStored,
                entity.IsDispatched,
                response,
                commands,
                events);
        }

        private static string SerializeObject(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            var envelope = new ObjectEnvelope
            {
                Type = obj.GetType().FullName,
                Body = JsonConvert.SerializeObject(obj, new ProtoMessageJsonConverter())
            };

            return JsonConvert.SerializeObject(envelope);
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

        private static Type FindType(string fullName)
        {
            // TODO: Cache
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName != null && t.FullName.Equals(fullName)) ?? Type.GetType(fullName);
        }
        
        private class ObjectEnvelope
        {
            public string Type { get; set; }
            public string Body { get; set; }
        }

        private class ProtoMessageJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(IMessage).IsAssignableFrom(objectType);
            }
            public override object ReadJson(JsonReader reader,
                Type objectType, object existingValue,
                JsonSerializer serializer)
            {
                // The only way to find where this json object begins and ends is by
                // reading it in as a generic ExpandoObject.
                // Read an entire object from the reader.
                var converter = new ExpandoObjectConverter();
                var o = converter.ReadJson(reader, objectType, existingValue, serializer);
                // Convert it back to json text.
                var text = JsonConvert.SerializeObject(o);
                // And let protobuf's parser parse the text.
                var message = (IMessage)Activator.CreateInstance(objectType);
                return JsonParser.Default.Parse(text, message.Descriptor);
            }

            /// <summary>
            /// Writes the json representation of a Protocol Message.
            /// </summary>
            public override void WriteJson(JsonWriter writer, object value,
                JsonSerializer serializer)
            {
                // Let Protobuf's JsonFormatter do all the work.
                writer.WriteRawValue(JsonFormatter.Default.Format((IMessage)value));
            }
        }

    }
}
