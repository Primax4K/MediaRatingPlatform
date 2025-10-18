using Model.Entities;

namespace Domain.Repositories.Interfaces;

public interface IAppUserRepository : IRepository<AppUser, Guid> {
	Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
}