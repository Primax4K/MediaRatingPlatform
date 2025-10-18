using Domain.Repositories.Interfaces;
using Model.Entities;

namespace WebAPI.Routers;

public class UserRouter : ARouter {
	private readonly IRatingLikeRepository _ratingLikeRepository;

	public UserRouter(IRatingLikeRepository ratingLikeRepository) {
		_ratingLikeRepository = ratingLikeRepository;
		Register(HttpMethod.Get.Method, "/abc/ta", HandleGet);
		RegisterWithParams(HttpMethod.Get.Method, "/abc/{id}", HandleGetWithId);
	}

	private async Task HandleGet(HttpListenerRequest request, HttpListenerResponse response) {
		await _ratingLikeRepository.AddAsync(
			Guid.Parse("a83b9ec8-d649-44b6-9cc0-606b174d718e"),
			Guid.Parse("2b71a163-014b-41cc-855f-6acdc89ae701")
		);
		await response.WriteResponse("<h1>User GET Handler</h1>");
	}

	private async Task HandleGetWithId(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		var id = parameters["id"];
		await response.WriteResponse($"<h1>User GET Handler - ID: {id}</h1>");
	}
}