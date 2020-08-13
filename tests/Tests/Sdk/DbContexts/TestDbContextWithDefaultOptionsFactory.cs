using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tests.Sdk.DbContexts
{
    public class TestDbContextWithDefaultOptionsFactory : IDesignTimeDbContextFactory<TestDbContextWithDefaultOptions>
    {
        public TestDbContextWithDefaultOptions CreateDbContext(string[] args)
        {
            var connString = Environment.GetEnvironmentVariable("POSTGRE_SQL_CONNECTION_STRING");

            var optionsBuilder = new DbContextOptionsBuilder<TestDbContextWithDefaultOptions>();
            optionsBuilder.UseNpgsql(connString);

            return new TestDbContextWithDefaultOptions(optionsBuilder.Options);
        }
    }
}
