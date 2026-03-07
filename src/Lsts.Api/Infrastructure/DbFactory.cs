using Oracle.ManagedDataAccess.Client;

namespace Lsts.Api.Infrastructure;

public sealed class DbFactory
{
    private readonly string _cs;

    public DbFactory(IConfiguration config)
    {
        _cs = config.GetConnectionString("Oracle")
              ?? throw new InvalidOperationException("Connection string 'Oracle' is missing.");
    }

    public async Task<OracleConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        var conn = new OracleConnection(_cs);
        await conn.OpenAsync(ct);
        return conn;
    }
}
