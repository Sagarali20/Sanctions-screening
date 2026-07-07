using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Utils;
using System.Xml;
using System.Xml.Serialization;

namespace Nec.Web.Config
{
    public class UKSchedulerApiCaller: BackgroundService
    {
        private readonly ILogger<UKSchedulerApiCaller> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        NecAppConfigForAcheduler _appConfig;
        private readonly IOfacService _ofacService;

        public UKSchedulerApiCaller(ILogger<UKSchedulerApiCaller> logger, IHttpClientFactory httpClientFactory, IServiceScopeFactory serviceScopeFactory, NecAppConfigForAcheduler necAppConfig)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _serviceScopeFactory = serviceScopeFactory;
            _appConfig = necAppConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Midnight API Caller started for UKSanction.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var nextMidnight = now.Date.AddDays(1);
                    var delay = nextMidnight - now;

                    await Task.Delay(9000, stoppingToken);

                    await CallApiAsync();

                    _logger.LogInformation("API called at: {time}", DateTimeOffset.Now);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Midnight API caller cancelled.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in API caller.");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // wait before retrying
                }
            }
            _logger.LogInformation("Midnight API Caller stopped for UKSanction");
        }
        private async Task CallApiAsync()
        {
            //var client = _httpClientFactory.CreateClient();
            _logger.LogInformation("xxxxxxxxxxxxxxxxxxx--UKSanction--xxxxxxxxxxxxxxxxxxxxxx");

            using var scope = _serviceScopeFactory.CreateScope();
            var UKService = scope.ServiceProvider.GetRequiredService<IUKService>();
            var SanctionService = scope.ServiceProvider.GetRequiredService<ISanctionService>();

            List<SanctionEntity> entities = new();

            try
            {
                var client = new HttpClient();

                using var stream = await client.GetStreamAsync("https://sanctionslist.fcdo.gov.uk/docs/UK-Sanctions-List.xml");

                XmlSerializer serializer = new XmlSerializer(typeof(Designations));

                XmlReaderSettings settings = new XmlReaderSettings
                {
                    IgnoreWhitespace = true
                };

                using XmlReader reader = XmlReader.Create(stream, settings);

                Designations data = (Designations)serializer.Deserialize(reader);

                int totalRecords = data.DesignationList.Count;


                UKService.CreateUKSanctionBulk(data);


            }
            catch (Exception ex)
            {
                _logger.LogWarning("Ann error occurs in catch Ofac when updating data: " + ex.StackTrace.ToString());
            }

        }
    }
}
