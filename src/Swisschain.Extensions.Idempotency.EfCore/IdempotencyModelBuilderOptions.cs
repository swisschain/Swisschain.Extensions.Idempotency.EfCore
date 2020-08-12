using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    public sealed class IdempotencyModelBuilderOptions
    {
        private readonly List<IdGeneratorModelBuilderOptions> _idGeneratorModelBuilderOptionsList;

        public IdempotencyModelBuilderOptions()
        {
            OutboxTableName = "outbox";
            IdGeneratorTableName = "id_generator";

            _idGeneratorModelBuilderOptionsList = new List<IdGeneratorModelBuilderOptions>();
        }

        public string OutboxTableName { get; set; }
        public string IdGeneratorTableName { get; set; }
        public IReadOnlyCollection<IdGeneratorModelBuilderOptions> IdGenerators => new ReadOnlyCollection<IdGeneratorModelBuilderOptions>(_idGeneratorModelBuilderOptionsList);

        public IdempotencyModelBuilderOptions AddIdGenerator(string name,
            long startNumber = 1,
            int incrementSize = 1)
        {
            return AddIdGenerator(x =>
            {
                x.SequenceName = name;
                x.StartNumber = startNumber;
                x.IncrementSize = incrementSize;
            });
        }

        public IdempotencyModelBuilderOptions AddIdGenerator(Action<IdGeneratorModelBuilderOptions> configure)
        {
            var options = new IdGeneratorModelBuilderOptions();

            configure.Invoke(options);

            options.Validate();

            _idGeneratorModelBuilderOptionsList.Add(options);

            return this;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(OutboxTableName))
            {
                throw new InvalidOperationException("Outbox table name should be not empty");
            }
        }
    }
}
