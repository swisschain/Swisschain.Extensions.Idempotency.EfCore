using System;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    public sealed class IdGeneratorModelBuilderOptions
    {
        public IdGeneratorModelBuilderOptions()
        {
            StartNumber = 1;
            IncrementSize = 1;
        }

        public string SequenceName { get; set; }
        public long StartNumber { get; set; }
        public int IncrementSize { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(SequenceName))
            {
                throw new InvalidOperationException("ID generator sequence name should be not empty");
            }

            if (IncrementSize <= 0)
            {
                throw new InvalidOperationException("ID generator increment size should be positive number");
            }
        }
    }
}
