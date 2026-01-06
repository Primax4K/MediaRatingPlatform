namespace WebAPI.Routers;

public class RatingRouter : ARouter {
	private readonly IAuthHandler _authHandler;
	private readonly IRatingLikeRepository _ratingLikeRepository;

	public RatingRouter(IAuthHandler authHandler, IRatingLikeRepository ratingLikeRepository) : base(authHandler) {
		_authHandler = authHandler;
		_ratingLikeRepository = ratingLikeRepository;

		RegisterWithParams(HttpMethod.Post.Method, "/{id}/like", LikeRating, requiresAuth: true);
	}

	private async Task LikeRating(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		string idRaw = parameters["id"];

		if (!Guid.TryParse(idRaw, out var id)) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid rating ID");
			return;
		}

		string token = request.GetAuthToken();

		string? userId = await _authHandler.GetUserIdFromTokenAsync(token);

		if (userId == null) {
			await response.WriteResponse(HttpStatusCode.Unauthorized, "Invalid token");
			return;
		}
		
		bool exists = await _ratingLikeRepository.ExistsAsync(id, Guid.Parse(userId));
		if (exists) {
			await response.WriteResponse(HttpStatusCode.Conflict, "Rating like already exists");
			return;
		}
		
		var ratingLike = new RatingLike {
			RatingId = id,
			UserId = Guid.Parse(userId)
		};

		await _ratingLikeRepository.AddAsync(id, ratingLike.UserId);

		await response.WriteResponse(HttpStatusCode.Created, JsonSerializer.Serialize(ratingLike));
	}
}