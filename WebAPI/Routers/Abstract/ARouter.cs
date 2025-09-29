using System.Net;

namespace WebAPI.Routers.Abstract;

public abstract class ARouter
{
    private readonly Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task>> _handlers =
        new(StringComparer.OrdinalIgnoreCase);

    protected void Register(string method, Func<HttpListenerRequest, HttpListenerResponse, Task> handler)
    {
        _handlers[method] = handler;
    }

    public async Task Route(HttpListenerRequest request, HttpListenerResponse response)
    {
        if (_handlers.TryGetValue(request.HttpMethod, out var handler))
            await handler(request, response);
        else
            response.StatusCode = 405;
    }
}