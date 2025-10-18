using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);

services.AddSingleton<UserRouter>();

services.AddSingleton<RouterHandler>();

var provider = services.BuildServiceProvider();

var routerHandler = provider.GetRequiredService<RouterHandler>();
var configuration = provider.GetRequiredService<IConfiguration>();

HttpListener listener = new HttpListener();

string serverUrl = configuration["server"] ?? "http://localhost:8080/";

listener.Prefixes.Add(serverUrl);
listener.Start();
Console.WriteLine($"Listening on {serverUrl}");

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
            await response.WriteResponse($"<h1>Error: {ex.Message}</h1>");
        }
        finally
        {
            response.OutputStream.Close();
        }
    });
}