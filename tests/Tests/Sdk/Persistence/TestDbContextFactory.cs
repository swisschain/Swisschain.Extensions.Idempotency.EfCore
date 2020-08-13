using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tests.Sdk.Persistence
{
    public class TestDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
    {
        public TestDbContext CreateDbContext(string[] args)
        {
            var connString = Environment.GetEnvironmentVariable("POSTGRE_SQL_CONNECTION_STRING");

            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
            optionsBuilder.UseNpgsql(connString);

            return new TestDbContext(optionsBuilder.Options);
        }
    }
}
