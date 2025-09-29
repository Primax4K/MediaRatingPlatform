using System.Net;
using WebAPI.Routers.Abstract;

namespace WebAPI.Routers.Handler;

public class RouterHandler
{
    private readonly Dictionary<string, ARouter> _routers;

    public RouterHandler(UserRouter usersRouter)
    {
        _routers = new Dictionary<string, ARouter>(StringComparer.OrdinalIgnoreCase)
        {
            { "/users", usersRouter },
        };
    }

    public async Task Dispatch(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url.AbsolutePath.ToLower();

        foreach (var kv in _routers)
        {
            if (path.StartsWith(kv.Key))
            {
                await kv.Value.Route(request, response);
                return;
            }
        }

        response.StatusCode = 404;
    }
}