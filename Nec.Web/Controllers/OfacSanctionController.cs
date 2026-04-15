using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nec.Web.Config;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Services;
using Nec.Web.Utils;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Nec.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OfacSanctionController : ControllerBase
    {
        private readonly IOfacService _ofacService;
        NecAppConfig _appConfig;
        public OfacSanctionController(IOfacService ofacService, NecAppConfig necAppConfig)
        {
            _ofacService = ofacService;
            _appConfig = necAppConfig;
        }

        [HttpPost("upload-sdn")]
        public async Task<IActionResult> UploadSdnXml(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                SdnList sdnList;

                using (var stream = file.OpenReadStream())
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(SdnList));
                    sdnList = (SdnList)serializer.Deserialize(stream);


                       int id = _ofacService.UpdateOfacSanctionSDN(sdnList.SdnEntries);

                    //foreach (var entry in sdnList.SdnEntries)
                    //{
                    //    entry.DataInfoType = "SDN";

                    //    int id = _ofacService.CreateOfacSanction(entry);

                    //    var list = new List<object>();

                    //    if (entry.AkaList is not null && entry.AkaList.Count > 0)
                    //    {
                    //        _ofacService.CreateOfacRefDetails(GenerateQueryAKA(entry, id, entry.DataInfoType));

                    //        foreach(var item in entry.AkaList)
                    //        {
                    //            list.Add(new { FirstName  = item.FirstName?.Replace("'", "''") ?? "", LastName = item.LastName?.Replace("'", "''") ?? "" });
                    //        }
                    //    }
                    //    if (entry.AddressList is not null && entry.AddressList.Count > 0)
                    //    {
                    //        _ofacService.CreateOfacRefDetails(GenerateQueryAddress(entry, id, entry.DataInfoType));
                    //    }

                    //}
                }
                return Ok(new
                {
                    TotalEntries = sdnList.SdnEntries.Count,
                    FirstEntry = new
                    {
                        sdnList.SdnEntries[0].Uid,
                        sdnList.SdnEntries[0].FirstName,
                        sdnList.SdnEntries[0].LastName,
                        sdnList.SdnEntries[0].SdnType
                    }
                });
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Error reading XML: {ex.Message}");
            }
        }
        [HttpPost("upload-non-sdn")]
        public async Task<IActionResult> UploadNonSdnXml(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                SdnList sdnList;

                using (var stream = file.OpenReadStream())
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(SdnList));
                    sdnList = (SdnList)serializer.Deserialize(stream);

                    foreach (var entry in sdnList.SdnEntries)
                    {
                        entry.DataInfoType = "NONSDN";

                        int id = _ofacService.CreateOfacSanction(entry);

                        if (entry.AkaList is not null && entry.AkaList.Count > 0)
                        {
                            _ofacService.CreateOfacRefDetails(GenerateQueryAKA(entry, id, entry.DataInfoType));
                        }
                        if (entry.AddressList is not null && entry.AddressList.Count > 0)
                        {
                            _ofacService.CreateOfacRefDetails(GenerateQueryAddress(entry, id, entry.DataInfoType));
                        }

                    }
                }
                return Ok(new
                {
                    TotalEntries = sdnList.SdnEntries.Count,
                    FirstEntry = new
                    {
                        sdnList.SdnEntries[0].Uid,
                        sdnList.SdnEntries[0].FirstName,
                        sdnList.SdnEntries[0].LastName,
                        sdnList.SdnEntries[0].SdnType
                    }
                });
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Error reading XML: {ex.Message}");
            }
        }

        [HttpPost("Download-sdn")]
        public async Task<IActionResult> DownloadSDN()
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
                    SdnList sdnList;


                    // Get the stream of data
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    {

                        XmlSerializer serializer = new XmlSerializer(typeof(SdnList));
                        sdnList = (SdnList)serializer.Deserialize(contentStream);

                     //   int id = _ofacService.UpdateOfacSanction(sdnList.SdnEntries);


                        // Prepare filename and path
                        string fileName = $"Ofac_SDN_consolidated_data_file_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xml";
                        string savePath = Path.Combine(_appConfig.OfacDownloadFilePath, fileName);

                        // Save the file
                        using (FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await contentStream.CopyToAsync(fileStream);
                        }

                        Console.WriteLine($"File downloaded successfully to {savePath}");
                    }
                }
            }

            return Ok("File downloaded successfully.");
        }

        [HttpPost("Download-non-sdn")]
        public async Task<IActionResult> DownloadNONSDN()
        {
            string url = "https://sanctionslistservice.ofac.treas.gov/api/PublicationPreview/exports/CONSOLIDATED.XML";

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
                    SdnList sdnList;


                    // Get the stream of data
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    {

                        XmlSerializer serializer = new XmlSerializer(typeof(SdnList));
                        sdnList = (SdnList)serializer.Deserialize(contentStream);

                           int id = _ofacService.UpdateOfacSanctionNONSDN(sdnList.SdnEntries);

                        // Prepare filename and path
                        string fileName = $"Ofac_SDN_consolidated_data_file_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xml";
                        string savePath = Path.Combine(_appConfig.OfacDownloadFilePath, fileName);

                        // Save the file
                        using (FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await contentStream.CopyToAsync(fileStream);
                        }

                        Console.WriteLine($"File downloaded successfully to {savePath}");
                    }
                }
            }

            return Ok("File downloaded successfully.");
        }


        [HttpPost]
        [Route("get-amlfilter")]
        public async Task<IActionResult> Getdelta(OfacFilter model)
        {
            var res = await _ofacService.GetSearchSanction(model);
            var result = res.FuzzySearch(model.SearchName, c => c.Name, 100).Where(x => x.Score >= model.Score).Select(c => new
            {
                Id = c.Value?.Id,
                Name = c.Value?.Name.ToString(),
                Address = c.Value?.Address.ToString(),
                Type = c.Value?.Type.ToString(),
                Program = c.Value?.Program.ToString(),
                List = c.Value?.List.ToString(),
                score = c.Score
            }).ToList();
            return Ok(new { Total = result.Count, Result = result });
        }

        //Get details data by id from nec system.
        [HttpGet]
        [Route("get-OfacSourcebyId")]
        public async Task<IActionResult> AmlsourcebyId(int Id)
        {
            var res = await _ofacService.GetSanctionDetailsById(Id);
            return Ok(new { Result = res });
        }


        private string GenerateQueryAKA(SdnEntry sdnEntries, int Refid,string sdntype)
        {

            var sb = new StringBuilder();
            foreach (var res in sdnEntries.AkaList)
            {
                string type = res.Type?.Replace("'", "''") ?? "";
                string category = res.Category?.Replace("'", "''") ?? "";
                string firstName = res.FirstName?.Replace("'", "''") ?? "";
                string lastName = res.LastName?.Replace("'", "''") ?? "";
                sb.AppendFormat(
                    "INSERT INTO [dbo].[AkaInfo]([Uid],[Type],[Category],[FirstName],[LastName],[OfacId],[DataInfo],[CreatedDate]) " +
                    "VALUES ({0},'{1}','{2}','{3}','{4}',{5},'{6}','{7:yyyy-MM-dd HH:mm:ss}');",
                    res.Uid, type, category, firstName, lastName, Refid, sdntype, DateTime.Now
                );
            }
            return sb.ToString();
        }
        private string GenerateQueryAddress(SdnEntry sdnEntriesint, int Refid, string sdntype)
        {
            var sb = new StringBuilder();
            foreach (var res in sdnEntriesint.AddressList)
            {
                string address1 = res.Address1?.Replace("'", "''") ?? "";
                string city = res.City?.Replace("'", "''") ?? "";
                string postalCode = res.PostalCode?.Replace("'", "''") ?? "";
                string country = res.Country?.Replace("'", "''") ?? "";
                sb.AppendFormat(
                    "INSERT INTO [dbo].[AddressInfo] ([Uid],[Address1],[City],[PostalCode],[Country],[DataInfo],[OfacId],[CreatedDate]) " +
                    "VALUES ({0},'{1}','{2}','{3}','{4}','{5}',{6},'{7:yyyy-MM-dd HH:mm:ss}');",
                    res.Uid, address1, city, postalCode, country,sdntype, Refid, DateTime.Now
                );
            }
            return sb.ToString();
        }

    }
}
