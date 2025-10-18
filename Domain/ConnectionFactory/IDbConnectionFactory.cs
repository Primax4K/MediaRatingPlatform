using Npgsql;

namespace Domain.ConnectionFactory;

public interface IDbConnectionFactory {
	NpgsqlConnection Create();
}