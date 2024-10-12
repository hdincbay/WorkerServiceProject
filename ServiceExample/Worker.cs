using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RestSharp;

namespace ServiceExample
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private IConfiguration _configuration { get; }


        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var result = string.Format("Worker running at: {0}", DateTimeOffset.Now);
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    try
                    {
                        var filepath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                        var stringContent = File.ReadAllText(filepath);
                        var appsettingsJObject = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(stringContent);
                        var page = appsettingsJObject!["valuesPageAndLimit"]?["page"]?.ToString();
                        var limit = appsettingsJObject!["valuesPageAndLimit"]?["limit"]?.ToString();
                        var userCreateEndpoint = appsettingsJObject!["userCreateEndpoint"]?.ToString();
                        var userPassword = appsettingsJObject!["userPassword"]?.ToString();
                        var userListEndpoint = appsettingsJObject!["userListEndpoint"]?.ToString();
                        string endpoint = string.Format("{0}={1}&limit={2}", userListEndpoint, page, limit);
                        var client = new RestClient();
                        var request = new RestRequest(endpoint, Method.Get);
                        var response = await client.ExecuteAsync(request);
                        var userList = new List<string>();
                        var responseJObject = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(response.Content!);
                        var userListByResponse = responseJObject!["data"]?["data"]?.ToString();
                        var userListJArray = Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(userListByResponse!);
                        foreach (var item in userListJArray!)
                        {
                            var userName = item["login"]?["username"]?.ToString();
                            var email = item["email"]?.ToString();
                            var endpointCreateUsr = string.Format("{0}/api/User/Create?userName={1}&email={2}&phoneNumber=1&password={3}", userCreateEndpoint, userName, email, userPassword);
                            var requestUserCreate = new RestRequest(endpointCreateUsr, Method.Post);


                            var responseUserCreate = await client.ExecuteAsync(requestUserCreate);
                            if (responseUserCreate.IsSuccessful)
                                _logger.LogInformation(string.Format("Kullanýcý oluþturuldu: {0}", userName));
                            else
                                _logger.LogInformation(string.Format("Hata: {0}", responseUserCreate.ErrorMessage));
                            _logger.LogInformation($"Worker running at: {DateTime.Now.ToString("HH:mm:ss")}");
                            //await Task.Delay(10000, stoppingToken);
                            
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(string.Format("Catch içerisinde: {0}", ex.ToString()));
                    }
                    
                }
            }
        }
    }
}
