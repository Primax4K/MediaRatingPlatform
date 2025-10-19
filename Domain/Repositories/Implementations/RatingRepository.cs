namespace Domain.Repositories.Implementations;

public sealed class RatingRepository : IRatingRepository {
	private readonly IDbConnectionFactory _factory;
	public RatingRepository(IDbConnectionFactory factory) => _factory = factory;

	public async Task<Rating?> GetByIdAsync(Guid id, CancellationToken ct = default) {
		const string sql = @"select id, media_id, user_id, stars, comment, comment_confirmed, created_at, updated_at
                             from rating where id=@id;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		return await c.QueryFirstOrDefaultAsync<Rating>(new CommandDefinition(sql, new { id }, cancellationToken: ct));
	}

	public async Task<IReadOnlyList<Rating>> ListAsync(int skip = 0, int take = 100, CancellationToken ct = default) {
		const string sql = @"select id, media_id, user_id, stars, comment, comment_confirmed, created_at, updated_at
                             from rating order by created_at desc offset @skip limit @take;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		var rows = await c.QueryAsync<Rating>(new CommandDefinition(sql, new { skip, take }, cancellationToken: ct));
		return rows.AsList();
	}

	public async Task<Rating?> GetByUserAndMediaAsync(Guid userId, Guid mediaId, CancellationToken ct = default) {
		const string sql = @"select id, media_id, user_id, stars, comment, comment_confirmed, created_at, updated_at
                             from rating where user_id=@u and media_id=@m limit 1;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		return await c.QueryFirstOrDefaultAsync<Rating>(new CommandDefinition(sql, new { u = userId, m = mediaId },
			cancellationToken: ct));
	}

	public async Task<Rating> CreateAsync(Rating e, CancellationToken ct = default) {
		const string sql =
			@"insert into rating (id, media_id, user_id, stars, comment, comment_confirmed, created_at, updated_at)
                             values (coalesce(@Id, gen_random_uuid()), @MediaId, @UserId, @Stars, @Comment, @CommentConfirmed, now(), now())
                             returning id, media_id, user_id, stars, comment, comment_confirmed, created_at, updated_at;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		return await c.QuerySingleAsync<Rating>(new CommandDefinition(sql, e, cancellationToken: ct));
	}

	public async Task<bool> UpdateAsync(Rating e, CancellationToken ct = default) {
		const string sql =
			@"update rating set stars=@Stars, comment=@Comment, comment_confirmed=@CommentConfirmed, updated_at=now()
                             where id=@Id;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		var n = await c.ExecuteAsync(new CommandDefinition(sql, e, cancellationToken: ct));
		return n == 1;
	}

	public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) {
		const string sql = "delete from rating where id=@id;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		var n = await c.ExecuteAsync(new CommandDefinition(sql, new { id }, cancellationToken: ct));
		return n == 1;
	}
}