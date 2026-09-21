using Npgsql;

namespace GvGPoc.Data;


public interface IDbConnectionFactory
{
    NpgsqlConnection Create();
}

public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Tacnet")
            ?? throw new InvalidOperationException(
                "Connection string 'Tacnet' is not configured. Add it under ConnectionStrings in appsettings.json " +
                "or set it via the ConnectionStrings__Tacnet environment variable.");
    }

    public NpgsqlConnection Create() => new(_connectionString);
}
