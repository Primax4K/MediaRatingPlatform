using System.Net;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.HttpResponseHandler;
using WebAPI.Routers;
using WebAPI.Routers.Handler;

var services = new ServiceCollection();

services.AddSingleton<UserRouter>();

services.AddSingleton<RouterHandler>();

var provider = services.BuildServiceProvider();

var routerHandler = provider.GetRequiredService<RouterHandler>();
HttpListener listener = new HttpListener();
listener.Prefixes.Add("http://localhost:8080/");
listener.Start();
Console.WriteLine("Listening on http://localhost:8080/");

while (true)
{
    HttpListenerContext context = await listener.GetContextAsync();
    _ = Task.Run(async () =>
    {
        var request = context.Request;
        var response = context.Response;
        try
        {
            await routerHandler.Dispatch(request, response);
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await HttpResponseWriter.WriteResponse(response, $"<h1>Error: {ex.Message}</h1>");
        }
        finally
        {
            response.OutputStream.Close();
        }
    });
}