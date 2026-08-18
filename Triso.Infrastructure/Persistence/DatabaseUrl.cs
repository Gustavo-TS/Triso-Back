using Npgsql;

namespace Triso.Infrastructure.Persistence;

public static class DatabaseUrl
{
    public static string ToConnectionString(string value, string? databaseName = null)
    {
        NpgsqlConnectionStringBuilder connection;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == "postgres" || uri.Scheme == "postgresql"))
        {
            var credentials = uri.UserInfo.Split(':', 2);
            connection = new NpgsqlConnectionStringBuilder { Host = uri.Host, Port = uri.IsDefaultPort ? 5432 : uri.Port, Database = uri.AbsolutePath.Trim('/'), Username = Uri.UnescapeDataString(credentials[0]), Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : "", SslMode = SslMode.Require, Pooling = true, MaxPoolSize = 50, Timeout = 10, CommandTimeout = 15 };
        }
        else
        {
            connection = new NpgsqlConnectionStringBuilder(value);
        }

        if (!string.IsNullOrWhiteSpace(databaseName)) connection.Database = databaseName.Trim();
        return connection.ConnectionString;
    }
}
