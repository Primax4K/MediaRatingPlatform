namespace WebAPI.Interfaces.Http;

public interface IHttpGetHandler
{
    Task HandleGet(HttpListenerRequest request, HttpListenerResponse response);
}