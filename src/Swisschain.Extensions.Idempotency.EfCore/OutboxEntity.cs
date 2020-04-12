namespace Swisschain.Extensions.Idempotency.EfCore
{
    public class OutboxEntity
    {
        public long AggregateId { get; set; }
        public string RequestId { get; set; }
        public string Response { get; set; }
        public string Events { get; set; }
        public string Commands { get; set; }
        public bool IsStored { get; set; }
        public bool IsDispatched { get; set; }
    }
}
