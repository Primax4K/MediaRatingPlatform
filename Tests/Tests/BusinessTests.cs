using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Domain.Repositories.Interfaces;
using Model.Entities;
using Moq;
using WebAPI.Auth;
using WebAPI.Dtos.User;
using WebAPI.Routers;
using WebAPI.Routers.Abstract;

namespace Tests.Tests;

public class EndpointTests_10_Total {
	// -------------------------
	// UserRouter (3)
	// -------------------------

	[Fact]
	public async Task User_Register_InvalidData_Returns400() {
		var auth = new Mock<IAuthHandler>();
		var repo = new Mock<IAppUserRepository>();
		var router = new UserRouter(auth.Object, repo.Object);

		var body = JsonSerializer.Serialize(new { Username = "", Password = "" });

		var res = await Send(router, "/users", HttpMethod.Post, "/users/register", body);

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task User_Register_Success_Returns201() {
		var auth = new Mock<IAuthHandler>();
		auth.Setup(a => a.RegisterUser(It.IsAny<RegisterDto>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var repo = new Mock<IAppUserRepository>();
		var router = new UserRouter(auth.Object, repo.Object);

		var body = JsonSerializer.Serialize(new { Username = "alice", Password = "pw" });

		var res = await Send(router, "/users", HttpMethod.Post, "/users/register", body);

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
	}

	[Fact]
	public async Task User_GetProfile_UserNotFound_Returns404() {
		var auth = new Mock<IAuthHandler>();
		auth.Setup(a => a.VerifyTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(true);

		var repo = new Mock<IAppUserRepository>();
		repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((AppUser?)null);

		var router = new UserRouter(auth.Object, repo.Object);

		var res = await Send(router, "/users", HttpMethod.Get, $"/users/{Guid.NewGuid()}/profile", body: null, bearer: "tok");

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	// -------------------------
	// MediaRouter (3)
	// -------------------------

	[Fact]
	public async Task Media_GetById_InvalidGuid_Returns400() {
		var auth = new Mock<IAuthHandler>();
		auth.Setup(a => a.VerifyTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(true);

		var mediaRepo = new Mock<IMediaRepository>();
		var ratingRepo = new Mock<IRatingRepository>();
		var favRepo = new Mock<IFavoriteRepository>();
		var router = new MediaRouter(auth.Object, mediaRepo.Object, ratingRepo.Object, favRepo.Object);

		var res = await Send(router, "/media", HttpMethod.Get, "/media/not-a-guid", body: null, bearer: "tok");

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Media_GetById_NotFound_Returns404() {
		var auth = new Mock<IAuthHandler>();
		auth.Setup(a => a.VerifyTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(true);

		var mediaRepo = new Mock<IMediaRepository>();
		mediaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Media?)null);

		var ratingRepo = new Mock<IRatingRepository>();
		var favRepo = new Mock<IFavoriteRepository>();
		var router = new MediaRouter(auth.Object, mediaRepo.Object, ratingRepo.Object, favRepo.Object);

		var res = await Send(router, "/media", HttpMethod.Get, $"/media/{Guid.NewGuid()}", body: null, bearer: "tok");

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	[Fact]
	public async Task Media_Favorite_InvalidMediaId_Returns400() {
		var auth = new Mock<IAuthHandler>();
		auth.Setup(a => a.VerifyTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(true);

		var mediaRepo = new Mock<IMediaRepository>();
		var ratingRepo = new Mock<IRatingRepository>();
		var favRepo = new Mock<IFavoriteRepository>();
		var router = new MediaRouter(auth.Object, mediaRepo.Object, ratingRepo.Object, favRepo.Object);

		var res = await Send(router, "/media", HttpMethod.Post, "/media/not-a-guid/favorite", body: null, bearer: "tok");

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	// -------------------------
	// RatingRouter (4)
	// -------------------------

	[Fact]
	public async Task Rating_Like_InvalidGuid_Returns400() {
		var auth = new Mock<IAuthHandler>();
		auth.Setup(a => a.VerifyTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(true);

		var likeRepo = new Mock<IRatingLikeRepository>();
		var ratingRepo = new Mock<IRatingRepository>();
		var router = new RatingRouter(auth.Object, likeRepo.Object, ratingRepo.Object);

		var res = await Send(router, "/ratings", HttpMethod.Post, "/ratings/not-a-guid/like", body: null, bearer: "tok");

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Rating_Like_AlreadyExists_Returns409() {
		var ratingId = Guid.NewGuid();
		var userId = Guid.NewGuid();

		var auth = new Mock<IAuthHandler>();
		auth.Setup(a => a.VerifyTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(true);
		auth.Setup(a => a.GetUserIdFromTokenAsync("tok", It.IsAny<CancellationToken>()))
			.ReturnsAsync(userId.ToString());

		var likeRepo = new Mock<IRatingLikeRepository>();
		likeRepo.Setup(r => r.ExistsAsync(ratingId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var ratingRepo = new Mock<IRatingRepository>();
		var router = new RatingRouter(auth.Object, likeRepo.Object, ratingRepo.Object);

		var res = await Send(router, "/ratings", HttpMethod.Post, $"/ratings/{ratingId}/like", body: null, bearer: "tok");

		Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
		likeRepo.Verify(r => r.AddAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Rating_Update_NotOwner_Returns403() {
		var ratingId = Guid.NewGuid();
		var ownerId = Guid.NewGuid();
		var tokenUserId = Guid.NewGuid();

		var auth = new Mock<IAuthHandler>();
		auth.Setup(a => a.VerifyTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(true);
		auth.Setup(a => a.GetUserIdFromTokenAsync("tok", It.IsAny<CancellationToken>()))
			.ReturnsAsync(tokenUserId.ToString());

		var likeRepo = new Mock<IRatingLikeRepository>();
		var ratingRepo = new Mock<IRatingRepository>();
		ratingRepo.Setup(r => r.GetByIdAsync(ratingId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Rating { Id = ratingId, UserId = ownerId, MediaId = Guid.NewGuid(), Stars = 3 });

		var router = new RatingRouter(auth.Object, likeRepo.Object, ratingRepo.Object);

		var body = JsonSerializer.Serialize(new { Stars = 4, Comment = "edit" });

		var res = await Send(router, "/ratings", HttpMethod.Put, $"/ratings/{ratingId}", body, bearer: "tok");

		Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
		ratingRepo.Verify(r => r.UpdateAsync(It.IsAny<Rating>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Rating_Confirm_NotFound_Returns404() {
		var ratingId = Guid.NewGuid();
		var tokenUserId = Guid.NewGuid();

		var auth = new Mock<IAuthHandler>();
		auth.Setup(a => a.VerifyTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(true);
		auth.Setup(a => a.GetUserIdFromTokenAsync("tok", It.IsAny<CancellationToken>()))
			.ReturnsAsync(tokenUserId.ToString());

		var likeRepo = new Mock<IRatingLikeRepository>();
		var ratingRepo = new Mock<IRatingRepository>();
		ratingRepo.Setup(r => r.GetByIdAsync(ratingId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Rating?)null);

		var router = new RatingRouter(auth.Object, likeRepo.Object, ratingRepo.Object);

		var res = await Send(router, "/ratings", HttpMethod.Post, urlPath: $"/ratings/{ratingId}/confirm", null,
			bearer: "tok");

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
		ratingRepo.Verify(r => r.ConfirmRating(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	// -------------------------
	// helper
	// -------------------------

	private static async Task<(HttpStatusCode StatusCode, string Body)> Send(
		ARouter router,
		string basePath,
		HttpMethod method,
		string urlPath,
		string? body,
		string? bearer = null
	) {
		var port = GetFreePort();
		var prefix = $"http://localhost:{port}/";

		using var listener = new HttpListener();
		listener.Prefixes.Add(prefix);
		listener.Start();

		var server = Task.Run(async () => {
			var ctx = await listener.GetContextAsync();
			try {
				await router.Route(ctx.Request, ctx.Response, basePath);
			}
			finally {
				try { ctx.Response.OutputStream.Close(); } catch { }
				try { ctx.Response.Close(); } catch { }
				try { listener.Stop(); } catch { }
			}
		});

		using var client = new HttpClient();
		using var req = new HttpRequestMessage(method, prefix.TrimEnd('/') + urlPath);

		if (bearer != null)
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

		if (body != null)
			req.Content = new StringContent(body, Encoding.UTF8, "application/json");

		using var resp = await client.SendAsync(req);
		var respBody = await resp.Content.ReadAsStringAsync();

		await server;

		return ((HttpStatusCode)resp.StatusCode, respBody);
	}

	private static int GetFreePort() {
		var l = new TcpListener(IPAddress.Loopback, 0);
		l.Start();
		var port = ((IPEndPoint)l.LocalEndpoint).Port;
		l.Stop();
		return port;
	}
}
