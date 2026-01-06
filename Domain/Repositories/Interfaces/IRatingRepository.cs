namespace Domain.Repositories.Interfaces;

public interface IRatingRepository : IRepository<Rating, Guid> {
	Task<Rating?> GetByUserAndMediaAsync(Guid userId, Guid mediaId, CancellationToken ct = default);
	Task<Rating?> ConfirmRating(Guid ratingId, CancellationToken ct = default);
}