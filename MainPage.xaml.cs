using System.Collections.ObjectModel;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Bcpg;

namespace DesktopDocumentSigner
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        public ObservableCollection<string> LogEntries { get; set; }
        private static string _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "settings.json");
        private Settings? settings;
        private const string _listenOn = "http://127.0.0.1:8080/";
        private static HttpListener _httpListener = new HttpListener();

        public MainPage()
        {
            InitializeComponent();
            LogEntries = new ObservableCollection<string>
            {
                "App started"
            };
            BindingContext = this;
            LoadSettings();
            StartHttpServer();
        }

        private async void StartHttpServer()
        {
            _httpListener.Prefixes.Add(_listenOn);

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

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

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

        private void HandleDefaultRequest(HttpListenerResponse response)
        {
            string responseString = "Pink OK!";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;

            using (var output = response.OutputStream)
            {
                output.Write(buffer, 0, buffer.Length);
            }

        }

        private void HandlePostRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    var requestBody = reader.ReadToEnd();
                    var parsedData = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);

                    if (parsedData is not null && settings is not null)
                    {
                        var signedFilesCollection = new Dictionary<string, string>();

                        foreach (var kvp in parsedData)
                        {
                            string documentName = kvp.Key;
                            string value = kvp.Value;

                            DocumentSigner documentSigner = new DocumentSigner(settings.CertificateHash, value, documentName, settings.SignatureLocation);
                            SignatureResult signatureResult = documentSigner.SignDocument();
                            signedFilesCollection.Add(documentName, signatureResult.SignedDocumentBase64);
                            LogEntries.Insert(0, signatureResult.ResultText);
                        }

                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.ContentType = "application/json";
                        var responseJson = JsonSerializer.Serialize(signedFilesCollection);

                        using (var writer = new StreamWriter(response.OutputStream))
                        {
                            writer.Write(responseJson);
                        }
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                using (var writer = new StreamWriter(response.OutputStream))
                {
                    writer.Write(JsonSerializer.Serialize(new { error = ex.Message }));
                }
            }
        }

        private void LoadSettings()
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                settings = JsonSerializer.Deserialize<Settings>(json);

                CertificateHashEntry.Text = settings.CertificateHash;
                LocationEntry.Text = settings.SignatureLocation;
            }
        }

        private void Save_Clicked(object sender, EventArgs e)
        {
            var settings = new Settings
            {
                CertificateHash = CertificateHashEntry.Text,
                SignatureLocation = LocationEntry.Text
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            X509Certificate2? certificate = DocumentSigner.GetCertificateFromStore(settings.CertificateHash);

            if (certificate == null)
            {
                DisplayAlert("Error", $"Certificate with fingerprint {settings.CertificateHash} was not found.", "OK");
                return;
            }

            try
            {
                File.WriteAllText(_filePath, json);
                LogEntries.Insert(0, "Configuration Saved");
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"An error occurred while saving settings: {ex.Message}", "OK");
            }
        }
    }

}
