namespace WebAPI.Auth;

public class AuthHandler : IAuthHandler {
	private readonly IConfiguration _config;
	private readonly IAppUserRepository _userRepository;

	public AuthHandler(IConfiguration config, IAppUserRepository userRepository) {
		_config = config;
		_userRepository = userRepository;
	}

	private string GenerateJwtToken(string userId, string username) {
		var key = new SymmetricSecurityKey(
			Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"] ?? throw new InvalidOperationException()));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: _config["Jwt:Issuer"],
			audience: _config["Jwt:Audience"],
			claims: [
				new Claim(JwtRegisteredClaimNames.Sub, userId),
				new Claim(JwtRegisteredClaimNames.Name, username)
			],
			notBefore: DateTime.UtcNow,
			expires: DateTime.UtcNow.AddHours(double.Parse(_config["Jwt:ExpirationHours"] ?? "1")),
			signingCredentials: creds
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	public async Task<bool> RegisterUser(RegisterDto dto, CancellationToken ct = default) {
		var user = await _userRepository.GetByUsernameAsync(dto.Username.ToLower(), ct);
		if (user != null)
			return false; // User already exists

		var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

		await _userRepository.CreateAsync(new AppUser {
			Username = dto.Username.ToLower(),
			PasswordHash = hashedPassword
		}, ct);

		return true;
	}

	public async Task<string?> AuthenticateUser(LoginDto dto, CancellationToken ct = default) {
		var user = await _userRepository.GetByUsernameAsync(dto.Username.ToLower(), ct);

		if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
			return null; // Invalid credentials

		return GenerateJwtToken(user.Id.ToString(), user.Username);
	}

	public async Task<bool> VerifyTokenAsync(string token, CancellationToken ct = default) {
		var tokenHandler = new JwtSecurityTokenHandler();
		var key = Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"] ?? throw new InvalidOperationException());

		try {
			tokenHandler.ValidateToken(token, new TokenValidationParameters {
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true,
				ValidIssuer = _config["Jwt:Issuer"],
				ValidAudience = _config["Jwt:Audience"],
				IssuerSigningKey = new SymmetricSecurityKey(key)
			}, out SecurityToken validatedToken);

			return true;
		}
		catch {
			return false;
		}
	}

	public Task<string?> GetUserIdFromTokenAsync(string token, CancellationToken ct = default) {
		var tokenHandler = new JwtSecurityTokenHandler();
		var jwtToken = tokenHandler.ReadJwtToken(token);
		var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
		return Task.FromResult(userIdClaim?.Value);
	}
}