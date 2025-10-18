using Model.Entities;

namespace Domain.Repositories.Interfaces;

public interface IGenreRepository : IRepository<Genre, Guid> {
	Task<Genre?> GetByNameAsync(string name, CancellationToken ct = default);
}