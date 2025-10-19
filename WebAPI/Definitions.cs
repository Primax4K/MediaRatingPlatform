global using RequestHandler =
	System.Func<System.Net.HttpListenerRequest, System.Net.HttpListenerResponse, System.Threading.Tasks.Task>;
global using ParamRequestHandler =
	System.Func<System.Net.HttpListenerRequest, System.Net.HttpListenerResponse,
		System.Collections.Generic.Dictionary<string, string>, System.Threading.Tasks.Task>;