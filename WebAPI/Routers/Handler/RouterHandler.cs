using System.Net;
using WebAPI.Routers.Abstract;

namespace WebAPI.Routers.Handler;

public class RouterHandler
{
    private readonly UserRouter _usersRouter;

    public RouterHandler(UserRouter usersRouter)
    {
        _usersRouter = usersRouter;
    }

    public async Task Dispatch(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url.AbsolutePath.ToLower();

        if (path.StartsWith("/users"))
            await _usersRouter.Route(request, response);
        else
            response.StatusCode = 404;
    }
}