using WebAPI.Dtos;

namespace WebAPI.Auth;

public interface IAuthHandler {
	Task<bool> RegisterUser(RegisterDto dto, CancellationToken ct = default);
	Task<string?> AuthenticateUser(LoginDto dto, CancellationToken ct = default);
}