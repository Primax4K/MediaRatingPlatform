using Npgsql;

namespace Domain.ConnectionFactory;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory {
	private readonly string _connString;
	public NpgsqlConnectionFactory(string connString) => _connString = connString;
	public NpgsqlConnection Create() => new NpgsqlConnection(_connString);
}