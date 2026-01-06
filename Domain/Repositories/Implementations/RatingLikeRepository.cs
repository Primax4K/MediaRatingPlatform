namespace Domain.Repositories.Implementations;

public sealed class RatingLikeRepository : DbHelper, IRatingLikeRepository {
	private readonly IDbConnectionFactory _factory;
	public RatingLikeRepository(IDbConnectionFactory factory) => _factory = factory;

	public async Task<bool> AddAsync(Guid ratingId, Guid userId, CancellationToken ct = default) {
		const string sql =
			"insert into rating_like (rating_id, user_id, created_at) values (@r, @u, now()) on conflict do nothing;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@r", ratingId);
		Param(cmd, "@u", userId);

		await cmd.ExecuteNonQueryAsync(ct);
		return true;
	}

	public async Task<bool> RemoveAsync(Guid ratingId, Guid userId, CancellationToken ct = default) {
		const string sql = "delete from rating_like where rating_id=@r and user_id=@u;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@r", ratingId);
		Param(cmd, "@u", userId);

		return await cmd.ExecuteNonQueryAsync(ct) == 1;
	}

	public async Task<bool> ExistsAsync(Guid ratingId, Guid userId, CancellationToken ct = default) {
		const string sql = "select count(1) from rating_like where rating_id=@r and user_id=@u;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@r", ratingId);
		Param(cmd, "@u", userId);

		var result = await cmd.ExecuteScalarAsync(ct);
		if (result == null || result == DBNull.Value)
			return false;

		return Convert.ToInt32(result) > 0;
	}
}