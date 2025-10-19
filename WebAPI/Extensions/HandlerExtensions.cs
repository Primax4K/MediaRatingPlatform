namespace WebAPI.Extensions;

public static class HandlerExtensions {
	public static async Task<bool> TryHandleExact(
		this Dictionary<string, Dictionary<string, (RequestHandler Handler, bool RequiresAuth)>> handlers,
		string path,
		string method,
		HttpListenerRequest request,
		HttpListenerResponse response,
		Func<HttpListenerRequest, Task<bool>> isAuthenticated) {
		if (!handlers.TryGetValue(path, out var methods)) return false;

		if (!methods.TryGetValue(method, out var handlerInfo)) {
			response.StatusCode = 405;
			return true;
		}

		if (handlerInfo.RequiresAuth && !await isAuthenticated(request)) {
			response.StatusCode = 401;
			await response.WriteResponse(401, "<h1>Unauthorized</h1>");
			return true;
		}

		await handlerInfo.Handler(request, response);
		return true;
	}

	public static async Task<bool> TryHandlePattern(
		this Dictionary<string, Dictionary<string, (ParamRequestHandler Handler, bool RequiresAuth)>> patternHandlers,
		string path,
		string method,
		HttpListenerRequest request,
		HttpListenerResponse response,
		Func<HttpListenerRequest, Task<bool>> isAuthenticated) {
		foreach (var (pattern, methods) in patternHandlers) {
			if (!pattern.MatchesPattern(path)) continue;

			if (!methods.TryGetValue(method, out var handlerInfo)) {
				response.StatusCode = 405;
				return true;
			}

			if (handlerInfo.RequiresAuth && !await isAuthenticated(request)) {
				response.StatusCode = 401;
				await response.WriteResponse(401, "<h1>Unauthorized</h1>");
				return true;
			}

			var parameters = pattern.ExtractParameters(path);
			await handlerInfo.Handler(request, response, parameters);
			return true;
		}

		return false;
	}
}