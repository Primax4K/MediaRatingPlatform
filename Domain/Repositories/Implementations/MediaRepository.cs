using System.Data.Common;
using System.Text;

namespace Domain.Repositories.Implementations;

public sealed class MediaRepository : DbHelper, IMediaRepository {
	private readonly IDbConnectionFactory _factory;
	public MediaRepository(IDbConnectionFactory factory) => _factory = factory;

	public async Task<Media?> GetByIdAsync(Guid id, CancellationToken ct = default) {
		const string sql = @"
select id,
       title,
       description,
       type,
       release_year,
       age_restriction,
       average_rating,
       ratings_count,
       created_by,
       created_at,
       updated_at
from media
where id=@id;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@id", id);

		await using var r = await cmd.ExecuteReaderAsync(ct);
		if (!await r.ReadAsync(ct)) return null;

		return Map(r);
	}

	public async Task<IReadOnlyList<Media>> ListAsync(int skip = 0, int take = 100, CancellationToken ct = default) {
		const string sql = @"
select id,
       title,
       description,
       type,
       release_year,
       age_restriction,
       average_rating,
       ratings_count,
       created_by,
       created_at,
       updated_at
from media
order by created_at desc
offset @skip limit @take;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@skip", skip);
		Param(cmd, "@take", take);

		var list = new List<Media>(take);
		await using var r = await cmd.ExecuteReaderAsync(ct);
		while (await r.ReadAsync(ct))
			list.Add(Map(r));

		return list;
	}

	public async Task<Media> CreateAsync(Media e, CancellationToken ct = default) {
		const string sql = @"
insert into media
(id, title, description, type, release_year, age_restriction, average_rating, ratings_count, created_by, created_at, updated_at)
values
(coalesce(@Id, gen_random_uuid()), @Title, @Description, @Type::media_type, @ReleaseYear, @AgeRestriction, 0, 0, @CreatedBy, now(), now())
returning id,
          title,
          description,
          type,
          release_year,
          age_restriction,
          average_rating,
          ratings_count,
          created_by,
          created_at,
          updated_at;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@Id", (object?)e.Id ?? DBNull.Value);
		Param(cmd, "@Title", e.Title);
		Param(cmd, "@Description", (object?)e.Description ?? DBNull.Value);
		Param(cmd, "@Type", e.Type);
		Param(cmd, "@ReleaseYear", e.ReleaseYear);
		Param(cmd, "@AgeRestriction", e.AgeRestriction);
		Param(cmd, "@CreatedBy", e.CreatedBy);

		await using var r = await cmd.ExecuteReaderAsync(ct);
		if (!await r.ReadAsync(ct)) throw new InvalidOperationException();

		return Map(r);
	}

	public async Task<bool> UpdateAsync(Media e, CancellationToken ct = default) {
		const string sql = @"
update media
set title=@Title,
    description=@Description,
    type=@Type::media_type,
    release_year=@ReleaseYear,
    age_restriction=@AgeRestriction,
    updated_at=now()
where id=@Id;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@Id", e.Id);
		Param(cmd, "@Title", e.Title);
		Param(cmd, "@Description", (object?)e.Description ?? DBNull.Value);
		Param(cmd, "@Type", e.Type);
		Param(cmd, "@ReleaseYear", e.ReleaseYear);
		Param(cmd, "@AgeRestriction", e.AgeRestriction);

		return await cmd.ExecuteNonQueryAsync(ct) == 1;
	}

	public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) {
		const string sql = "delete from media where id=@id;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@id", id);

		return await cmd.ExecuteNonQueryAsync(ct) == 1;
	}

	public async Task<IReadOnlyList<Media>> SearchByTitleAsync(string q, int take = 20, CancellationToken ct = default) {
		const string sql = @"
select id,
       title,
       description,
       type,
       release_year,
       age_restriction,
       average_rating,
       ratings_count,
       created_by,
       created_at,
       updated_at
from media
where title ilike @q
order by created_at desc
limit @take;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@q", $"%{q}%");
		Param(cmd, "@take", take);

		var list = new List<Media>(take);
		await using var r = await cmd.ExecuteReaderAsync(ct);
		while (await r.ReadAsync(ct))
			list.Add(Map(r));

		return list;
	}

	public async Task<bool> AddGenreAsync(Guid mediaId, Guid genreId, CancellationToken ct = default) {
		const string sql =
			"insert into media_genre (media_id, genre_id) values (@m, @g) on conflict do nothing;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@m", mediaId);
		Param(cmd, "@g", genreId);

		await cmd.ExecuteNonQueryAsync(ct);
		return true;
	}

	public async Task<bool> RemoveGenreAsync(Guid mediaId, Guid genreId, CancellationToken ct = default) {
		const string sql = "delete from media_genre where media_id=@m and genre_id=@g;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@m", mediaId);
		Param(cmd, "@g", genreId);

		return await cmd.ExecuteNonQueryAsync(ct) == 1;
	}

	private static Media Map(DbDataReader r) => new() {
		Id = r.GetGuid(r.GetOrdinal("id")),
		Title = r.GetString(r.GetOrdinal("title")),
		Description = r.IsDBNull(r.GetOrdinal("description")) ? null : r.GetString(r.GetOrdinal("description")),
		Type = r.GetString(r.GetOrdinal("type")),
		ReleaseYear = r.GetInt32(r.GetOrdinal("release_year")),
		AgeRestriction = r.GetInt16(r.GetOrdinal("age_restriction")),
		CreatedBy = r.GetGuid(r.GetOrdinal("created_by")),
		CreatedAt = r.GetDateTime(r.GetOrdinal("created_at")),
		UpdatedAt = r.GetDateTime(r.GetOrdinal("updated_at")),
		RatingsCount = r.GetInt32(r.GetOrdinal("ratings_count")),
		AverageStars = (double)r.GetDecimal(r.GetOrdinal("average_rating")) // NUMERIC -> decimal -> double
	};

	public async Task<IReadOnlyList<Media>> QueryAsync(
		string? title,
		string? genre,
		string? mediaType,
		int? releaseYear,
		short? ageRestriction,
		int? rating,
		string? sortBy,
		int skip = 0,
		int take = 100,
		CancellationToken ct = default) {

		// Use precomputed columns in media: average_rating + ratings_count
		var sql = new StringBuilder(@"
select distinct
       m.id,
       m.title,
       m.description,
       m.type,
       m.release_year,
       m.age_restriction,
       m.average_rating,
       m.ratings_count,
       m.created_by,
       m.created_at,
       m.updated_at
from media m
left join media_genre mg on mg.media_id = m.id
left join genre g on g.id = mg.genre_id
where 1=1
");

		if (!string.IsNullOrWhiteSpace(title))
			sql.Append("\n and m.title ilike @title");

		if (!string.IsNullOrWhiteSpace(genre))
			sql.Append("\n and lower(g.name) = lower(@genre)");

		if (!string.IsNullOrWhiteSpace(mediaType))
			sql.Append("\n and m.type = @mediaType::media_type");

		if (releaseYear.HasValue)
			sql.Append("\n and m.release_year = @releaseYear");

		if (ageRestriction.HasValue)
			sql.Append("\n and m.age_restriction = @ageRestriction");

		if (rating.HasValue)
			sql.Append("\n and m.average_rating >= @rating");

		sortBy = (sortBy ?? "").Trim().ToLowerInvariant();
		sql.Append(sortBy switch {
			"title" => "\n order by m.title asc",
			"year"  => "\n order by m.release_year desc",
			"score" => "\n order by m.average_rating desc, m.ratings_count desc",
			_       => "\n order by m.created_at desc"
		});

		sql.Append("\n offset @skip limit @take;");

		await using var c = _factory.Create();
		await OpenAsync(c, ct);
		await using var cmd = Command(c, sql.ToString());

		if (!string.IsNullOrWhiteSpace(title)) Param(cmd, "@title", $"%{title}%");
		if (!string.IsNullOrWhiteSpace(genre)) Param(cmd, "@genre", genre);
		if (!string.IsNullOrWhiteSpace(mediaType)) Param(cmd, "@mediaType", mediaType);
		if (releaseYear.HasValue) Param(cmd, "@releaseYear", releaseYear.Value);
		if (ageRestriction.HasValue) Param(cmd, "@ageRestriction", ageRestriction.Value);
		if (rating.HasValue) Param(cmd, "@rating", rating.Value);

		Param(cmd, "@skip", skip);
		Param(cmd, "@take", take);

		var list = new List<Media>(take);
		await using var r = await cmd.ExecuteReaderAsync(ct);
		while (await r.ReadAsync(ct))
			list.Add(Map(r));

		return list;
	}
}
