namespace WebAPI.Extensions;

public static class HttpListenerResponseExtension {
	public static async Task WriteResponse(this HttpListenerResponse response, string content, string contentType = "text/html") {
		response.ContentType = contentType;
		byte[] buffer = Encoding.UTF8.GetBytes($"<h1>{content}</h1>");
		response.ContentLength64 = buffer.Length;
		await using var output = response.OutputStream;
		await output.WriteAsync(buffer);
	}

	public static async Task WriteResponse(this HttpListenerResponse response, HttpStatusCode statusCode, string content, string contentType = "text/html") {
		response.StatusCode = (int)statusCode;
		await response.WriteResponse(content, contentType);
	}
}