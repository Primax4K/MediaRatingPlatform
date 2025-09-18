using WebAPI.Http;

namespace WebAPI.ExtensionMethods;

public static class AddHttpListenerExtension
{
    public static async Task InitMRP(this WebApplication app, HttpFunction[] functions)
    {
        var httpHandler = app.Services.GetRequiredService<CustomHttpHandler>();
        await httpHandler.StartListener(functions);
    }
}