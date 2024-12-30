using System.Net;
using Microsoft.Extensions.Logging;

namespace DesktopDocumentSigner
{
    public static class MauiProgram
    {
        private const string listenOn = "http://localhost:5000/";
        private static HttpListener _httpListener = new HttpListener();

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            StartHttpServer();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private async static void StartHttpServer()
        {
            _httpListener.Prefixes.Add(listenOn);

            try
            {
                _httpListener.Start();

                while (true)
                {
                    var context = await _httpListener.GetContextAsync();
                    HandleRequest(context);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private async static void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                if (request.HttpMethod == "POST")
                {
                    HandlePostRequest(request, response);
                }
                else
                {
                    HandleDefaultRequest(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling request: {ex.Message}");
            }
        }

        private static void HandleDefaultRequest(HttpListenerResponse response)
        {
            string responseString = "Pink OK!";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;

            using (var output = response.OutputStream)
            {
                output.Write(buffer, 0, buffer.Length);
            }
        }

        private static void HandlePostRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    string requestBody = reader.ReadToEnd();
                    Console.WriteLine($"Received POST data: {requestBody}");

                    string responseString = $"Received your POST data: {requestBody}";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;

                    using (var output = response.OutputStream)
                    {
                        output.Write(buffer, 0, buffer.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing POST request: {ex.Message}");
            }
        }
    }
}
