namespace WebAPI.Routers.Handler;

public class RouterHandler {
	private readonly UserRouter _usersRouter;

	public RouterHandler(UserRouter usersRouter) {
		_usersRouter = usersRouter;
	}

	public async Task Dispatch(HttpListenerRequest request, HttpListenerResponse response) {
		string path = request.Url.AbsolutePath;

		if (path.StartsWith("/users", StringComparison.OrdinalIgnoreCase))
			await _usersRouter.Route(request, response, "/users");
		else
			response.StatusCode = 404;

		response.Close();
	}
}