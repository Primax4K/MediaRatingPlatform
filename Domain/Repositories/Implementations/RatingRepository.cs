namespace Domain.Repositories.Implementations;

public sealed class RatingRepository : IRatingRepository {
    private readonly IDbConnectionFactory _factory;
    public RatingRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<Rating?> GetByIdAsync(Guid id, CancellationToken ct = default) {
        const string sql = @"select id,
		                            mediaid as MediaId,
		                            userid as UserId,
		                            stars,
		                            comment,
		                            commentconfirmed as CommentConfirmed,
		                            createdat as CreatedAt,
		                            updatedat as UpdatedAt
		                     from rating where id=@id;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        return await c.QueryFirstOrDefaultAsync<Rating>(new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Rating>> ListAsync(int skip = 0, int take = 100, CancellationToken ct = default) {
        const string sql = @"select id,
		                            mediaid as MediaId,
		                            userid as UserId,
		                            stars,
		                            comment,
		                            commentconfirmed as CommentConfirmed,
		                            createdat as CreatedAt,
		                            updatedat as UpdatedAt
		                     from rating
		                     order by createdat desc
		                     offset @skip limit @take;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        var rows = await c.QueryAsync<Rating>(new CommandDefinition(sql, new { skip, take }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<Rating?> GetByUserAndMediaAsync(Guid userId, Guid mediaId, CancellationToken ct = default) {
        const string sql = @"select id,
		                            mediaid as MediaId,
		                            userid as UserId,
		                            stars,
		                            comment,
		                            commentconfirmed as CommentConfirmed,
		                            createdat as CreatedAt,
		                            updatedat as UpdatedAt
		                     from rating
		                     where userid=@u and mediaid=@m
		                     limit 1;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        return await c.QueryFirstOrDefaultAsync<Rating>(new CommandDefinition(sql, new { u = userId, m = mediaId },
            cancellationToken: ct));
    }

    public async Task<Rating> CreateAsync(Rating e, CancellationToken ct = default) {
        const string sql =
            @"insert into rating (id, mediaid, userid, stars, comment, commentconfirmed, createdat, updatedat)
			  values (coalesce(@Id, gen_random_uuid()), @MediaId, @UserId, @Stars, @Comment, @CommentConfirmed, now(), now())
			  returning id,
			            mediaid as MediaId,
			            userid as UserId,
			            stars,
			            comment,
			            commentconfirmed as CommentConfirmed,
			            createdat as CreatedAt,
			            updatedat as UpdatedAt;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        return await c.QuerySingleAsync<Rating>(new CommandDefinition(sql, e, cancellationToken: ct));
    }

    public async Task<bool> UpdateAsync(Rating e, CancellationToken ct = default) {
        const string sql =
            @"update rating
			  set stars=@Stars,
			      comment=@Comment,
			      commentconfirmed=@CommentConfirmed,
			      updatedat=now()
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