namespace WebAPI.Routers;

public class LeaderBoardRouter : ARouter {
	private readonly IAuthHandler _authHandler;
	private readonly IAppUserRepository _appUserRepository;

	public LeaderBoardRouter(IAuthHandler authHandler, IAppUserRepository appUserRepository) : base(authHandler) {
		_authHandler = authHandler;
		_appUserRepository = appUserRepository;
		
		Register(HttpMethod.Get.Method, "/", GetLeaderBoard, requiresAuth: true);
	}
	
	private async Task GetLeaderBoard(HttpListenerRequest request, HttpListenerResponse response) {
		var users = await _appUserRepository.GetMostActiveUsers();

		users = users.OrderByDescending(u => u.RatingsCount).ToList();
		await response.WriteResponse(HttpStatusCode.OK, JsonSerializer.Serialize(users));
	}
}