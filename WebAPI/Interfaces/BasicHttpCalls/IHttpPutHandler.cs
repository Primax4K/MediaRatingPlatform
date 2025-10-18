namespace WebAPI.Interfaces.Http;

public interface IHttpPutHandler
{
    Task HandlePut(HttpListenerRequest request, HttpListenerResponse response);
}