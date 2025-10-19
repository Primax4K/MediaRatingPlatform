using System.Text.Json;
using Domain.Repositories.Interfaces;
using WebAPI.Auth;
using WebAPI.Dtos;

namespace WebAPI.Routers;

public class UserRouter : ARouter {
	private readonly IRatingLikeRepository _ratingLikeRepository;
	private readonly IAuthHandler _authHandler;

	public UserRouter(IRatingLikeRepository ratingLikeRepository, IAuthHandler authHandler) {
		_ratingLikeRepository = ratingLikeRepository;
		_authHandler = authHandler;
		
		Register(HttpMethod.Get.Method, "/abc/ta", HandleGet);
		RegisterWithParams(HttpMethod.Get.Method, "/abc/{id}", HandleGetWithId);
		Register(HttpMethod.Post.Method, "/register", HandleRegister);
		Register(HttpMethod.Post.Method, "/login", HandleLogin);
	}

	private async Task HandleGet(HttpListenerRequest request, HttpListenerResponse response) {
		await _ratingLikeRepository.AddAsync(
			Guid.Parse("a83b9ec8-d649-44b6-9cc0-606b174d718e"),
			Guid.Parse("2b71a163-014b-41cc-855f-6acdc89ae701")
		);
		await response.WriteResponse("<h1>User GET Handler</h1>");
	}

	private async Task HandleGetWithId(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		var id = parameters["id"];
		await response.WriteResponse($"<h1>User GET Handler - ID: {id}</h1>");
	}

	private async Task HandleRegister(HttpListenerRequest request, HttpListenerResponse response) {
		var body = await request.ReadRequestBodyAsync();

		RegisterDto? registerDto = JsonSerializer.Deserialize<RegisterDto>(body);

		if (registerDto == null || string.IsNullOrEmpty(registerDto.Username) ||
		    string.IsNullOrEmpty(registerDto.Password)) {
			await response.WriteResponse(400, "<h1>Invalid registration data</h1>");
			return;
		}

		bool success = await _authHandler.RegisterUser(registerDto);

		if (success)
			await response.WriteResponse(201, "<h1>User registered successfully</h1>");
		else
			await response.WriteResponse(409, "<h1>Failed to create user</h1>");
	}

	private async Task HandleLogin(HttpListenerRequest request, HttpListenerResponse response) {
		var body = await request.ReadRequestBodyAsync();

		LoginDto? authDto = JsonSerializer.Deserialize<LoginDto>(body);

		if (authDto == null || string.IsNullOrEmpty(authDto.Username) || string.IsNullOrEmpty(authDto.Password)) {
			await response.WriteResponse(400, "<h1>Invalid authentication data</h1>");
			return;
		}

		string? token = await _authHandler.AuthenticateUser(authDto);

		if (token != null)
			await response.WriteResponse(200, JsonSerializer.Serialize(new { Token = token }));
		else
			await response.WriteResponse(401, "<h1>Authentication failed</h1>");
	}
}