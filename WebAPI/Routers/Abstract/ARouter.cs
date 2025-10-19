using WebAPI.Auth;

namespace WebAPI.Routers.Abstract;

public abstract class ARouter(IAuthHandler authHandler) {
	private readonly Dictionary<string, Dictionary<string, (RequestHandler Handler, bool RequiresAuth)>> _handlers =
		new(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, Dictionary<string, (ParamRequestHandler Handler, bool RequiresAuth)>>
		_patternHandlers =
			new(StringComparer.OrdinalIgnoreCase);

	protected void Register(string method, string path, RequestHandler handler, bool requiresAuth = true) {
		path = path.NormalizePath();
		if (!_handlers.TryGetValue(path, out var methods))
			_handlers[path] = methods = new(StringComparer.OrdinalIgnoreCase);

		methods[method] = (handler, requiresAuth);
	}

	protected void RegisterWithParams(string method, string pathPattern, ParamRequestHandler handler,
		bool requiresAuth = true) {
		pathPattern = pathPattern.NormalizePath();
		if (!_patternHandlers.TryGetValue(pathPattern, out var methods))
			_patternHandlers[pathPattern] = methods = new(StringComparer.OrdinalIgnoreCase);

		methods[method] = (handler, requiresAuth);
	}

	public async Task Route(HttpListenerRequest request, HttpListenerResponse response, string basePath = "/") {
		var method = request.HttpMethod;
		var rawPath = request.Url?.AbsolutePath ?? request.RawUrl ?? "/";
		var requestPath = rawPath.NormalizePath();
		basePath = basePath.NormalizePath();

		var relativePath = requestPath.GetRelativePath(basePath);
		if (relativePath is null) {
			response.StatusCode = 404;
			return;
		}

		if (await _handlers.TryHandleExact(relativePath, method, request, response, IsAuthenticated)) return;
		if (await _patternHandlers.TryHandlePattern(relativePath, method, request, response, IsAuthenticated)) return;

		response.StatusCode = 404;
	}

	private async Task<bool> IsAuthenticated(HttpListenerRequest request) {
		var authHeader = request.Headers["Authorization"];
		if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
			return false;

		var token = authHeader["Bearer ".Length..];
		return await authHandler.VerifyTokenAsync(token);
	}
}