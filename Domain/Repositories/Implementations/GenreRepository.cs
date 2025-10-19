namespace Domain.Repositories.Implementations;

public sealed class GenreRepository : IGenreRepository {
	private readonly IDbConnectionFactory _factory;
	public GenreRepository(IDbConnectionFactory factory) => _factory = factory;

	public async Task<Genre?> GetByIdAsync(Guid id, CancellationToken ct = default) {
		const string sql = "select id, name from genre where id=@id;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		return await c.QueryFirstOrDefaultAsync<Genre>(new CommandDefinition(sql, new { id }, cancellationToken: ct));
	}

	public async Task<IReadOnlyList<Genre>> ListAsync(int skip = 0, int take = 100, CancellationToken ct = default) {
		const string sql = "select id, name from genre order by name offset @skip limit @take;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		var rows = await c.QueryAsync<Genre>(new CommandDefinition(sql, new { skip, take }, cancellationToken: ct));
		return rows.AsList();
	}

	public async Task<Genre?> GetByNameAsync(string name, CancellationToken ct = default) {
		const string sql = "select id, name from genre where lower(name)=lower(@n) limit 1;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		return await c.QueryFirstOrDefaultAsync<Genre>(new CommandDefinition(sql, new { n = name },
			cancellationToken: ct));
	}

	public async Task<Genre> CreateAsync(Genre e, CancellationToken ct = default) {
		const string sql =
			"insert into genre (id, name) values (coalesce(@Id, gen_random_uuid()), @Name) returning id, name;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		return await c.QuerySingleAsync<Genre>(new CommandDefinition(sql, e, cancellationToken: ct));
	}

	public async Task<bool> UpdateAsync(Genre e, CancellationToken ct = default) {
		const string sql = "update genre set name=@Name where id=@Id;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		var n = await c.ExecuteAsync(new CommandDefinition(sql, e, cancellationToken: ct));
		return n == 1;
	}

	public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) {
		const string sql = "delete from genre where id=@id;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		var n = await c.ExecuteAsync(new CommandDefinition(sql, new { id }, cancellationToken: ct));
		return n == 1;
	}
}