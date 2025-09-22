using System.Net;
using System.Text;

namespace WebAPI.Http;

public class CustomHttpHandler
{
    public HttpListener Listener { get; set; }


    public CustomHttpHandler()
    {
        Listener = new HttpListener();
    }

    public async Task StartListener(HttpFunction[] functions)
    {
        try
        {
            Listener.Prefixes.Add("http://localhost:8080/");
            Listener.Start();
            Console.WriteLine("Listening for incoming requests on http://localhost:8080/");

            if (!functions.Any())
                throw new Exception("No functions found");

            if (functions.GroupBy(f => new { f.Path, f.HttpMethod }).Any(g => g.Count() > 1))
                throw new Exception("Duplicate function(s) found");

            while (true)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await Listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                //ignore favicon.ico requests
                if (req?.Url?.AbsolutePath == "/favicon.ico")
                    continue;

                // Print out some info about the request
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();


                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/shutdown"))
                {
                    Console.WriteLine("Shutdown requested");
                }

                HttpFunction? function = functions.FirstOrDefault(f => f.HttpMethod.Method == req.HttpMethod &&
                                                                       f.Path.Equals(req.Url.AbsolutePath,
                                                                           StringComparison
                                                                               .InvariantCultureIgnoreCase));

                if (function is not null)
                    await function.Function();


                // Write the response info
                byte[] data = Encoding.UTF8.GetBytes(String.Format("pageData", "pageViews", "disableSubmit"));
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An exception occured: ");
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            Console.WriteLine("Stopping listener...");
            Listener.Stop();
        }
    }
}