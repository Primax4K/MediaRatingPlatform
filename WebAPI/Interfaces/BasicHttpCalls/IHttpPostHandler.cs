namespace WebAPI.Interfaces.Http;

public interface IHttpPostHandler
{
    Task HandlePost(HttpListenerRequest request, HttpListenerResponse response);
}