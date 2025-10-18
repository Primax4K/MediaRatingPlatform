namespace Domain.Repositories.Interfaces;

public interface IRepository<TEntity, in TId> {
	Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default);
	Task<IReadOnlyList<TEntity>> ListAsync(int skip = 0, int take = 100, CancellationToken ct = default);
	Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default);
	Task<bool> UpdateAsync(TEntity entity, CancellationToken ct = default);
	Task<bool> DeleteAsync(TId id, CancellationToken ct = default);
}