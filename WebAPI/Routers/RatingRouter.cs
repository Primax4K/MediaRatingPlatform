using WebAPI.Dtos.Rating;

namespace WebAPI.Routers;

public class RatingRouter : ARouter {
	private readonly IAuthHandler _authHandler;
	private readonly IRatingLikeRepository _ratingLikeRepository;
	private readonly IRatingRepository _ratingRepository;

	public RatingRouter(IAuthHandler authHandler, IRatingLikeRepository ratingLikeRepository,
		IRatingRepository ratingRepository) : base(authHandler) {
		_authHandler = authHandler;
		_ratingLikeRepository = ratingLikeRepository;
		_ratingRepository = ratingRepository;

		RegisterWithParams(HttpMethod.Post.Method, "/{id}/like", LikeRating, requiresAuth: true);
		RegisterWithParams(HttpMethod.Put.Method, "/{ratingId}", UpdateRating, requiresAuth: true);
		RegisterWithParams(HttpMethod.Post.Method, "/{ratingId}/confirm", ConfirmRating, requiresAuth: true);
		
		RegisterWithParams(HttpMethod.Delete.Method, "/{ratingId}", DeleteRating, requiresAuth: true);
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

	private async Task UpdateRating(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		string ratingIdRaw = parameters["ratingId"];

		if (!Guid.TryParse(ratingIdRaw, out var ratingId)) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid rating ID");
			return;
		}

		string token = request.GetAuthToken();
		string? userId = await _authHandler.GetUserIdFromTokenAsync(token);

		if (userId == null) {
			await response.WriteResponse(HttpStatusCode.Unauthorized, "Invalid token");
			return;
		}

		var existingRating = await _ratingRepository.GetByIdAsync(ratingId);

		if (existingRating == null) {
			await response.WriteResponse(HttpStatusCode.NotFound, "Rating not found");
			return;
		}

		if (existingRating.UserId != Guid.Parse(userId)) {
			await response.WriteResponse(HttpStatusCode.Forbidden, "You can only update your own ratings");
			return;
		}

		var body = await request.ReadRequestBodyAsync();
		var updateData = JsonSerializer.Deserialize<RatingUpdateDto>(body, JsonSerializerOptions.Web);

		if (updateData == null) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid request body");
			return;
		}

		if (updateData.Stars is < 1 or > 5) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Stars must be between 1 and 5.");
			return;
		}
		existingRating.Stars = updateData.Stars;
		existingRating.Comment = updateData.Comment;

		await _ratingRepository.UpdateAsync(existingRating);

		await response.WriteResponse(HttpStatusCode.OK, JsonSerializer.Serialize(existingRating));
	}

	private async Task ConfirmRating(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		string ratingIdRaw = parameters["ratingId"];

		if (!Guid.TryParse(ratingIdRaw, out var ratingId)) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid rating ID");
			return;
		}

		string token = request.GetAuthToken();
		string? userId = await _authHandler.GetUserIdFromTokenAsync(token);

		if (userId == null) {
			await response.WriteResponse(HttpStatusCode.Unauthorized, "Invalid token");
			return;
		}

		var rating = await _ratingRepository.GetByIdAsync(ratingId);

		if (rating == null) {
			await response.WriteResponse(HttpStatusCode.NotFound, "Rating not found");
			return;
		}

		if (rating.UserId != Guid.Parse(userId)) {
			await response.WriteResponse(HttpStatusCode.Forbidden, "You can only confirm your own ratings");
			return;
		}

		var confirmedRating = await _ratingRepository.ConfirmRating(ratingId);

		await response.WriteResponse(HttpStatusCode.OK, JsonSerializer.Serialize(confirmedRating));
	}
	
	private async Task DeleteRating(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		string ratingIdRaw = parameters["ratingId"];

		if (!Guid.TryParse(ratingIdRaw, out var ratingId)) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid rating ID");
			return;
		}

		string token = request.GetAuthToken();
		string? userId = await _authHandler.GetUserIdFromTokenAsync(token);

		if (userId == null) {
			await response.WriteResponse(HttpStatusCode.Unauthorized, "Invalid token");
			return;
		}

		var existingRating = await _ratingRepository.GetByIdAsync(ratingId);
		if (existingRating == null) {
			await response.WriteResponse(HttpStatusCode.NotFound, "Rating not found");
			return;
		}

		if (existingRating.UserId != Guid.Parse(userId)) {
			await response.WriteResponse(HttpStatusCode.Forbidden, "You can only delete your own ratings");
			return;
		}

		await _ratingRepository.DeleteAsync(ratingId);

		await response.WriteResponse(HttpStatusCode.NoContent, string.Empty);
	}
}