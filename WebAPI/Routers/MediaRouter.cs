namespace WebAPI.Routers;

public class MediaRouter : ARouter {
	private readonly IAuthHandler _authHandler;
	private readonly IMediaRepository _mediaRepository;
	private readonly IRatingRepository _ratingRepository;
	private readonly IFavoriteRepository _favoriteRepository;

	public MediaRouter(IAuthHandler authHandler, IMediaRepository mediaRepository, IRatingRepository ratingRepository,
		IFavoriteRepository favoriteRepository) : base(authHandler) {
		_authHandler = authHandler;
		_mediaRepository = mediaRepository;
		_ratingRepository = ratingRepository;
		_favoriteRepository = favoriteRepository;

		Register(HttpMethod.Post.Method, "/", CreateMedia, requiresAuth: true);
		RegisterWithParams(HttpMethod.Get.Method, "/{id}", GetMediaById, requiresAuth: true);
		RegisterWithParams(HttpMethod.Put.Method, "/{id}", UpdateMedia, requiresAuth: true);
		RegisterWithParams(HttpMethod.Delete.Method, "/{id}", DeleteMedia, requiresAuth: true);

		RegisterWithParams(HttpMethod.Post.Method, "/{id}/rate", RateMedia, requiresAuth: true);
		RegisterWithParams(HttpMethod.Post.Method, "{mediaId}/favorite", MarkAsFavorite, requiresAuth: true);
		RegisterWithParams(HttpMethod.Delete.Method, "{mediaId}/favorite", RemoveFavorite, requiresAuth: true);
		
		Register(HttpMethod.Get.Method, "/", ListMedia, requiresAuth: true);

	}

	private async Task CreateMedia(HttpListenerRequest request, HttpListenerResponse response) {
		var body = await request.ReadRequestBodyAsync();

		var media = JsonSerializer.Deserialize<Media>(body, JsonSerializerOptions.Web);

		if (media == null) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid media data");
			return;
		}

		string token = request.GetAuthToken();

		string? userId = await _authHandler.GetUserIdFromTokenAsync(token);

		if (userId == null) {
			await response.WriteResponse(HttpStatusCode.Unauthorized, "Invalid token");
			return;
		}

		media.CreatedBy = Guid.Parse(userId);

		await _mediaRepository.CreateAsync(media);

		await response.WriteResponse(HttpStatusCode.Created, JsonSerializer.Serialize(media));
	}

	private async Task GetMediaById(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		string idRaw = parameters["id"];

		if (!Guid.TryParse(idRaw, out var id)) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid media ID");
			return;
		}

		var media = await _mediaRepository.GetByIdAsync(id);

		if (media == null) {
			await response.WriteResponse(HttpStatusCode.NotFound, "Media not found");
			return;
		}

		await response.WriteResponse(HttpStatusCode.OK, JsonSerializer.Serialize(media));
	}

	private async Task UpdateMedia(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		string idRaw = parameters["id"];

		if (!Guid.TryParse(idRaw, out var id)) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid media ID");
			return;
		}

		var existingMedia = await _mediaRepository.GetByIdAsync(id);
		if (existingMedia == null) {
			await response.WriteResponse(HttpStatusCode.NotFound, "Media not found");
			return;
		}

		string token = request.GetAuthToken();
		string? userIdStr = await _authHandler.GetUserIdFromTokenAsync(token);
		if (userIdStr == null)
		{
			await response.WriteResponse(HttpStatusCode.Unauthorized, "Unauthorized");
			return;
		}

		if (existingMedia.CreatedBy != Guid.Parse(userIdStr))
		{
			await response.WriteResponse(HttpStatusCode.Forbidden, "Forbidden");
			return;
		}
		
		var body = await request.ReadRequestBodyAsync();
		var updatedMedia = JsonSerializer.Deserialize<Media>(body, JsonSerializerOptions.Web);
		if (updatedMedia == null) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid media data");
			return;
		}

		existingMedia.Title = updatedMedia.Title;
		existingMedia.Description = updatedMedia.Description;
		existingMedia.Type = updatedMedia.Type;
		existingMedia.ReleaseYear = updatedMedia.ReleaseYear;
		existingMedia.AgeRestriction = updatedMedia.AgeRestriction;
		await _mediaRepository.UpdateAsync(existingMedia);

		await response.WriteResponse(HttpStatusCode.OK, JsonSerializer.Serialize(existingMedia));
	}

	private async Task DeleteMedia(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		string idRaw = parameters["id"];

		if (!Guid.TryParse(idRaw, out var id)) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid media ID");
			return;
		}

		string token = request.GetAuthToken();
		string? userId = await _authHandler.GetUserIdFromTokenAsync(token);

		if (userId == null) {
			await response.WriteResponse(HttpStatusCode.Unauthorized, "Invalid token");
			return;
		}

		var existingMedia = await _mediaRepository.GetByIdAsync(id);
		if (existingMedia == null) {
			await response.WriteResponse(HttpStatusCode.NotFound, "Media not found");
			return;
		}

		if (existingMedia.CreatedBy != Guid.Parse(userId)) {
			await response.WriteResponse(HttpStatusCode.Forbidden, "You can only delete your own media");
			return;
		}

		await _mediaRepository.DeleteAsync(id);

		await response.WriteResponse(HttpStatusCode.NoContent, string.Empty);
	}


	private async Task RateMedia(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		string idRaw = parameters["id"];

		if (!Guid.TryParse(idRaw, out var id)) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid media ID");
			return;
		}

		var body = await request.ReadRequestBodyAsync();
		var ratingData = JsonSerializer.Deserialize<Rating>(body, JsonSerializerOptions.Web);

		if (ratingData == null || ratingData.Stars < 1 || ratingData.Stars > 5) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid rating data");
			return;
		}

		string token = request.GetAuthToken();

		string? userId = await _authHandler.GetUserIdFromTokenAsync(token);

		if (userId == null) {
			await response.WriteResponse(HttpStatusCode.Unauthorized, "Invalid token");
			return;
		}
		
		var existing = await _ratingRepository.GetByUserAndMediaAsync(Guid.Parse(userId), id);
		if (existing != null)
		{
			await response.WriteResponse(HttpStatusCode.Conflict, "Already rated");
			return;
		}

		ratingData.UserId = Guid.Parse(userId);
		ratingData.MediaId = id;

		await _ratingRepository.CreateAsync(ratingData);

		await response.WriteResponse(HttpStatusCode.OK, "Rating submitted successfully");
	}

	private async Task MarkAsFavorite(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		string mediaIdRaw = parameters["mediaId"];

		if (!Guid.TryParse(mediaIdRaw, out var mediaId)) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid media ID");
			return;
		}

		string token = request.GetAuthToken();

		string? userId = await _authHandler.GetUserIdFromTokenAsync(token);

		if (userId == null) {
			await response.WriteResponse(HttpStatusCode.Unauthorized, "Invalid token");
			return;
		}

		await _favoriteRepository.AddAsync(Guid.Parse(userId), mediaId);

		await response.WriteResponse(HttpStatusCode.OK, "Media marked as favorite successfully");
	}

	private async Task RemoveFavorite(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		string mediaIdRaw = parameters["mediaId"];

		if (!Guid.TryParse(mediaIdRaw, out var mediaId)) {
			await response.WriteResponse(HttpStatusCode.BadRequest, "Invalid media ID");
			return;
		}

		string token = request.GetAuthToken();

		string? userId = await _authHandler.GetUserIdFromTokenAsync(token);

		if (userId == null) {
			await response.WriteResponse(HttpStatusCode.Unauthorized, "Invalid token");
			return;
		}

		await _favoriteRepository.RemoveAsync(Guid.Parse(userId), mediaId);

		await response.WriteResponse(HttpStatusCode.OK, "Media removed from favorites successfully");
	}
	
	private async Task ListMedia(HttpListenerRequest request, HttpListenerResponse response)
	{
		int skip = int.TryParse(request.QueryString["skip"], out var s) ? s : 0;
		int take = int.TryParse(request.QueryString["take"], out var t) ? t : 100;

		string? title = request.QueryString["title"];
		string? genre = request.QueryString["genre"];
		string? mediaType = request.QueryString["mediaType"];
		string? sortBy = request.QueryString["sortBy"];

		int? releaseYear = int.TryParse(request.QueryString["releaseYear"], out var y) ? y : null;
		short? ageRestriction = short.TryParse(request.QueryString["ageRestriction"], out var a) ? a : null;
		int? rating = int.TryParse(request.QueryString["rating"], out var r) ? r : null;

		var list = await _mediaRepository.QueryAsync(
			title,
			genre,
			mediaType,
			releaseYear,
			ageRestriction,
			rating,
			sortBy,
			skip,
			take);

		await response.WriteResponse(HttpStatusCode.OK, JsonSerializer.Serialize(list));
	}
}