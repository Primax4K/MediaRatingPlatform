namespace WebAPI.Extensions;

public static class HttpListenerRequestExtension {
	public static async Task<string> ReadRequestBodyAsync(this HttpListenerRequest request) {
		var stream = request.InputStream;
		if (stream.CanSeek)
			stream.Position = 0;

		using var reader = new StreamReader(stream, request.ContentEncoding, leaveOpen: true);
		return await reader.ReadToEndAsync().ConfigureAwait(false);
	}
}