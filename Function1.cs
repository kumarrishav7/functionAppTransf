using System.Text.Json;
using System.Web;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace IngressService
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        private Dictionary<string, string> _keyMap;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("Function1")]
        [ServiceBusOutput("dev-messages", Connection = "ServiceBusConnection", EntityType = ServiceBusEntityType.Topic)]
        public async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            try
            {
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                string containerName = "mappers";
                // string blobName = "mappings.json";

                // Extract mapping file name from query string
                var query = HttpUtility.ParseQueryString(req.Url.Query);
                string blobName = query["mapper"]; // Default fallback

                var blobClient = new BlobClient(connectionString, containerName, blobName);
                var blobContent = blobClient.DownloadContent().Value.Content.ToString();
                _keyMap = JsonSerializer.Deserialize<Dictionary<string, string>>(blobContent)
                          ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load mapping file from blob storage.");
                _keyMap = new Dictionary<string, string>();
            }
            _logger.LogInformation("Processing request...");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var inputData = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody);

            var outputData = new Dictionary<string, object>();
            foreach (var (inputKey, outputKey) in _keyMap)
            {
                if (inputData.TryGetValue(inputKey, out var value))
                {
                    outputData[outputKey] = value;
                }
            }
            return JsonSerializer.Serialize(outputData);
        }
    }
}
