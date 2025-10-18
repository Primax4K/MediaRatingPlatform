namespace Domain.Repositories.Interfaces;

public interface IFavoriteRepository {
	Task<bool> AddAsync(Guid userId, Guid mediaId, CancellationToken ct = default);
	Task<bool> RemoveAsync(Guid userId, Guid mediaId, CancellationToken ct = default);
}