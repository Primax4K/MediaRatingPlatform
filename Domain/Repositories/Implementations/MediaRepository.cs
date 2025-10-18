using Dapper;
using Domain.ConnectionFactory;
using Domain.Repositories.Interfaces;
using Model.Entities;

namespace Domain.Repositories.Implementations;

public sealed class MediaRepository : IMediaRepository {
	private readonly IDbConnectionFactory _factory;
	public MediaRepository(IDbConnectionFactory factory) => _factory = factory;

	public async Task<Media?> GetByIdAsync(Guid id, CancellationToken ct = default) {
		const string sql =
			@"select id, title, description, type, release_year, age_restriction, created_by, created_at, updated_at
                             from media where id=@id;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		return await c.QueryFirstOrDefaultAsync<Media>(new CommandDefinition(sql, new { id }, cancellationToken: ct));
	}

	public async Task<IReadOnlyList<Media>> ListAsync(int skip = 0, int take = 100, CancellationToken ct = default) {
		const string sql =
			@"select id, title, description, type, release_year, age_restriction, created_by, created_at, updated_at
                             from media order by created_at desc offset @skip limit @take;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		var rows = await c.QueryAsync<Media>(new CommandDefinition(sql, new { skip, take }, cancellationToken: ct));
		return rows.AsList();
	}

	public async Task<Media> CreateAsync(Media e, CancellationToken ct = default) {
		const string sql = @"insert into media
            (id, title, description, type, release_year, age_restriction, created_by, created_at, updated_at)
            values (coalesce(@Id, gen_random_uuid()), @Title, @Description, @Type, @ReleaseYear, @AgeRestriction, @CreatedBy, now(), now())
            returning id, title, description, type, release_year, age_restriction, created_by, created_at, updated_at;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		return await c.QuerySingleAsync<Media>(new CommandDefinition(sql, e, cancellationToken: ct));
	}

	public async Task<bool> UpdateAsync(Media e, CancellationToken ct = default) {
		const string sql = @"update media set title=@Title, description=@Description, type=@Type,
                             release_year=@ReleaseYear, age_restriction=@AgeRestriction, updated_at=now()
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
			@"select id, title, description, type, release_year, age_restriction, created_by, created_at, updated_at
                             from media where title ilike @q order by created_at desc limit @take;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		var rows = await c.QueryAsync<Media>(new CommandDefinition(sql, new { q = $"%{q}%", take },
			cancellationToken: ct));
		return rows.AsList();
	}

	public async Task<bool> AddGenreAsync(Guid mediaId, Guid genreId, CancellationToken ct = default) {
		const string sql = "insert into media_genre (media_id, genre_id) values (@m, @g) on conflict do nothing;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		await c.ExecuteAsync(new CommandDefinition(sql, new { m = mediaId, g = genreId }, cancellationToken: ct));
		return true;
	}

	public async Task<bool> RemoveGenreAsync(Guid mediaId, Guid genreId, CancellationToken ct = default) {
		const string sql = "delete from media_genre where media_id=@m and genre_id=@g;";
		await using var c = _factory.Create();
		await c.OpenAsync(ct);
		var n = await c.ExecuteAsync(
			new CommandDefinition(sql, new { m = mediaId, g = genreId }, cancellationToken: ct));
		return n == 1;
	}
}