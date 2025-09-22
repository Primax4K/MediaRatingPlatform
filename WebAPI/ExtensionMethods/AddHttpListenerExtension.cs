using WebAPI.Http;

namespace WebAPI.ExtensionMethods;

public static class AddHttpListenerExtension
{
    public static void InitMRP(this WebApplication app, HttpFunction[] functions)
    {
        CustomHttpHandler httpHandler = new CustomHttpHandler();
        //var httpHandler = app.Services.GetRequiredService<CustomHttpHandler>();
        Task.Run(async () => await httpHandler.StartListener(functions));
    }
}