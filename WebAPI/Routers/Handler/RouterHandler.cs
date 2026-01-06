namespace WebAPI.Routers.Handler;

public class RouterHandler {
	private readonly UserRouter _usersRouter;
	private readonly IConfiguration _configuration;
	private readonly MediaRouter _mediaRouter;
	private readonly RatingRouter _ratingRouter;
	private readonly LeaderBoardRouter _leaderBoardRouter;

	public RouterHandler(UserRouter usersRouter, IConfiguration configuration, MediaRouter mediaRouter, RatingRouter ratingRouter, LeaderBoardRouter leaderBoardRouter) {
		_usersRouter = usersRouter;
		_configuration = configuration;
		_mediaRouter = mediaRouter;
		_ratingRouter = ratingRouter;
		_leaderBoardRouter = leaderBoardRouter;
	}

	public async Task Dispatch(HttpListenerRequest request, HttpListenerResponse response) {
		string path = request.Url.AbsolutePath;

		string userRouter = _configuration["routers:userRouter"] ??
		                    throw new Exception("User router configuration missing");
		
		string mediaRouter = _configuration["routers:mediaRouter"] ??
		                     throw new Exception("Media router configuration missing");
		
		string ratingRouter = _configuration["routers:ratingRouter"] ??
		                      throw new Exception("Rating router configuration missing");
		
		string leaderBoardRouter = _configuration["routers:leaderBoardRouter"] ??
		                      throw new Exception("LeaderBoard router configuration missing");

		if (path.StartsWith(userRouter, StringComparison.OrdinalIgnoreCase))
			await _usersRouter.Route(request, response, userRouter);
		if(path.StartsWith(mediaRouter, StringComparison.OrdinalIgnoreCase))
			await _mediaRouter.Route(request, response, mediaRouter);
		if(path.StartsWith(ratingRouter, StringComparison.OrdinalIgnoreCase))
			await _ratingRouter.Route(request, response, ratingRouter);
		if(path.StartsWith(leaderBoardRouter, StringComparison.OrdinalIgnoreCase))
			await _leaderBoardRouter.Route(request, response, leaderBoardRouter);
		else
			response.StatusCode = 404;

		response.Close();
	}
}