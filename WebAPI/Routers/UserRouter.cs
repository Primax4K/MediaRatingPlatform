namespace WebAPI.Routers;

public class UserRouter : ARouter {
	private readonly IAuthHandler _authHandler;
	private readonly IAppUserRepository _userRepository;

	public UserRouter(IAuthHandler authHandler, IAppUserRepository userRepository) : base(authHandler) {
		_authHandler = authHandler;
		_userRepository = userRepository;

		Register(HttpMethod.Get.Method, "/abc/ta", HandleGet, requiresAuth: true);
		RegisterWithParams(HttpMethod.Get.Method, "/abc/{id}/{name}", HandleGetWithId, requiresAuth: true);
		Register(HttpMethod.Post.Method, "/register", HandleRegister, requiresAuth: false);
		Register(HttpMethod.Post.Method, "/login", HandleLogin, requiresAuth: false);
		RegisterWithParams(HttpMethod.Delete.Method, "/{id}", DeleteUser, requiresAuth: true);
	}

	private async Task HandleGet(HttpListenerRequest request, HttpListenerResponse response) {
		await response.WriteResponse("User GET Handler");
	}

	private async Task HandleGetWithId(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		var id = parameters["id"];
		var name = parameters["name"];
		await response.WriteResponse($"User GET Handler - ID: {id} and name {name}");
	}

	private async Task HandleRegister(HttpListenerRequest request, HttpListenerResponse response) {
		var body = await request.ReadRequestBodyAsync();

		RegisterDto? registerDto = JsonSerializer.Deserialize<RegisterDto>(body);

		if (registerDto == null || string.IsNullOrEmpty(registerDto.Username) ||
		    string.IsNullOrEmpty(registerDto.Password)) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid registration data");
			return;
		}

		bool success = await _authHandler.RegisterUser(registerDto);

		if (success)
			await response.WriteResponse(HttpStatusCode.Created, "User registered successfully");
		else
			await response.WriteResponse(HttpStatusCode.Conflict, "Failed to create user");
	}

	private async Task HandleLogin(HttpListenerRequest request, HttpListenerResponse response) {
		var body = await request.ReadRequestBodyAsync();

		LoginDto? authDto = JsonSerializer.Deserialize<LoginDto>(body);

		if (authDto == null || string.IsNullOrEmpty(authDto.Username) || string.IsNullOrEmpty(authDto.Password)) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid authentication data");
			return;
		}

		string? token = await _authHandler.AuthenticateUser(authDto);

		if (token != null)
			await response.WriteResponse(JsonSerializer.Serialize(new { Token = token }));
		else
			await response.WriteResponse(HttpStatusCode.Unauthorized, "Authentication failed");
	}
	
	private async Task DeleteUser(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		var id = parameters["id"];
		bool success = await _userRepository.DeleteAsync(Guid.Parse(id));
		
		if (success)
			response.StatusCode = (int)HttpStatusCode.NoContent;
		else
			await response.WriteResponse(HttpStatusCode.NotFound, "User not found");
	}
}