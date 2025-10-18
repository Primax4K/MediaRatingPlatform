namespace WebAPI.Routers.Abstract;

public abstract class ARouter {
	private readonly Dictionary<string, Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task>>>
		_handlers = new(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, Dictionary<string,
			Func<HttpListenerRequest, HttpListenerResponse, Dictionary<string, string>, Task>>>
		_patternHandlers = new(StringComparer.OrdinalIgnoreCase);

	protected void Register(string method, string path, Func<HttpListenerRequest, HttpListenerResponse, Task> handler) {
		path = NormalizePath(path);

		if (!_handlers.TryGetValue(path, out var methodMap)) {
			methodMap =
				new Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task>>(StringComparer
					.OrdinalIgnoreCase);
			_handlers[path] = methodMap;
		}

		methodMap[method] = handler;
	}

	protected void RegisterWithParams(string method, string pathPattern,
		Func<HttpListenerRequest, HttpListenerResponse, Dictionary<string, string>, Task> handler) {
		pathPattern = NormalizePath(pathPattern);

		if (!_patternHandlers.TryGetValue(pathPattern, out var methodMap)) {
			methodMap =
				new Dictionary<string,
					Func<HttpListenerRequest, HttpListenerResponse, Dictionary<string, string>, Task>>(StringComparer
					.OrdinalIgnoreCase);
			_patternHandlers[pathPattern] = methodMap;
		}

		methodMap[method] = handler;
	}

	private Dictionary<string, string> ExtractParameters(string pattern, string path) {
		var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var patternSegments = pattern.Split('/');
		var pathSegments = path.Split('/');

		if (patternSegments.Length != pathSegments.Length)
			return parameters;

		for (int i = 0; i < patternSegments.Length; i++) {
			if (patternSegments[i].StartsWith("{") && patternSegments[i].EndsWith("}")) {
				var paramName = patternSegments[i].Trim('{', '}');
				parameters[paramName] = pathSegments[i];
			}
		}

		return parameters;
	}

	private static string NormalizePath(string path) {
		if (string.IsNullOrEmpty(path))
			return "/";

		if (!path.StartsWith("/"))
			path = "/" + path;

		if (path.Length > 1 && path.EndsWith("/"))
			path = path.TrimEnd('/');

		return path;
	}

	public async Task Route(HttpListenerRequest request, HttpListenerResponse response, string basePath = "/") {
		var method = request.HttpMethod;
		var rawPath = request.Url?.AbsolutePath ?? request.RawUrl ?? "/";
		basePath = NormalizePath(basePath);
		var requestPath = NormalizePath(rawPath);

		string relativePath;
		if (basePath == "/") {
			relativePath = requestPath;
		}
		else if (requestPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) {
			relativePath = requestPath.Substring(basePath.Length);
			if (string.IsNullOrEmpty(relativePath) || relativePath == "/")
				relativePath = "/";
			else
				relativePath = NormalizePath(relativePath);
		}
		else {
			response.StatusCode = 404;
			return;
		}

		// Exakter Pfad
		if (_handlers.TryGetValue(relativePath, out var methodMap)) {
			if (!methodMap.TryGetValue(method, out var handler)) {
				response.StatusCode = 405;
				return;
			}

			await handler(request, response);
			return;
		}

		// Pattern-Match
		foreach (var (pattern, methods) in _patternHandlers) {
			if (MatchesPattern(pattern, relativePath)) {
				if (!methods.TryGetValue(method, out var handler)) {
					response.StatusCode = 405;
					return;
				}

				var parameters = ExtractParameters(pattern, relativePath);
				await handler(request, response, parameters);
				return;
			}
		}

		response.StatusCode = 404;
	}

	private bool MatchesPattern(string pattern, string path) {
		var patternSegments = pattern.Split('/');
		var pathSegments = path.Split('/');

		if (patternSegments.Length != pathSegments.Length)
			return false;

		for (int i = 0; i < patternSegments.Length; i++) {
			if (patternSegments[i].StartsWith("{") && patternSegments[i].EndsWith("}"))
				continue;

			if (!string.Equals(patternSegments[i], pathSegments[i], StringComparison.OrdinalIgnoreCase))
				return false;
		}

		return true;
	}
}