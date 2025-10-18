namespace WebAPI.Routers.Abstract;

public abstract class ARouter {

	private readonly Dictionary<string, Dictionary<string, RequestHandler>> _handlers =
		new(StringComparer.OrdinalIgnoreCase);

	private readonly
		Dictionary<string, Dictionary<string,
			ParamRequestHandler>> _patternHandlers
			= new(StringComparer.OrdinalIgnoreCase);

	protected void Register(string method, string path, RequestHandler handler) {
		path = path.NormalizePath();
		if (!_handlers.TryGetValue(path, out var methods))
			_handlers[path] = methods = new(StringComparer.OrdinalIgnoreCase);
		methods[method] = handler;
	}

	protected void RegisterWithParams(string method, string pathPattern,
		ParamRequestHandler handler) {
		pathPattern = pathPattern.NormalizePath();
		if (!_patternHandlers.TryGetValue(pathPattern, out var methods))
			_patternHandlers[pathPattern] = methods = new(StringComparer.OrdinalIgnoreCase);
		methods[method] = handler;
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

		if (await _handlers.TryHandleExact(relativePath, method, request, response)) return;
		if (await _patternHandlers.TryHandlePattern(relativePath, method, request, response)) return;

		response.StatusCode = 404;
	}
}