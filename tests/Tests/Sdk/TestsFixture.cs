using Swisschain.Extensions.Testing;

namespace Tests.Sdk
{
    public class TestsFixture : PostgresFixture
    {
        public TestsFixture() :
            base("idempotency-tests-pg")
        {
        }
    }
}
