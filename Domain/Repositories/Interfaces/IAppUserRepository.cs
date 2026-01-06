using Domain.Dtos;

namespace Domain.Repositories.Interfaces;

public interface IAppUserRepository : IRepository<AppUser, Guid> {
	Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
	Task<List<ActiveUserDto>> GetMostActiveUsers(CancellationToken ct = default);
	Task<List<Rating>> GetRatingsOfUserAsync(Guid userId, int skip = 0, int take = 100,
		CancellationToken ct = default);
	Task<List<Favorite>> GetFavoritesOfUserAsync(Guid userId, int skip = 0, int take = 100,
		CancellationToken ct = default);
}