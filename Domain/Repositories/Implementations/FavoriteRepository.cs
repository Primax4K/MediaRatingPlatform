namespace Domain.Repositories.Implementations;

public sealed class FavoriteRepository : DbHelper, IFavoriteRepository {
	private readonly IDbConnectionFactory _factory;
	public FavoriteRepository(IDbConnectionFactory factory) => _factory = factory;

	public async Task<bool> AddAsync(Guid userId, Guid mediaId, CancellationToken ct = default) {
		const string sql =
			"insert into favorite (user_id, media_id, created_at) values (@u, @m, now()) on conflict do nothing;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@u", userId);
		Param(cmd, "@m", mediaId);

		await cmd.ExecuteNonQueryAsync(ct);
		return true;
	}

	public async Task<bool> RemoveAsync(Guid userId, Guid mediaId, CancellationToken ct = default) {
		const string sql = "delete from favorite where user_id=@u and media_id=@m;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@u", userId);
		Param(cmd, "@m", mediaId);

		return await cmd.ExecuteNonQueryAsync(ct) == 1;
	}
}