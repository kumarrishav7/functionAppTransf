using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace IngressService
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        private readonly Dictionary<string, string> _keyMap;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
            // Load mapping from file at startup (you can move this to DI for prod)
            try
            {
                var mappingJson = File.ReadAllText("mappings.json");
                _keyMap = JsonSerializer.Deserialize<Dictionary<string, string>>(mappingJson);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        [Function("Function1")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing request...");

            // Read request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var inputData = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody);

            // Transform based on mapping
            var outputData = new Dictionary<string, object>();
            foreach (var (inputKey, outputKey) in _keyMap)
            {
                if (inputData.TryGetValue(inputKey, out var value))
                {
                    outputData[outputKey] = value;
                }
            }

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(outputData);
            return response;
        }
    }
}
