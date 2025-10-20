namespace Domain.Repositories.Implementations;

public sealed class RatingLikeRepository : IRatingLikeRepository {
    private readonly IDbConnectionFactory _factory;
    public RatingLikeRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<bool> AddAsync(Guid ratingId, Guid userId, CancellationToken ct = default) {
        const string sql =
            "insert into ratinglike (ratingid, userid, createdat) values (@r, @u, now()) on conflict do nothing;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        await c.ExecuteAsync(new CommandDefinition(sql, new { r = ratingId, u = userId }, cancellationToken: ct));
        return true;
    }

    public async Task<bool> RemoveAsync(Guid ratingId, Guid userId, CancellationToken ct = default) {
        const string sql = "delete from ratinglike where ratingid=@r and userid=@u;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        var n = await c.ExecuteAsync(
            new CommandDefinition(sql, new { r = ratingId, u = userId }, cancellationToken: ct));
        return n == 1;
    }
}