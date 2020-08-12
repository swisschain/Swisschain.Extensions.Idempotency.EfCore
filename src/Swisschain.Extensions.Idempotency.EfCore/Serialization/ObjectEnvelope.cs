namespace Swisschain.Extensions.Idempotency.EfCore.Serialization
{
    internal class ObjectEnvelope
    {
        public string Type { get; set; }
        public string Body { get; set; }
    }
}
