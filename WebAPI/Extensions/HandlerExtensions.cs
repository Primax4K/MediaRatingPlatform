namespace WebAPI.Extensions;

public static class HandlerExtensions {
	public static async Task<bool> TryHandleExact(
		this Dictionary<string, Dictionary<string, RequestHandler>> handlers,
		string path,
		string method,
		HttpListenerRequest request,
		HttpListenerResponse response) {
		if (!handlers.TryGetValue(path, out var methods)) return false;

		if (!methods.TryGetValue(method, out var handler)) {
			response.StatusCode = 405;
			return true;
		}

		await handler(request, response);
		return true;
	}

	public static async Task<bool> TryHandlePattern(
		this Dictionary<string, Dictionary<string, ParamRequestHandler>> patternHandlers,
		string path,
		string method,
		HttpListenerRequest request,
		HttpListenerResponse response) {
		
		foreach (var (pattern, methods) in patternHandlers) {
			if (!pattern.MatchesPattern(path)) continue;

			if (!methods.TryGetValue(method, out var handler)) {
				response.StatusCode = 405;
				return true;
			}

			var parameters = pattern.ExtractParameters(path);
			await handler(request, response, parameters);
			return true;
		}

		return false;
	}
}