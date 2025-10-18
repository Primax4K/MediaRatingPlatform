namespace WebAPI.Interfaces.Http;

public interface IHttpDeleteHandler
{
    Task HandleDelete(HttpListenerRequest request, HttpListenerResponse response);
}