using Microsoft.EntityFrameworkCore;
using Swisschain.Extensions.Idempotency.EfCore;

namespace Tests.Sdk.DbContexts
{
    public class TestDbContextWithDefaultOptions : TestDbContextBase
    {
        public TestDbContextWithDefaultOptions(DbContextOptions<TestDbContextWithDefaultOptions> options) :
            base(options)
        {
        }

        public const string Schema = "default_options";
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);

            modelBuilder.BuildIdempotency();

            base.OnModelCreating(modelBuilder);
        }
    }
}
