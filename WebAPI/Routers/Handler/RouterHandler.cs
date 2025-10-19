namespace WebAPI.Routers.Handler;

public class RouterHandler {
	private readonly UserRouter _usersRouter;
	private readonly IConfiguration _configuration;

	public RouterHandler(UserRouter usersRouter, IConfiguration configuration) {
		_usersRouter = usersRouter;
		_configuration = configuration;
	}

	public async Task Dispatch(HttpListenerRequest request, HttpListenerResponse response) {
		string path = request.Url.AbsolutePath;

		string userRouter = _configuration["routers:userRouter"] ??
		                    throw new Exception("User router configuration missing");

		if (path.StartsWith(userRouter, StringComparison.OrdinalIgnoreCase))
			await _usersRouter.Route(request, response, userRouter);
		else
			response.StatusCode = 404;

		response.Close();
	}
}