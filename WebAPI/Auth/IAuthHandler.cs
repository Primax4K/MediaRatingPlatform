namespace WebAPI.Auth;

public interface IAuthHandler {
	Task<bool> RegisterUser(RegisterDto dto, CancellationToken ct = default);
	Task<string?> AuthenticateUser(LoginDto dto, CancellationToken ct = default);
	Task<bool> VerifyTokenAsync(string token, CancellationToken ct = default);
}