namespace WebAPI.Routers.Handler;

public class RouterHandler {
	private readonly UserRouter _usersRouter;
	private readonly IConfiguration _configuration;
	private readonly MediaRouter _mediaRouter;

	public RouterHandler(UserRouter usersRouter, IConfiguration configuration, MediaRouter mediaRouter) {
		_usersRouter = usersRouter;
		_configuration = configuration;
		_mediaRouter = mediaRouter;
	}

	public async Task Dispatch(HttpListenerRequest request, HttpListenerResponse response) {
		string path = request.Url.AbsolutePath;

		string userRouter = _configuration["routers:userRouter"] ??
		                    throw new Exception("User router configuration missing");
		
		string mediaRouter = _configuration["routers:mediaRouter"] ??
		                     throw new Exception("Media router configuration missing");

		if (path.StartsWith(userRouter, StringComparison.OrdinalIgnoreCase))
			await _usersRouter.Route(request, response, userRouter);
		if(path.StartsWith(mediaRouter, StringComparison.OrdinalIgnoreCase))
			await _mediaRouter.Route(request, response, mediaRouter);
		else
			response.StatusCode = 404;

		response.Close();
	}
}