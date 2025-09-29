namespace WebAPI.HttpResponseHandler;

public static class HttpResponseWriter
{
    public static async Task WriteResponse(HttpListenerResponse response, string content, string contentType = "text/html")
    {
        response.ContentType = contentType;
        byte[] buffer = Encoding.UTF8.GetBytes(content);
        response.ContentLength64 = buffer.Length;
        await using var output = response.OutputStream;
        await output.WriteAsync(buffer);
    }
}