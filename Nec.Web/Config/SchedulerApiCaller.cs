using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Utils;
namespace Nec.Web.Config
{
    public class SchedulerApiCaller: BackgroundService
    {
        private readonly ILogger<SchedulerApiCaller> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        NecAppConfigForAcheduler _appConfig;

        public SchedulerApiCaller(ILogger<SchedulerApiCaller> logger, IHttpClientFactory httpClientFactory, IServiceScopeFactory serviceScopeFactory, NecAppConfigForAcheduler necAppConfig)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _serviceScopeFactory = serviceScopeFactory;
            _appConfig = necAppConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Midnight API Caller started.");

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

            _logger.LogInformation("Midnight API Caller stopped.");
        }

        private async Task CallApiAsync()
        {
            //var client = _httpClientFactory.CreateClient();
            _logger.LogInformation("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");

            using var scope = _serviceScopeFactory.CreateScope();
            var sanctionService = scope.ServiceProvider.GetRequiredService<ISanctionService>();

            List<SanctionEntity> entities = new();

            try
            {  
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(40)
                };

                string? FileVersion = await sanctionService.GetFileVersion();

                var request = new HttpRequestMessage(HttpMethod.Get, _appConfig.DilisenseUrl + FileVersion);
                request.Headers.Add("x-api-key", _appConfig.APIKey);
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {

                    string Version = string.Empty;

                    foreach (var header in response.Headers)
                    {
                        if (header.Key == "File-Version")
                        {
                            Version = string.Join(", ", header.Value);
                        }
                    }

                    string responseBody = await response.Content.ReadAsStringAsync();
                    string[] jsonArray = responseBody.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    int TotalPrivious = 0, TotalNew = 0, TotalUpdate = 0, TotalDelete = 0;
                     

                    AMLSourceLog aMLSourceLog = new AMLSourceLog();
                    aMLSourceLog.Total = jsonArray.Count();
                    aMLSourceLog.FileVersion = Version;
                    aMLSourceLog.FileName = $"Dilisense_consolidated_data_file_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                    aMLSourceLog.SourceName = "Dilisense";
                    aMLSourceLog.SourceLink = _appConfig.DilisenseUrl + FileVersion;
                    aMLSourceLog.SourceCountry = "Zurich and Luxembourg";

                    int RowId = sanctionService.CreateAMLLog(aMLSourceLog);
                    aMLSourceLog.TotalPrivious = await sanctionService.TotalDataCount();
                    int Totaldownload = 0;


                    foreach (var line in jsonArray)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        Totaldownload++;
                        ConsolidatedDelta entity = JsonSerializer.Deserialize<ConsolidatedDelta>(line)!;
                        if (entity.type == "UPDATE")
                        {
                            TotalUpdate++;
                            bool Res = sanctionService.UpdateSanction(entity.record);
                        }
                        else if (entity.type == "ADD")
                        {  
                            TotalNew++;
                            entity.record.VersionId = RowId;
                            bool Res = sanctionService.CreateSanctionNew(entity.record);
                        }
                        else if (entity.type == "DELETE")
                        {
                            TotalDelete++;
                            bool Res = sanctionService.DeleteSanction(entity.record.id);
                        }

                    }

                    aMLSourceLog.TotalNew = TotalNew;
                    aMLSourceLog.TotalUpdate = TotalUpdate;
                    aMLSourceLog.TotalDelete = TotalDelete;
                    aMLSourceLog.TotalData = Totaldownload;


                    var res = sanctionService.CreateAMLDataStatusLog(aMLSourceLog);

                }
                else
                {
                    Console.WriteLine($"Request failed with status code: {response.StatusCode} ({(int)response.StatusCode})");

                }


            }
            catch (Exception ex)
            {
                _logger.LogWarning("Ann error occurs in catch section: "+ex.StackTrace);
               
            }

        }
    }


}
