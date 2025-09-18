namespace WebAPI.Http;

public class HttpFunction
{
    public HttpMethod HttpMethod { get; private set; }
    public Func<Task> Function { get; private set; }
    public string Path { get; private set; }

    public HttpFunction(HttpMethod httpMethod, string path, Func<Task> function)
    {
        HttpMethod = httpMethod;
        Path = path;
        Function = function;
    }
}