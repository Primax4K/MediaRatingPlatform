namespace WebAPI.Routers;

public class MediaRouter : ARouter {
    private readonly IAuthHandler _authHandler;
    private readonly IMediaRepository _mediaRepository;

    public MediaRouter(IAuthHandler authHandler, IMediaRepository mediaRepository) : base(authHandler) {
        _authHandler = authHandler;
        _mediaRepository = mediaRepository;

        Register(HttpMethod.Post.Method, "/", CreateMedia, requiresAuth: true);
        RegisterWithParams(HttpMethod.Get.Method, "/{id}", GetMediaById, requiresAuth: true);
        RegisterWithParams(HttpMethod.Put.Method, "/{id}", UpdateMedia, requiresAuth: true);
        RegisterWithParams(HttpMethod.Delete.Method, "/{id}", DeleteMedia, requiresAuth: true);
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
        
        var existingMedia = await _mediaRepository.GetByIdAsync(id);
        if (existingMedia == null) {
            await response.WriteResponse(HttpStatusCode.NotFound, "Media not found");
            return;
        }
        
        await _mediaRepository.DeleteAsync(id);
        
        await response.WriteResponse(HttpStatusCode.NoContent, string.Empty);
    }
}