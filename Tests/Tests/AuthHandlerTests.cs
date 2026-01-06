using Domain.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Model.Entities;
using Moq;
using WebAPI.Auth;
using WebAPI.Dtos.Auth;
using WebAPI.Dtos.User;

namespace Tests.Tests;

public class AuthHandlerTests {
	private static IConfiguration BuildConfig() =>
		new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> {
				["Jwt:SecretKey"] = "THIS_IS_A_TEST_SECRET_KEY_32CHARS_MIN!!",
				["Jwt:Issuer"] = "test-issuer",
				["Jwt:Audience"] = "test-audience",
				["Jwt:ExpirationHours"] = "1"
			})
			.Build();

	private static AuthHandler CreateSut(Mock<IAppUserRepository> repoMock)
		=> new AuthHandler(BuildConfig(), repoMock.Object);

	[Fact]
	public async Task RegisterUser_UserAlreadyExists_ReturnsFalse() {
		var repo = new Mock<IAppUserRepository>(MockBehavior.Strict);
		repo.Setup(r => r.GetByUsernameAsync("alice", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new AppUser { Username = "alice", PasswordHash = "x" });

		var sut = CreateSut(repo);

		var ok = await sut.RegisterUser(new RegisterDto("Alice", "pw"));

		Assert.False(ok);
		repo.Verify(r => r.CreateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task RegisterUser_NewUser_CreatesAndReturnsTrue() {
		var repo = new Mock<IAppUserRepository>(MockBehavior.Strict);
		repo.Setup(r => r.GetByUsernameAsync("alice", It.IsAny<CancellationToken>()))
			.ReturnsAsync((AppUser?)null);

		repo.Setup(r => r.CreateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((AppUser u, CancellationToken _) => u);

		var sut = CreateSut(repo);

		var ok = await sut.RegisterUser(new RegisterDto("Alice", "pw"));

		Assert.True(ok);
		repo.Verify(r => r.CreateAsync(It.Is<AppUser>(u =>
				u.Username == "alice" &&
				!string.IsNullOrWhiteSpace(u.PasswordHash) &&
				u.PasswordHash != "pw"),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task RegisterUser_StoresLowercasedUsername() {
		var repo = new Mock<IAppUserRepository>(MockBehavior.Strict);
		repo.Setup(r => r.GetByUsernameAsync("bob", It.IsAny<CancellationToken>()))
			.ReturnsAsync((AppUser?)null);

		repo.Setup(r => r.CreateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((AppUser u, CancellationToken _) => u);

		var sut = CreateSut(repo);

		var ok = await sut.RegisterUser(new RegisterDto("BoB", "pw"));

		Assert.True(ok);
		repo.Verify(r => r.CreateAsync(It.Is<AppUser>(u => u.Username == "bob"), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task RegisterUser_PasswordIsHashedAndVerifiable() {
		var repo = new Mock<IAppUserRepository>(MockBehavior.Strict);
		repo.Setup(r => r.GetByUsernameAsync("carol", It.IsAny<CancellationToken>()))
			.ReturnsAsync((AppUser?)null);

		AppUser? created = null;
		repo.Setup(r => r.CreateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
			.Callback<AppUser, CancellationToken>((u, _) => created = u)
			.ReturnsAsync((AppUser u, CancellationToken _) => u);

		var sut = CreateSut(repo);

		var ok = await sut.RegisterUser(new RegisterDto("Carol", "secret"));

		Assert.True(ok);
		Assert.NotNull(created);
		Assert.True(BCrypt.Net.BCrypt.Verify("secret", created!.PasswordHash));
	}

	[Fact]
	public async Task AuthenticateUser_UserNotFound_ReturnsNull() {
		var repo = new Mock<IAppUserRepository>(MockBehavior.Strict);
		repo.Setup(r => r.GetByUsernameAsync("dave", It.IsAny<CancellationToken>()))
			.ReturnsAsync((AppUser?)null);

		var sut = CreateSut(repo);

		var token = await sut.AuthenticateUser(new LoginDto("Dave", "pw"));

		Assert.Null(token);
	}

	[Fact]
	public async Task AuthenticateUser_WrongPassword_ReturnsNull() {
		var repo = new Mock<IAppUserRepository>(MockBehavior.Strict);
		repo.Setup(r => r.GetByUsernameAsync("erin", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new AppUser {
				Id = Guid.NewGuid(),
				Username = "erin",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct")
			});

		var sut = CreateSut(repo);

		var token = await sut.AuthenticateUser(new LoginDto("Erin", "wrong"));

		Assert.Null(token);
	}

	[Fact]
	public async Task AuthenticateUser_ValidCredentials_ReturnsJwtToken() {
		var id = Guid.NewGuid();

		var repo = new Mock<IAppUserRepository>(MockBehavior.Strict);
		repo.Setup(r => r.GetByUsernameAsync("frank", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new AppUser {
				Id = id,
				Username = "frank",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("pw")
			});

		var sut = CreateSut(repo);

		var token = await sut.AuthenticateUser(new LoginDto("Frank", "pw"));

		Assert.False(string.IsNullOrWhiteSpace(token));
		Assert.Contains(".", token!);
	}

	[Fact]
	public async Task VerifyTokenAsync_TokenFromAuthenticateUser_IsValid() {
		var id = Guid.NewGuid();

		var repo = new Mock<IAppUserRepository>(MockBehavior.Strict);
		repo.Setup(r => r.GetByUsernameAsync("gina", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new AppUser {
				Id = id,
				Username = "gina",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("pw")
			});

		var sut = CreateSut(repo);

		var token = await sut.AuthenticateUser(new LoginDto("Gina", "pw"));
		Assert.NotNull(token);

		var ok = await sut.VerifyTokenAsync(token!);

		Assert.True(ok);
	}

	[Fact]
	public async Task VerifyTokenAsync_InvalidToken_ReturnsFalse() {
		var repo = new Mock<IAppUserRepository>(MockBehavior.Strict);
		var sut = CreateSut(repo);

		var ok = await sut.VerifyTokenAsync("not-a-jwt");

		Assert.False(ok);
	}

	[Fact]
	public async Task GetUserIdFromTokenAsync_ReturnsSubClaim() {
		var id = Guid.NewGuid();

		var repo = new Mock<IAppUserRepository>(MockBehavior.Strict);
		repo.Setup(r => r.GetByUsernameAsync("henry", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new AppUser {
				Id = id,
				Username = "henry",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("pw")
			});

		var sut = CreateSut(repo);

		var token = await sut.AuthenticateUser(new LoginDto("Henry", "pw"));
		Assert.NotNull(token);

		var sub = await sut.GetUserIdFromTokenAsync(token!);

		Assert.Equal(id.ToString(), sub);
	}
}