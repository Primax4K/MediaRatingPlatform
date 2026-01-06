using System.Data.Common;

namespace Domain.Repositories.Implementations;

public sealed class RatingRepository : DbHelper, IRatingRepository {
	private readonly IDbConnectionFactory _factory;
	public RatingRepository(IDbConnectionFactory factory) => _factory = factory;

	public async Task<Rating?> GetByIdAsync(Guid id, CancellationToken ct = default) {
		const string sql = @"select id,
       media_id,
       user_id,
       stars,
       comment,
       comment_confirmed,
       created_at,
       updated_at
from rating
where id = @id;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@id", id);

		await using var r = await cmd.ExecuteReaderAsync(ct);
		if (!await r.ReadAsync(ct)) return null;

		return Map(r);
	}

	public async Task<IReadOnlyList<Rating>>
		ListAsync(int skip = 0, int take = 100, CancellationToken ct = default) {
		const string sql = @"select id,
       media_id,
       user_id,
       stars,
       comment,
       comment_confirmed,
       created_at,
       updated_at
from rating
order by created_at desc
offset @skip limit @take;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@skip", skip);
		Param(cmd, "@take", take);

		var list = new List<Rating>(take);
		await using var r = await cmd.ExecuteReaderAsync(ct);
		while (await r.ReadAsync(ct))
			list.Add(Map(r));

		return list;
	}

	public async Task<Rating?> GetByUserAndMediaAsync(Guid userId, Guid mediaId, CancellationToken ct = default) {
		const string sql = @"select id,
       media_id,
       user_id,
       stars,
       comment,
       comment_confirmed,
       created_at,
       updated_at
from rating
where user_id = @u
  and media_id = @m limit 1;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@u", userId);
		Param(cmd, "@m", mediaId);

		await using var r = await cmd.ExecuteReaderAsync(ct);
		if (!await r.ReadAsync(ct)) return null;

		return Map(r);
	}

	public async Task<Rating> CreateAsync(Rating e, CancellationToken ct = default) {
		const string sql = @"
insert into rating (id, media_id, user_id, stars, comment, comment_confirmed, created_at, updated_at)
values (coalesce(@Id, gen_random_uuid()), @MediaId, @UserId, @Stars, @Comment, @CommentConfirmed, now(), now())
returning id,
          media_id,
          user_id,
          stars,
          comment,
          comment_confirmed,
          created_at,
          updated_at;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@Id", (object?)e.Id ?? DBNull.Value);
		Param(cmd, "@MediaId", e.MediaId);
		Param(cmd, "@UserId", e.UserId);
		Param(cmd, "@Stars", e.Stars);
		Param(cmd, "@Comment", (object?)e.Comment ?? DBNull.Value);
		Param(cmd, "@CommentConfirmed", e.CommentConfirmed);

		await using var r = await cmd.ExecuteReaderAsync(ct);
		if (!await r.ReadAsync(ct)) throw new InvalidOperationException();

		return Map(r);
	}

	public async Task<bool> UpdateAsync(Rating e, CancellationToken ct = default) {
		const string sql = @"
update rating
set stars=@Stars,
    comment=@Comment,
    comment_confirmed=@CommentConfirmed,
    updated_at=now()
where id=@Id;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@Id", e.Id);
		Param(cmd, "@Stars", e.Stars);
		Param(cmd, "@Comment", (object?)e.Comment ?? DBNull.Value);
		Param(cmd, "@CommentConfirmed", e.CommentConfirmed);

		return await cmd.ExecuteNonQueryAsync(ct) == 1;
	}

	public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) {
		const string sql = "delete from rating where id=@id;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@id", id);

		return await cmd.ExecuteNonQueryAsync(ct) == 1;
	}

	private static Rating Map(DbDataReader r) => new() {
		Id = r.GetGuid(r.GetOrdinal("id")),
		MediaId = r.GetGuid(r.GetOrdinal("media_id")),
		UserId = r.GetGuid(r.GetOrdinal("user_id")),
		Stars = r.GetInt16(r.GetOrdinal("stars")),
		Comment = r.IsDBNull(r.GetOrdinal("comment")) ? null : r.GetString(r.GetOrdinal("comment")),
		CommentConfirmed = r.GetBoolean(r.GetOrdinal("comment_confirmed")),
		CreatedAt = r.GetDateTime(r.GetOrdinal("created_at")),
		UpdatedAt = r.GetDateTime(r.GetOrdinal("updated_at"))
	};
}