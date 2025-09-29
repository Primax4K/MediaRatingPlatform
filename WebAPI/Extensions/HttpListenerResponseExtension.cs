using System.Net;
using WebAPI.HttpResponseHandler;

namespace WebAPI.Extensions;

public static class HttpListenerResponseExtension
{
    public static async Task WriteResponse(this HttpListenerResponse response, string responseMessage) 
        => await HttpResponseWriter.WriteResponse(response, responseMessage);
}