namespace Swisschain.Extensions.Idempotency.EfCore
{
    public sealed class IdempotencyEfCoreOptions
    {
        internal IdempotencyEfCoreOptions()
        {
            OutboxDeserializer = new OutboxDeserializerOptions();
        }

        public OutboxDeserializerOptions OutboxDeserializer { get; }
    }
}
