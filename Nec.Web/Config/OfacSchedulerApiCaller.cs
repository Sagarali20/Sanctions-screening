
using Microsoft.Extensions.DependencyInjection;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Utils;
using System.Net.Http;
using System.Text.Json;
using System.Xml.Serialization;

namespace Nec.Web.Config
{
    public class OfacSchedulerApiCaller : BackgroundService
    {
        private readonly ILogger<OfacSchedulerApiCaller> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        NecAppConfigForAcheduler _appConfig;
        private readonly IOfacService _ofacService;

        public OfacSchedulerApiCaller(ILogger<OfacSchedulerApiCaller> logger, IHttpClientFactory httpClientFactory, IServiceScopeFactory serviceScopeFactory, NecAppConfigForAcheduler necAppConfig)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _serviceScopeFactory = serviceScopeFactory;
            _appConfig = necAppConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Midnight API Caller started for Ofac.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var nextMidnight = now.Date.AddDays(1);
                    var delay = nextMidnight - now;

                    await Task.Delay(delay, stoppingToken);

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
            _logger.LogInformation("Midnight API Caller stopped for Ofac");
        }
        private async Task CallApiAsync()
        {
            //var client = _httpClientFactory.CreateClient();
            _logger.LogInformation("xxxxxxxxxxxxxxxxxxx--OFAC--xxxxxxxxxxxxxxxxxxxxxx");

            using var scope = _serviceScopeFactory.CreateScope();
            var OfacService = scope.ServiceProvider.GetRequiredService<IOfacService>();
            var SanctionService = scope.ServiceProvider.GetRequiredService<ISanctionService>();

            List<SanctionEntity> entities = new();

            try
            {


                string url = "https://sanctionslistservice.ofac.treas.gov/api/PublicationPreview/exports/SDN.XML";


                using (HttpClient client = new HttpClient()
                {
                    Timeout = TimeSpan.FromMinutes(30)
                })
                {
                    // Add a User-Agent header to avoid 403 (many servers block requests without one)
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MyApp/1.0)");

                    Console.WriteLine("Downloading OFAC SDN XML file...");

                    // Use GET, not POST
                    using (HttpResponseMessage response = await client.GetAsync(url))
                    {
                        response.EnsureSuccessStatusCode(); // throws if not 2xx
                        SdnList sdnList = new SdnList();


                        // Get the stream of data
                        using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                        {

                            XmlSerializer serializer = new XmlSerializer(typeof(SdnList));
                            sdnList = (SdnList)serializer.Deserialize(contentStream);

                            int id = OfacService.UpdateOfacSanctionSDN(sdnList.SdnEntries);

                            // Prepare filename and path
                            string fileName = $"Ofac_SDN_consolidated_data_file_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xml";
                        }
                        AMLSourceLog aMLSourceLog = new AMLSourceLog();
                        aMLSourceLog.Total = sdnList.SdnEntries.Count();
                        aMLSourceLog.FileVersion = "";
                        aMLSourceLog.FileName = $"Ofac_data_file_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                        aMLSourceLog.SourceName = "OFAC-SDN";
                        aMLSourceLog.SourceLink = "https://sanctionslistservice.ofac.treas.gov/api/PublicationPreview/exports/SDN.XML";
                        aMLSourceLog.SourceCountry = "U.S.";
                        int RowId = SanctionService.CreateAMLLog(aMLSourceLog);

                    }
                }


            }
            catch (Exception ex)
            {
                _logger.LogWarning("Ann error occurs in catch Ofac when updating data: " + ex.StackTrace.ToString());
            }

        }
    }
}
