namespace Swisschain.Extensions.Idempotency.EfCore
{
    public class IdGeneratorEntity
    {
        public string IdempotencyId { get; set; }
        public long Value { get; set; }
    }
}
