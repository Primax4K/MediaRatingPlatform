using Model.Entities;

namespace Domain.Repositories.Interfaces;

public interface IMediaRepository : IRepository<Media, Guid> {
	Task<IReadOnlyList<Media>> SearchByTitleAsync(string q, int take = 20, CancellationToken ct = default);
	Task<bool> AddGenreAsync(Guid mediaId, Guid genreId, CancellationToken ct = default);
	Task<bool> RemoveGenreAsync(Guid mediaId, Guid genreId, CancellationToken ct = default);
}