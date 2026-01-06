using System.Data.Common;

namespace Domain.Repositories.Implementations {
	public sealed class GenreRepository : DbHelper, IGenreRepository {
		private readonly IDbConnectionFactory _factory;
		public GenreRepository(IDbConnectionFactory factory) => _factory = factory;

		public async Task<Genre?> GetByIdAsync(Guid id, CancellationToken ct = default) {
			const string sql = "select id, name from genre where id=@id;";

			await using var c = _factory.Create();
			await OpenAsync(c, ct);

			await using var cmd = Command(c, sql);
			Param(cmd, "@id", id);

			await using var r = await cmd.ExecuteReaderAsync(ct);
			if (!await r.ReadAsync(ct)) return null;

			return Map(r);
		}

		public async Task<IReadOnlyList<Genre>>
			ListAsync(int skip = 0, int take = 100, CancellationToken ct = default) {
			const string sql = "select id, name from genre order by name offset @skip limit @take;";

			await using var c = _factory.Create();
			await OpenAsync(c, ct);

			await using var cmd = Command(c, sql);
			Param(cmd, "@skip", skip);
			Param(cmd, "@take", take);

			var list = new List<Genre>(take);
			await using var r = await cmd.ExecuteReaderAsync(ct);
			while (await r.ReadAsync(ct))
				list.Add(Map(r));

			return list;
		}

		public async Task<Genre?> GetByNameAsync(string name, CancellationToken ct = default) {
			const string sql = "select id, name from genre where lower(name)=lower(@n) limit 1;";

			await using var c = _factory.Create();
			await OpenAsync(c, ct);

			await using var cmd = Command(c, sql);
			Param(cmd, "@n", name);

			await using var r = await cmd.ExecuteReaderAsync(ct);
			if (!await r.ReadAsync(ct)) return null;

			return Map(r);
		}

		public async Task<Genre> CreateAsync(Genre e, CancellationToken ct = default) {
			const string sql =
				"insert into genre (id, name) values (coalesce(@Id, gen_random_uuid()), @Name) returning id, name;";

			await using var c = _factory.Create();
			await OpenAsync(c, ct);

			await using var cmd = Command(c, sql);
			Param(cmd, "@Id", (object?)e.Id ?? DBNull.Value);
			Param(cmd, "@Name", e.Name);

			await using var r = await cmd.ExecuteReaderAsync(ct);
			if (!await r.ReadAsync(ct)) throw new InvalidOperationException();

			return Map(r);
		}

		public async Task<bool> UpdateAsync(Genre e, CancellationToken ct = default) {
			const string sql = "update genre set name=@Name where id=@Id;";

			await using var c = _factory.Create();
			await OpenAsync(c, ct);

			await using var cmd = Command(c, sql);
			Param(cmd, "@Id", e.Id);
			Param(cmd, "@Name", e.Name);

			return await cmd.ExecuteNonQueryAsync(ct) == 1;
		}

		public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) {
			const string sql = "delete from genre where id=@id;";

			await using var c = _factory.Create();
			await OpenAsync(c, ct);

			await using var cmd = Command(c, sql);
			Param(cmd, "@id", id);

			return await cmd.ExecuteNonQueryAsync(ct) == 1;
		}

		private static Genre Map(DbDataReader r) => new() {
			Id = r.GetGuid(r.GetOrdinal("id")),
			Name = r.GetString(r.GetOrdinal("name"))
		};
	}
}