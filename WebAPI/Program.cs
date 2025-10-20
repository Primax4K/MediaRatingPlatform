var services = new ServiceCollection();


var configuration = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.Build();
services.AddSingleton<IConfiguration>(configuration);


services.AddScoped<IAppUserRepository, AppUserRepository>();
services.AddScoped<IGenreRepository, GenreRepository>();
services.AddScoped<IMediaRepository, MediaRepository>();
services.AddScoped<IRatingRepository, RatingRepository>();
services.AddScoped<IRatingLikeRepository, RatingLikeRepository>();
services.AddScoped<IFavoriteRepository, FavoriteRepository>();


services.AddSingleton<IDbConnectionFactory>(_ =>
	new NpgsqlConnectionFactory(
		configuration.GetConnectionString("DefaultConnection")
		?? throw new Exception("No connection string found.")));


services.AddSingleton<UserRouter>();
services.AddSingleton<MediaRouter>();
services.AddSingleton<RouterHandler>();

services.AddScoped<IAuthHandler, AuthHandler>();


await using var provider = services.BuildServiceProvider();
var routerHandler = provider.GetRequiredService<RouterHandler>();


var listener = new HttpListener();
var serverUrl = configuration["server"] ?? "http://localhost:8080/";
listener.Prefixes.Add(serverUrl);
listener.Start();
Console.WriteLine($"Listening on {serverUrl}");

while (true) {
	var context = await listener.GetContextAsync();
	_ = Task.Run(async () => {
		var request = context.Request;
		var response = context.Response;

		try {
			await routerHandler.Dispatch(request, response);
		}
		catch (Exception ex) {
			response.StatusCode = 500;
			await response.WriteResponse($"Error: {ex.Message}");
			Console.WriteLine(ex);
		}
		finally {
			response.OutputStream.Close();
		}
	});
}