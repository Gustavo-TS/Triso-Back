using Npgsql;
using Triso.Infrastructure.Persistence;

namespace Triso.Tests.Persistence;

public sealed class DatabaseUrlTests
{
    [Theory]
    [InlineData("postgresql://user:password@localhost/neondb", "trisostudio")]
    [InlineData("Host=localhost;Database=neondb;Username=user;Password=password", "trisostudio")]
    public void Database_name_override_works_for_supported_formats(string value, string expected)
    {
        var result = new NpgsqlConnectionStringBuilder(DatabaseUrl.ToConnectionString(value, expected));

        Assert.Equal(expected, result.Database);
    }
}
