namespace Domain.Repositories.Implementations;

public sealed class AppUserRepository : IAppUserRepository {
    private readonly IDbConnectionFactory _factory;
    public AppUserRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default) {
        const string sql =
            "select id, username, passwordhash as PasswordHash, createdat as CreatedAt from appuser where id = @id;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        return await c.QueryFirstOrDefaultAsync<AppUser>(new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<AppUser>> ListAsync(int skip = 0, int take = 100, CancellationToken ct = default) {
        const string sql =
            "select id, username, passwordhash as PasswordHash, createdat as CreatedAt from appuser order by createdat desc offset @skip limit @take;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        var rows = await c.QueryAsync<AppUser>(new CommandDefinition(sql, new { skip, take }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default) {
        const string sql =
            "select id, username, passwordhash as PasswordHash, createdat as CreatedAt from appuser where lower(username)=lower(@u) limit 1;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        return await c.QueryFirstOrDefaultAsync<AppUser>(new CommandDefinition(sql, new { u = username },
            cancellationToken: ct));
    }

    public async Task<AppUser> CreateAsync(AppUser e, CancellationToken ct = default) {
        const string sql = @"insert into appuser (id, username, passwordhash, createdat)
                             values (coalesce(@Id, gen_random_uuid()), @Username, @PasswordHash, now())
                             returning id, username, passwordhash as PasswordHash, createdat as CreatedAt;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        return await c.QuerySingleAsync<AppUser>(new CommandDefinition(sql, e, cancellationToken: ct));
    }

    public async Task<bool> UpdateAsync(AppUser e, CancellationToken ct = default) {
        const string sql = "update appuser set username=@Username, passwordhash=@PasswordHash where id=@Id;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        var n = await c.ExecuteAsync(new CommandDefinition(sql, e, cancellationToken: ct));
        return n == 1;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) {
        const string sql = "delete from appuser where id=@id;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        var n = await c.ExecuteAsync(new CommandDefinition(sql, new { id }, cancellationToken: ct));
        return n == 1;
    }
}