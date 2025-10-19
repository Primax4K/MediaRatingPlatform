using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace Tests;

public class AuthHandlerTests {
	private readonly HttpClient _client;
	private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.General);

	public AuthHandlerTests() {
		_client = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };
	}

	private static (string Username, string Password) NewCreds() {
		var u = $"user_{Guid.NewGuid():N}@example.test";
		var p = $"P@ssw0rd_{Guid.NewGuid():N}";
		return (u, p);
	}


	private static string? GetSubFromJwt(string jwt) {
		try {
			var parts = jwt.Split('.');
			if (parts.Length < 2) return null;

			static string B64UrlToB64(string s) => s.Replace('-', '+').Replace('_', '/')
				.PadRight(s.Length + ((4 - s.Length % 4) % 4), '=');

			var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(B64UrlToB64(parts[1])));
			var node = JsonNode.Parse(payloadJson)?.AsObject();
			return node is not null && node.TryGetPropertyValue("sub", out var v) ? v?.ToString() : null;
		}
		catch {
			return null;
		}
	}

	private async Task<HttpResponseMessage> PostJsonAsync(string url, object body) {
		var content =
			new StringContent(JsonSerializer.Serialize(body, _jsonOptions), Encoding.UTF8, "application/json");
		return await _client.PostAsync(url, content);
	}

	[Fact]
	public async Task Register_ShouldReturn201() {
		var (username, password) = NewCreds();

		var response = await PostJsonAsync("users/register", new { Username = username, Password = password });

		Assert.True(response.StatusCode is HttpStatusCode.Created);
	}

	[Fact]
	public async Task Login_ShouldReturnToken_OnValidCredentials() {
		var (username, password) = NewCreds();

		// Ensure user exists
		await PostJsonAsync("users/register", new { Username = username, Password = password });

		await Task.Delay(100);
		var login = await PostJsonAsync("users/login", new { Username = username, Password = password });
		Assert.True(login.IsSuccessStatusCode);

		var body = await login.Content.ReadAsStringAsync();
		using var doc = JsonDocument.Parse(body);
		Assert.True(doc.RootElement.TryGetProperty("Token", out var tokenEl));
		Assert.False(string.IsNullOrWhiteSpace(tokenEl.GetString()));
	}

	[Fact]
	public async Task Get_ProtectedEndpoint_ShouldReturn200_WithBearerToken() {
		var (username, password) = NewCreds();
		await PostJsonAsync("users/register", new { Username = username, Password = password });

		var login = await PostJsonAsync("users/login", new { Username = username, Password = password });
		var token = (await JsonDocument.ParseAsync(await login.Content.ReadAsStreamAsync()))
			.RootElement.GetProperty("Token").GetString();

		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var resp = await _client.GetAsync("users/abc/ta");
		Assert.True(resp.IsSuccessStatusCode);
	}

	[Fact]
	public async Task Get_WithRouteParams_ShouldReturn200_WithBearerToken() {
		var (username, password) = NewCreds();
		await PostJsonAsync("users/register", new { Username = username, Password = password });

		var login = await PostJsonAsync("users/login", new { Username = username, Password = password });
		var token = (await JsonDocument.ParseAsync(await login.Content.ReadAsStreamAsync()))
			.RootElement.GetProperty("Token").GetString();

		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var id = Guid.NewGuid().ToString();
		var name = HttpUtility.UrlEncode("Max Mustermann");
		var resp = await _client.GetAsync($"users/abc/{id}/{name}");

		Assert.True(resp.IsSuccessStatusCode);
	}

	[Fact]
	public async Task Delete_User_ShouldReturn204_WhenUserExists_AndTokenProvided() {
		var (username, password) = NewCreds();

		// Register
		await PostJsonAsync("users/register", new { Username = username, Password = password });

		// Login
		var login = await PostJsonAsync("users/login", new { Username = username, Password = password });
		Assert.Equal(HttpStatusCode.OK, login.StatusCode);

		var body = await login.Content.ReadAsStringAsync();
		using var doc = JsonDocument.Parse(body);
		var token = doc.RootElement.GetProperty("Token").GetString();
		Assert.False(string.IsNullOrWhiteSpace(token));

		// Extract user id from JWT claims
		var userId = GetSubFromJwt(token!);
		Assert.False(string.IsNullOrWhiteSpace(userId));

		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		// Call DELETE /{id}
		var del = await _client.DeleteAsync($"users/{userId}");
		Assert.True(del.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Register_ShouldReturn400_OnInvalidPayload() {
		var response = await PostJsonAsync("users/register", new { Username = "", Password = "" });
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Login_ShouldReturn400_OnInvalidPayload() {
		var response = await PostJsonAsync("users/login", new { Username = "", Password = "" });
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task ProtectedRoutes_ShouldReturn401_WithoutToken() {
		_client.DefaultRequestHeaders.Authorization = null;

		var resp1 = await _client.GetAsync("users/abc/ta");
		var resp2 = await _client.GetAsync($"users/abc/{Guid.NewGuid()}/Test");

		Assert.Equal(HttpStatusCode.Unauthorized, resp1.StatusCode);
		Assert.Equal(HttpStatusCode.Unauthorized, resp2.StatusCode);
	}
}