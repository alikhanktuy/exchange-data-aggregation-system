using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Infrastructure.Persistence;

/// <summary>Creates PostgreSQL connections.</summary>
public sealed class DbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>Creates a connection factory from configuration.</summary>
    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
    }

    /// <summary>Opens a PostgreSQL connection.</summary>
    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
