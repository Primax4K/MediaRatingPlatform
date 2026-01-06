namespace Domain.Repositories.Interfaces;

public interface IRatingLikeRepository {
	Task<bool> AddAsync(Guid ratingId, Guid userId, CancellationToken ct = default);
	Task<bool> RemoveAsync(Guid ratingId, Guid userId, CancellationToken ct = default);
	Task<bool> ExistsAsync(Guid ratingId, Guid userId, CancellationToken ct = default);
}