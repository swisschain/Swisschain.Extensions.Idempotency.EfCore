using System;
using Microsoft.EntityFrameworkCore;

namespace Swisschain.Extensions.Idempotency.EfCore
{
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder BuildIdempotency(this ModelBuilder modelBuilder, 
            Action<IdempotencyModelBuilderOptions> optionsBuilder = null)
        {
            var options = new IdempotencyModelBuilderOptions();

            optionsBuilder?.Invoke(options);

            options.Validate();

            modelBuilder.Entity<OutboxEntity>()
                .ToTable(options.OutboxTableName)
                .HasKey(x => x.IdempotencyId);

            modelBuilder.Entity<IdGeneratorEntity>()
                .ToTable(options.IdGeneratorTableName)
                .HasKey(x => x.IdempotencyId);

            foreach (var generatorOptions in options.IdGenerators)
            {
                modelBuilder.HasSequence<long>(generatorOptions.SequenceName)
                    .StartsAt(generatorOptions.StartNumber)
                    .IncrementsBy(generatorOptions.IncrementSize);
            }

            return modelBuilder;
        }
    }
}
