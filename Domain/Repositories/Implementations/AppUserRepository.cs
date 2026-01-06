using System.Data.Common;
using Domain.Dtos;

namespace Domain.Repositories.Implementations;

public sealed class AppUserRepository : DbHelper, IAppUserRepository {
	private readonly IDbConnectionFactory _factory;
	public AppUserRepository(IDbConnectionFactory factory) => _factory = factory;

	public async Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default) {
		const string sql =
			"select id, username, password_hash as PasswordHash, created_at as CreatedAt from app_user where id=@id;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@id", id);

		await using var r = await cmd.ExecuteReaderAsync(ct);
		if (!await r.ReadAsync(ct)) return null;

		return Map(r);
	}

	public async Task<IReadOnlyList<AppUser>> ListAsync(int skip = 0, int take = 100,
		CancellationToken ct = default) {
		const string sql =
			"select id, username, password_hash as PasswordHash, created_at as CreatedAt from app_user order by created_at desc offset @skip limit @take;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@skip", skip);
		Param(cmd, "@take", take);

		var list = new List<AppUser>(take);
		await using var r = await cmd.ExecuteReaderAsync(ct);
		while (await r.ReadAsync(ct))
			list.Add(Map(r));

		return list;
	}

	public async Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default) {
		const string sql =
			"select id, username, password_hash as PasswordHash, created_at as CreatedAt from app_user where lower(username)=lower(@u) limit 1;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@u", username);

		await using var r = await cmd.ExecuteReaderAsync(ct);
		if (!await r.ReadAsync(ct)) return null;

		return Map(r);
	}

	public async Task<List<ActiveUserDto>> GetMostActiveUsers(CancellationToken ct = default) {
		const string sql = @"select u.id, u.username, count(r.id) as ratings_count
from app_user u
         left join rating r on u.id = r.user_id
group by u.id, u.username
order by count(r.id) desc limit 10;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);

		var list = new List<ActiveUserDto>();
		await using var r = await cmd.ExecuteReaderAsync(ct);
		while (await r.ReadAsync(ct)) {
			list.Add(new ActiveUserDto(
				r.GetGuid(r.GetOrdinal("id")),
				r.GetString(r.GetOrdinal("username")),
				r.GetInt32(r.GetOrdinal("ratings_count"))
			));
		}

		return list;
	}

	public async Task<List<Rating>> GetRatingsOfUserAsync(Guid userId, int skip = 0, int take = 100,
		CancellationToken ct = default) {
		const string sql = @"select id,
       media_id,
       user_id,
       stars,
       comment,
       comment_confirmed,
       created_at,
       updated_at
from rating
where user_id = @userId
order by created_at desc
offset @skip limit @take;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@userId", userId);
		Param(cmd, "@skip", skip);
		Param(cmd, "@take", take);

		var list = new List<Rating>(take);
		await using var r = await cmd.ExecuteReaderAsync(ct);
		while (await r.ReadAsync(ct)) {
			list.Add(new Rating {
				Id = r.GetGuid(r.GetOrdinal("id")),
				MediaId = r.GetGuid(r.GetOrdinal("media_id")),
				UserId = r.GetGuid(r.GetOrdinal("user_id")),
				Stars = r.GetInt16(r.GetOrdinal("stars")),
				Comment = r.IsDBNull(r.GetOrdinal("comment")) ? null : r.GetString(r.GetOrdinal("comment")),
				CommentConfirmed = r.GetBoolean(r.GetOrdinal("comment_confirmed")),
				CreatedAt = r.GetDateTime(r.GetOrdinal("created_at")),
				UpdatedAt = r.GetDateTime(r.GetOrdinal("updated_at"))
			});
		}

		return list;
	}

	public async Task<List<Favorite>> GetFavoritesOfUserAsync(Guid userId, int skip = 0, int take = 100,
		CancellationToken ct = default) {
		const string sql = @"select user_id, media_id, created_at
from favorite
where user_id = @userId
order by created_at desc
offset @skip limit @take;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@userId", userId);
		Param(cmd, "@skip", skip);
		Param(cmd, "@take", take);

		var list = new List<Favorite>(take);
		await using var r = await cmd.ExecuteReaderAsync(ct);
		while (await r.ReadAsync(ct)) {
			list.Add(new Favorite {
				UserId = r.GetGuid(r.GetOrdinal("user_id")),
				MediaId = r.GetGuid(r.GetOrdinal("media_id")),
				CreatedAt = r.GetDateTime(r.GetOrdinal("created_at"))
			});
		}

		return list;
	}


	public async Task<AppUser> CreateAsync(AppUser e, CancellationToken ct = default) {
		const string sql = @"
insert into app_user (id, username, password_hash, created_at)
values (coalesce(@Id, gen_random_uuid()), @Username, @PasswordHash, now())
returning id, username, password_hash as PasswordHash, created_at as CreatedAt;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@Id", (object?)e.Id ?? DBNull.Value);
		Param(cmd, "@Username", e.Username);
		Param(cmd, "@PasswordHash", e.PasswordHash);

		await using var r = await cmd.ExecuteReaderAsync(ct);
		if (!await r.ReadAsync(ct)) throw new InvalidOperationException();

		return Map(r);
	}

	public async Task<bool> UpdateAsync(AppUser e, CancellationToken ct = default) {
		const string sql =
			"update app_user set username=@Username, password_hash=@PasswordHash where id=@Id;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@Id", e.Id);
		Param(cmd, "@Username", e.Username);
		Param(cmd, "@PasswordHash", e.PasswordHash);

		return await cmd.ExecuteNonQueryAsync(ct) == 1;
	}

	public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) {
		const string sql = "delete from app_user where id=@id;";

		await using var c = _factory.Create();
		await OpenAsync(c, ct);

		await using var cmd = Command(c, sql);
		Param(cmd, "@id", id);

		return await cmd.ExecuteNonQueryAsync(ct) == 1;
	}

	private static AppUser Map(DbDataReader r) => new() {
		Id = r.GetGuid(r.GetOrdinal("id")),
		Username = r.GetString(r.GetOrdinal("username")),
		PasswordHash = r.GetString(r.GetOrdinal("PasswordHash")),
		CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt"))
	};
}