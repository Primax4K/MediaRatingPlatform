namespace WebAPI.Routers;

public class UserRouter : ARouter {
	public UserRouter() {
		Register(HttpMethod.Get.Method, "/abc/ta", HandleGet);
		RegisterWithParams(HttpMethod.Get.Method, "/abc/{id}", HandleGetWithId);
	}

	public async Task HandleGet(HttpListenerRequest request, HttpListenerResponse response) {
		await response.WriteResponse("<h1>User GET Handler</h1>");
	}

	public async Task HandleGetWithId(HttpListenerRequest request, HttpListenerResponse response,
		Dictionary<string, string> parameters) {
		var id = parameters["id"];
		await response.WriteResponse($"<h1>User GET Handler - ID: {id}</h1>");
	}
}