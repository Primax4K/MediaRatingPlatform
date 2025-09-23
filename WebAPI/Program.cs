using WebAPI.ExtensionMethods;
using WebAPI.Http;
using WebAPI.Routers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

builder.Services.AddSingleton<CustomHttpHandler>();

builder.Services.AddScoped<UserRouter>();



var app = builder.Build();
/*
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
*/
//app.UseHttpsRedirection();

app.InitMRP([
    new HttpFunction(HttpMethod.Get, "/profil", async () =>
    {
        Console.WriteLine("Profile wurde aufgerufen");
        await Task.CompletedTask;
    })
]);


app.Run();