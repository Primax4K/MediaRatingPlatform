namespace WebAPI.Routers;

public class UserRouter : ARouter, IHttpGetHandler
{
    public UserRouter()
    {
        Register("GET", HandleGet);
    }

    public async Task HandleGet(HttpListenerRequest request, HttpListenerResponse response)
    {
        await response.WriteResponse("<h1>User GET Handler</h1>");
    }
}