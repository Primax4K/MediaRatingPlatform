namespace Domain.Repositories.Implementations;

public sealed class MediaRepository : IMediaRepository {
    private readonly IDbConnectionFactory _factory;
    public MediaRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<Media?> GetByIdAsync(Guid id, CancellationToken ct = default) {
        const string sql =
            @"select id, title, description, type,
			          releaseyear as ReleaseYear,
			          agerestriction as AgeRestriction,
			          createdby as CreatedBy,
			          createdat as CreatedAt,
			          updatedat as UpdatedAt
			  from media
			  where id=@id;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        return await c.QueryFirstOrDefaultAsync<Media>(new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Media>> ListAsync(int skip = 0, int take = 100, CancellationToken ct = default) {
        const string sql =
            @"select id, title, description, type,
			          releaseyear as ReleaseYear,
			          agerestriction as AgeRestriction,
			          createdby as CreatedBy,
			          createdat as CreatedAt,
			          updatedat as UpdatedAt
			  from media
			  order by createdat desc
			  offset @skip limit @take;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        var rows = await c.QueryAsync<Media>(new CommandDefinition(sql, new { skip, take }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<Media> CreateAsync(Media e, CancellationToken ct = default) {
        const string sql = @"
		insert into media
		  (id, title, description, type, releaseyear, agerestriction, createdby, createdat, updatedat)
		values
		  (coalesce(@Id, gen_random_uuid()), @Title, @Description, @Type::media_type, @ReleaseYear, @AgeRestriction, @CreatedBy, now(), now())
		returning id, title, description, type,
		         releaseyear as ReleaseYear,
		         agerestriction as AgeRestriction,
		         createdby as CreatedBy,
		         createdat as CreatedAt,
		         updatedat as UpdatedAt;";

        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        return await c.QuerySingleAsync<Media>(new CommandDefinition(sql, e, cancellationToken: ct));
    }

    public async Task<bool> UpdateAsync(Media e, CancellationToken ct = default) {
        const string sql = @"
			update media
			set title=@Title,
			    description=@Description,
			    type=@Type::media_type,
			    releaseyear=@ReleaseYear,
			    agerestriction=@AgeRestriction,
			    updatedat=now()
			where id=@Id;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        var n = await c.ExecuteAsync(new CommandDefinition(sql, e, cancellationToken: ct));
        return n == 1;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) {
        const string sql = "delete from media where id=@id;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        var n = await c.ExecuteAsync(new CommandDefinition(sql, new { id }, cancellationToken: ct));
        return n == 1;
    }

    public async Task<IReadOnlyList<Media>>
        SearchByTitleAsync(string q, int take = 20, CancellationToken ct = default) {
        const string sql =
            @"select id, title, description, type,
			          releaseyear as ReleaseYear,
			          agerestriction as AgeRestriction,
			          createdby as CreatedBy,
			          createdat as CreatedAt,
			          updatedat as UpdatedAt
			  from media
			  where title ilike @q
			  order by createdat desc
			  limit @take;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        var rows = await c.QueryAsync<Media>(new CommandDefinition(sql, new { q = $"%{q}%", take },
            cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<bool> AddGenreAsync(Guid mediaId, Guid genreId, CancellationToken ct = default) {
        const string sql = "insert into mediagenre (mediaid, genreid) values (@m, @g) on conflict do nothing;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        await c.ExecuteAsync(new CommandDefinition(sql, new { m = mediaId, g = genreId }, cancellationToken: ct));
        return true;
    }

    public async Task<bool> RemoveGenreAsync(Guid mediaId, Guid genreId, CancellationToken ct = default) {
        const string sql = "delete from mediagenre where mediaid=@m and genreid=@g;";
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        var n = await c.ExecuteAsync(
            new CommandDefinition(sql, new { m = mediaId, g = genreId }, cancellationToken: ct));
        return n == 1;
    }
}