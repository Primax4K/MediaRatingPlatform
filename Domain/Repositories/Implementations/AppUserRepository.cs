using Dapper;
using Domain.ConnectionFactory;
using Domain.Repositories.Interfaces;
using Model.Entities;

namespace Domain.Repositories.Implementations;

public sealed class AppUserRepository : IAppUserRepository {
	private readonly IDbConnectionFactory _factory;
	public AppUserRepository(IDbConnectionFactory factory) => _factory = factory;

	public async Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default) {
		const string sql = "select id, username, password_hash, created_at from app_user where id = @id;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		return await c.QueryFirstOrDefaultAsync<AppUser>(new CommandDefinition(sql, new { id }, cancellationToken: ct));
	}

	public async Task<IReadOnlyList<AppUser>> ListAsync(int skip = 0, int take = 100, CancellationToken ct = default) {
		const string sql =
			"select id, username, password_hash, created_at from app_user order by created_at desc offset @skip limit @take;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		var rows = await c.QueryAsync<AppUser>(new CommandDefinition(sql, new { skip, take }, cancellationToken: ct));
		return rows.AsList();
	}

	public async Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default) {
		const string sql =
			"select id, username, password_hash, created_at from app_user where lower(username)=lower(@u) limit 1;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		return await c.QueryFirstOrDefaultAsync<AppUser>(new CommandDefinition(sql, new { u = username },
			cancellationToken: ct));
	}

	public async Task<AppUser> CreateAsync(AppUser e, CancellationToken ct = default) {
		const string sql = @"insert into app_user (id, username, password_hash, created_at)
                             values (coalesce(@Id, gen_random_uuid()), @Username, @PasswordHash, now())
                             returning id, username, password_hash, created_at;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		return await c.QuerySingleAsync<AppUser>(new CommandDefinition(sql, e, cancellationToken: ct));
	}

	public async Task<bool> UpdateAsync(AppUser e, CancellationToken ct = default) {
		const string sql = "update app_user set username=@Username, password_hash=@PasswordHash where id=@Id;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		var n = await c.ExecuteAsync(new CommandDefinition(sql, e, cancellationToken: ct));
		return n == 1;
	}

	public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) {
		const string sql = "delete from app_user where id=@id;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		var n = await c.ExecuteAsync(new CommandDefinition(sql, new { id }, cancellationToken: ct));
		return n == 1;
	}
}