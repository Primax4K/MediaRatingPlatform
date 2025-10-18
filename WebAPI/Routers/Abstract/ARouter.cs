namespace WebAPI.Routers.Abstract;

public abstract class ARouter {
	// Outer: Pfad (case-insensitive)
	// Inner: HTTP-Methode (case-insensitive) -> Handler
	private readonly Dictionary<string, Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task>>>
		_handlers =
			new(StringComparer.OrdinalIgnoreCase);

	protected void Register(string path, string method, Func<HttpListenerRequest, HttpListenerResponse, Task> handler) {
		if (!_handlers.TryGetValue(path, out var methodMap)) {
			methodMap =
				new Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task>>(StringComparer
					.OrdinalIgnoreCase);
			_handlers[path] = methodMap;
		}

		methodMap[method] = handler;
	}

	public async Task Route(HttpListenerRequest request, HttpListenerResponse response) {
		var method = request.HttpMethod;
		var path = request.Url?.AbsolutePath ?? request.RawUrl ?? "/";

		if (!_handlers.TryGetValue(path, out var methodMap)) {
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