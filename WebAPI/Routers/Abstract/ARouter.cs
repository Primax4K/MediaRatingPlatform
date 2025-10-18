namespace WebAPI.Routers.Abstract;

public abstract class ARouter {
	// Outer: Pfad (case-insensitive)
	// Inner: HTTP-Methode (case-insensitive) -> Handler
	private readonly Dictionary<string, Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task>>>
		_handlers =
			new(StringComparer.OrdinalIgnoreCase);

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

		// Prüfe, ob requestPath unter dem basePath liegt und bestimme den relativen Pfad
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
			response.StatusCode = 404; // Not Found (basePath nicht vorhanden)
			return;
		}

		if (!_handlers.TryGetValue(relativePath, out var methodMap)) {
			response.StatusCode = 404; // Not Found
			return;
		}

		if (!methodMap.TryGetValue(method, out var handler)) {
			response.StatusCode = 405; // Method Not Allowed
			return;
		}

		await handler(request, response);
	}
}