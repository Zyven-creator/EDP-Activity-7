using MySql.Data.MySqlClient;

namespace StudentInfoSystemAPI;

public class MySqlConnectionFactory
{
    private readonly IConfiguration _configuration;

    public MySqlConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public MySqlConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing DefaultConnection in appsettings.json.");
        }

        return new MySqlConnection(connectionString);
    }
}
