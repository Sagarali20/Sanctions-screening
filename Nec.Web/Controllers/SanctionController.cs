using DocumentFormat.OpenXml.Office2013.Word;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using ICSharpCode.SharpZipLib.Core;
using MathNet.Numerics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using MigraDocCore.Rendering;
using Nec.Web.Config;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Models.Model;
using Nec.Web.Services;
using Nec.Web.Utils;
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;
using PdfSharpCore.Pdf;
using System.Reflection;
using System.Text.Json;
using System.Xml.Serialization;
using Colors = MigraDocCore.DocumentObjectModel.Colors;
using Document = MigraDocCore.DocumentObjectModel.Document;



namespace Nec.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SanctionController : ControllerBase
    {
        private readonly ISanctionService _sanctionService;
        private readonly ICommonService _CommonService;
        private readonly IOfacService _OfacService;
        private readonly IUKService _UKService;
        private readonly IUNService _UNService;
        NecAppConfig _appConfig;
        private readonly ILogger<SanctionController> _logger;


        public SanctionController(ISanctionService sanctionService, NecAppConfig necAppConfig
            ,ICommonService CommonService,IOfacService OfacService, IUKService uKService,IUNService uNService, ILogger<SanctionController> logger
            )
        {
            _sanctionService=sanctionService;
            _appConfig=necAppConfig;
            _CommonService = CommonService;
            _OfacService = OfacService;
            _UKService = uKService;
            _UNService = uNService;
            _logger = logger;

        }

        // Download all data from dilisense for intial stored.
        [HttpGet]
        [Route("getDownload")]
        public async Task<IActionResult> getDownload()
        {
            string url = "https://api.dilisense.com/v1/getConsolidatedFile";
            // string outputFilePath = @"E:\Development\Dilisense\consolidated_file.json"; // Save path

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("x-api-key", _appConfig.APIKey);

                    Console.WriteLine("Downloading file, please wait...");

                    using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        string Version = string.Empty;

                        // Print response headers
                        Console.WriteLine("Response Headers:");

                        foreach (var header in response.Headers)
                        {
                            Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");

                            if (header.Key == "File-Version")
                            {
                                Version = string.Join(", ", header.Value);
                            }
                        }

                        string responseBody = await response.Content.ReadAsStringAsync();
                        string[] jsonArray = responseBody.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        AMLSourceLog aMLSourceLog = new AMLSourceLog();
                        aMLSourceLog.Total = jsonArray.Length;
                        aMLSourceLog.FileVersion = Version;
                        aMLSourceLog.FileName = $"Dilisense_consolidated_data_file_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                        aMLSourceLog.SourceName = "Dilisense";
                        aMLSourceLog.SourceLink = url;
                        aMLSourceLog.SourceCountry = "Zurich and Luxembourg";

                        _sanctionService.CreateAMLLog(aMLSourceLog);

                        // Save file stream
                        using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                                     fileStream = new FileStream(_appConfig.DownloadFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            await contentStream.CopyToAsync(fileStream);

                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }

            return Ok("okkk");
        }

        // Save all dilisense data to nec system.
        [HttpPost]
        [Route("save-dilisense")]
        public async Task<IActionResult> Save()
        {

            //string filePath = @"E:\Development\Dilisense\consolidated_file.json";
            string? filePath = _appConfig.DownloadFilePath;
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($" File not found: {filePath}");
            }

            //Console.WriteLine($" Reading file: {filePath}");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            List<SanctionEntity> entities = new();

            try
            {
                string[] lines = await System.IO.File.ReadAllLinesAsync(filePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    SanctionEntity entity = System.Text.Json.JsonSerializer.Deserialize<SanctionEntity>(line)!;
                    entities.Add(entity);
                    _sanctionService.CreateSanction(entity);
                }

            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing file: {ex.Message}");
            }

            if (entities.Count == 0)
            {
                return BadRequest("No records found in the file.");
            }
            Console.WriteLine($" Successfully processed {entities.Count} records.");

            return Ok(new
            {
                Message = "File processed successfully.",
                TotalRecords = entities.Count,
            });
        }

        // Download againg from dilisense for updated database by version id.
        [HttpGet]
        //[ApiKeyAuthorize]
        [Route("get-consolidatedDelta")]
        public async Task<IActionResult> GetConsolidatedDelta()
        {

            List<SanctionEntity> entities = new();

            try
            {
                using var client = new HttpClient() { 
                
                    Timeout = TimeSpan.FromMinutes(30) 
                };

                string? FileVersion = await _sanctionService.GetFileVersion();


                var request = new HttpRequestMessage(HttpMethod.Get, _appConfig.DilisenseUrl+FileVersion);
                request.Headers.Add("x-api-key", _appConfig.APIKey);
                var response = await client.SendAsync(request);
                string Version = string.Empty;

                if (response.IsSuccessStatusCode)
                {


                    foreach (var header in response.Headers)
                    {
                        if (header.Key == "File-Version")
                        {
                            Version = string.Join(", ", header.Value);
                        }
                    }

                    string responseBody = await response.Content.ReadAsStringAsync();
                    string[] jsonArray = responseBody.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    int TotalPrivious=0, TotalNew = 0,TotalUpdate=0,TotalDelete=0;

                   // var addcount= jsonArray.Select(x => x.).ToString();

                    AMLSourceLog aMLSourceLog = new AMLSourceLog();
                    aMLSourceLog.Total = jsonArray.Count();
                    aMLSourceLog.FileVersion = Version;
                    aMLSourceLog.FileName = $"Dilisense_consolidated_data_file_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                    aMLSourceLog.SourceName = "Dilisense";
                    aMLSourceLog.SourceLink = _appConfig.DilisenseUrl + FileVersion;
                    aMLSourceLog.SourceCountry = "Zurich and Luxembourg";

                    int RowId = _sanctionService.CreateAMLLog(aMLSourceLog);
                    int Totaldownload = 0;
                    // string[] lines = await System.IO.File.ReadAllLinesAsync(filePath);
                    foreach (var line in jsonArray)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        Totaldownload++;
                        ConsolidatedDelta entity = JsonSerializer.Deserialize<ConsolidatedDelta>(line)!;
                        if(entity.type== "UPDATE")
                        {
                            TotalUpdate++;
                            bool Res = _sanctionService.UpdateSanction(entity.record);
                        }
                        else if(entity.type== "ADD")
                        {
                            TotalNew++; 
                            entity.record.VersionId = RowId;
                            bool Res = _sanctionService.CreateSanctionNew(entity.record);
                        }
                        else if(entity.type== "DELETE")
                        {
                            TotalDelete++;  
                            bool Res = _sanctionService.DeleteSanction(entity.record.id);
                        }

                    }

                    aMLSourceLog.TotalNew = TotalNew;
                    aMLSourceLog.TotalUpdate = TotalUpdate;
                    aMLSourceLog.TotalDelete = TotalDelete;
                    aMLSourceLog.TotalData = Totaldownload;
                    aMLSourceLog.TotalPrivious = await _sanctionService.TotalDataCount();

                    var res = _sanctionService.CreateAMLDataStatusLog(aMLSourceLog);
                }
                else
                {
                    if((int)response.StatusCode == 400)
                    {
                        string url = "https://api.dilisense.com/v1/getConsolidatedFile";
                        // string outputFilePath = @"E:\Development\Dilisense\consolidated_file.json"; // Save path

                        using (HttpClient clientNew = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("x-api-key", _appConfig.APIKey);

                            Console.WriteLine("Downloading file, please wait...");

                            using (HttpResponseMessage responsenew = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                            {
                                responsenew.EnsureSuccessStatusCode();

                                // Print response headers
                                Console.WriteLine("Response Headers:");
                                foreach (var header in responsenew.Headers)
                                {
                                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");

                                    if (header.Key == "File-Version")
                                    {
                                        Version = string.Join(", ", header.Value);
                                    }
                                }


                                AMLSourceLog aMLSourceLog = new AMLSourceLog();
                                aMLSourceLog.Total = 0;
                                aMLSourceLog.FileVersion = Version;
                                aMLSourceLog.SourceCountry = "Zurich and Luxembourg";

                                aMLSourceLog.FileName = "using for file version";

                                aMLSourceLog.SourceName = "Dilisense";
                                aMLSourceLog.SourceLink = _appConfig.DilisenseUrl + FileVersion;

                                _sanctionService.CreateAMLLog(aMLSourceLog);

                                //// Save file stream
                                //using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                                //             fileStream = new FileStream(_appConfig.DownloadFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                                //{
                                //    await contentStream.CopyToAsync(fileStream);

                                //}
                                GetConsolidatedDelta();

                            }
                        }
                    }

                    Console.WriteLine($"Request failed with status code: {response.StatusCode} ({(int)response.StatusCode})");

                }


            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing file: {ex.Message}");
            }

 

            return Ok(new
            {
                Message = "File processed successfully.",
                TotalRecords = entities.Count,
            });
        }

        // Download sourcelist and insert to database from dilisense .
        [HttpGet]
        [ApiKeyAuthorize]
        [Route("get-sourcelist")]
        public async Task<IActionResult> SourceList()
        {

            List<SanctionEntity> entities = new();

            try
            {
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(30)
                };

                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.dilisense.com/v1/listSources");
                request.Headers.Add("x-api-key", _appConfig.APIKey);
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {

                    string Version = string.Empty;
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Root root = JsonSerializer.Deserialize<Root>(responseBody);

                    List<Source> sourceList = root.sources;
                    foreach (var model in sourceList)
                    {
                       // if (string.IsNullOrWhiteSpace(line)) continue;
                        bool Res = _sanctionService.CreateSource(model);
                    }
                }
                else
                {
                    Console.WriteLine($"Request failed with status code: {response.StatusCode} ({(int)response.StatusCode})");

                }


            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing file: {ex.Message}");
            }

            return Ok(new
            {
                Message = "File processed successfully.",
                TotalRecords = entities.Count,
            });
        }

        // Search data by requird parameter from nec system.
        [HttpPost]
        [ApiKeyAuthorize]
        [Route("get-amlfilter")]
        public async Task<IActionResult> Getdelta(AMLFilter aMLFilter)
        {

            // List<SearchResult?> res = new List<SearchResult?>();
            //var  res = await _sanctionService.GetSearchSanction(aMLFilter);

            //var result = res.FuzzySearch(aMLFilter.Name, c => c.Name, 100).Where(x => x.Score >= aMLFilter.TopMatch).Select(c => new
            //{
            //    Id=c.Value?.Id,
            //    Name=c.Value?.Name.ToString(),
            //    Address=c.Value?.Address.ToString(),
            //    EntityType = c.Value?.EntityType.ToString(),
            //    SourceType = c.Value?.SourceType.ToString(),
            //    SourceId = c.Value?.SourceId.ToString(),
            //    score= c.Score
            //}).ToList();

            //var results = AMLFilterFuzzyMatcher.SearchByName(res, aMLFilter.Name);

            // List<SearchResult?> res = new List<SearchResult?>();

            var results = await _sanctionService.GetSearchSanctionIndividual(aMLFilter);

            var result = results
                .Where(x => x.Score >= aMLFilter.TopMatch)
                .Select(c => new
                {
                    Id = c.id,
                    Name = c.name,
                    Aliasnames = c.alias_names != null ? string.Join(", ", c.alias_names) : string.Empty,
                    Address = c.address,
                    EntityType = c.entity_type,
                    SourceType = c.source_type,
                    SourceId = c.source_id,
                    score = c.Score
                })
                .ToList();

            return Ok(new {Total= result.Count, Result= result });
        }

        // Search data by requird parameter from nec system.
        [HttpPost]
        [ApiKeyAuthorize]
        [Route("get-amlfilter-status")]
        public async Task<IActionResult> GetAmlfilterStatus(AMLFilter aMLFilter)
        {
            var results = await _sanctionService.GetSearchSanctionIndividual(aMLFilter);


            var sanctionList = results
                .Where(x => string.Equals(x.source_type, "SANCTION", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var pepList = results
                .Where(x => string.Equals(x.source_type, "PEP", StringComparison.OrdinalIgnoreCase))
                .ToList();

            string GetStatus(int score) =>
                score == 100 ? "Positive"
                : score >= 86 ? "False Positive"
                : "False Negative";

            int sanctionScore = sanctionList.Any()
                ? sanctionList.Max(c => c.Score ?? 0)
                : 0;

            int pepScore = pepList.Any()
                ? pepList.Max(c => c.Score ?? 0)
                : 0;

            string sanctionStatus = GetStatus(sanctionScore);
            string pepStatus = GetStatus(pepScore);

            var result = new
            {
                Sanction = new
                {
                    Count = sanctionList.Count,
                    MaxScore = sanctionScore,
                    Status = sanctionStatus
                },
                PEP = new
                {
                    Count = pepList.Count,
                    MaxScore = pepScore,
                    Status = pepStatus
                }
            };

            return Ok(new { Result = result });
        }

        // Search data by requird parameter from nec system.
        [HttpPost]
        [ApiKeyAuthorize]
        [Route("get-amlfilter-pdf-download")]
        public async Task<IActionResult> GetdeltaPdf(AMLFilter aMLFilter)
        {

            var res = await _sanctionService.GetSearchSanction(aMLFilter);

            var result = res.FuzzySearch(aMLFilter.Name, c => c.Name).Where(x => x.Score >= aMLFilter.TopMatch).Select(c => new
            {
                Id = c.Value?.Id,
                Name = c.Value?.Name.ToString(),
                Address = c.Value?.Address.ToString(),
                EntityType = c.Value?.EntityType.ToString(),
                SourceType = c.Value?.SourceType.ToString(),
                SourceId = c.Value?.SourceId.ToString(),
                score = c.Score
            }).ToList();

            try
            {
                var doc = new MigraDocCore.DocumentObjectModel.Document();
                var section = doc.AddSection();
                var title = section.AddParagraph("Sanction Bulk Result");
                title.Format.Font.Size = 16;
                title.Format.Font.Bold = true;
                title.Format.SpaceAfter = "1cm";
                title.Format.Alignment = ParagraphAlignment.Center;

                var table = section.AddTable();
                table.Borders.Width = 0.5;

                string[] headers =
                                {
                                    "Name", "Address", "EntityType", "SourceType", "SourceId", "score"
                                };

                foreach (var _ in headers)
                {
                    table.AddColumn(Unit.FromCentimeter(3)); 
                }

                var headerRow = table.AddRow();
                headerRow.Shading.Color = Colors.Gray;
                headerRow.Format.Font.Bold = true;

                for (int i = 0; i < headers.Length; i++)
                {
                    headerRow.Cells[i].AddParagraph(headers[i]);
                }

                foreach (var p in result)
                {
                    var row = table.AddRow();
                    row.Cells[0].AddParagraph(p.Name).Format.Alignment = ParagraphAlignment.Left;
                    row.Cells[1].AddParagraph(p.Address).Format.Alignment = ParagraphAlignment.Left;
                    row.Cells[2].AddParagraph(p.EntityType).Format.Alignment = ParagraphAlignment.Left;
                    row.Cells[3].AddParagraph(p.SourceType).Format.Alignment = ParagraphAlignment.Left;
                    row.Cells[4].AddParagraph(p.SourceId).Format.Alignment = ParagraphAlignment.Left;
                    row.Cells[5].AddParagraph(p.score.ToString()).Format.Alignment = ParagraphAlignment.Right;
                }

                var pdfRenderer = new PdfDocumentRenderer(true)
                {
                    Document = doc
                };

                pdfRenderer.RenderDocument();

                using var stream = new MemoryStream();
                pdfRenderer.PdfDocument.Save(stream, false);
                var bytes = stream.ToArray();

                var fileName = $"sanction_bulk_result_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

                return File(bytes, "application/pdf", fileName);


            }
            catch (Exception ex)
            {
                throw;
            }

        }
        // Search data by requird parameter from nec system.
        [HttpPost]
        [ApiKeyAuthorize]
        [Route("get-amlfilter-excel-download")]
        public async Task<IActionResult> GetdeltaExcel(AMLFilter aMLFilter)
        {


            var res = await _sanctionService.GetSearchSanction(aMLFilter);


            var result = res.FuzzySearch(aMLFilter.Name, c => c.Name).Where(x => x.Score >= aMLFilter.TopMatch).Select(c => new
            {
                Id = c.Value?.Id,
                Name = c.Value?.Name.ToString(),
                Address = c.Value?.Address.ToString(),
                EntityType = c.Value?.EntityType.ToString(),
                SourceType = c.Value?.SourceType.ToString(),
                SourceId = c.Value?.SourceId.ToString(),
                score = c.Score
            }).ToList();

            try
            {
                var wb = new HSSFWorkbook();
                var sheet2 = wb.CreateSheet("SanctionBulkResult");

                // --- Header row ------------------------------------------------------
                string[] headers =
                [
                    "Name", "Address", "EntityType","SourceType", "SourceId", "Score"
                ];

                var headerRow2 = sheet2.CreateRow(0);
                for (int c = 0; c < headers.Length; c++)
                {
                    headerRow2.CreateCell(c).SetCellValue(headers[c]);
                    sheet2.AutoSizeColumn(c);
                }

                var dateStyle = wb.CreateCellStyle();
                var format = wb.CreateDataFormat();
                dateStyle.DataFormat = format.GetFormat("yyyy-mm-dd");

                int rowIndex = 1;
                foreach (var p in result)
                {
                    var row = sheet2.CreateRow(rowIndex++);

                    row.CreateCell(0).SetCellValue(p.Name);
                    row.CreateCell(1).SetCellValue(p.Address);
                    row.CreateCell(2).SetCellValue(p.EntityType);
                    row.CreateCell(3).SetCellValue(p.SourceType);
                    row.CreateCell(4).SetCellValue(p.SourceId);
                    row.CreateCell(5).SetCellValue(p.score);

                }
                using var ms2 = new MemoryStream();
                wb.Write(ms2, true);

                var fileName = $"sanction_bulk_result_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xls";
                var content = ms2.ToArray();
                const string mime = "application/vnd.ms-excel";
                return File(content, mime, fileName);



            }
            catch (Exception ex)
            {
                throw ex;
            }


        }

        // Search Individual data by requird parameter from nec system.
        [HttpGet]
        [ApiKeyAuthorize]
        [Route("checkIndividual")]
        public async Task<IActionResult> CheckIndividual(string names,int? fuzzy_search,string? dob,string? gender,string? includes,int? screeningid, string? screeningtype)
        {


            AMLFilter aMLFilter = new AMLFilter();
            aMLFilter.Name = names;
            aMLFilter.DateOfBirth = dob;
            aMLFilter.Gender = gender;
            aMLFilter.SourceId = includes;
            aMLFilter.ScreeningId = screeningid;
            aMLFilter.ScreeningType = screeningtype;

            if (fuzzy_search is not null && fuzzy_search !=0)
            {
                aMLFilter.IsFuzzy = true;
            }

            var results = await _sanctionService.GetSearchSanctionIndividual(aMLFilter);

            //var res = Levenshtein.FindClosestPersons(results, aMLFilter.Name,1);

            //int gg = Levenshtein.Ltest();
            //if (aMLFilter.IsFuzzy)
            //{
            //    //var results1 = Levenshtein.FindClosestPersons(results, aMLFilter.Name, maxDistance:2);
            //    //var results2 = FuzzySearch.Search(results, names, 1);
            //}


            return Ok(new { timestamp= DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), Total_hits = results.Count, Found_records = results.OrderByDescending(a => a.Score) });
        }

        [Authorize]
        [HttpPost]
        [ApiKeyAuthorize]
        [Route("CheckIndividualForUI")]
        public async Task<IActionResult> CheckIndividualForUI(SanctionUIRequest model)
        {
            AMLFilter aMLFilter = new AMLFilter();
            aMLFilter.Name = model.Payload?.Name;
            aMLFilter.Type = model.Payload?.Type;
            aMLFilter.SourceType = model.Payload?.SourceType;
            aMLFilter.City = model.Payload?.City;
            aMLFilter.StateProvince = model.Payload?.StateProvince;
            aMLFilter.Nationality = model.Payload?.Nationality;
            aMLFilter.Country = model.Payload?.Country;
            aMLFilter.DateOfBirth = model.Payload?.DateOfBirth;
            aMLFilter.MatchParcentage = model.Payload?.MatchParcentage;
            aMLFilter.Includes = model.Payload?.Includes;
            aMLFilter.IpAddress = model.Payload?.IpAddress;
            aMLFilter.UserId = model.Header?.UserModifidId;


            // List<SearchResult?> res = new List<SearchResult?>();

            var results = await _sanctionService.GetSearchSanctionIndividualForUI(aMLFilter);

            //var res = Levenshtein.FindClosestPersons(results, aMLFilter.Name,1);

            //int gg = Levenshtein.Ltest();

            //if (aMLFilter.IsFuzzy)
            //{
            //    //var results1 = Levenshtein.FindClosestPersons(results, aMLFilter.Name, maxDistance:2);
            //   var results2 = FuzzySearch.Search(results, names, 1);
            //}



            //var ff = results.OrderByDescending(a => a.Score);
            //var selected = results
            //    .Select((x, index) => new
            //    {
            //        x.entity_type,
            //        x.name,
            //        x.gender,
            //        x.source_type,
            //        x.list_date,
            //        x.date_of_birth,
            //        x.alias_names,
            //        x.last_names,
            //        x.given_names,
            //        x.name_remarks,
            //        x.spouse,
            //        x.parents,
            //        x.children,
            //        x.siblings,
            //        x.citizenship,
            //        x.date_of_birth_remarks,
            //        x.place_of_birth,
            //        x.place_of_birth_remarks,
            //        x.address,
            //        x.address_remarks,
            //        x.citizenship_remarks,
            //        x.pep_type,
            //        x.sanction_details,
            //        x.description,
            //        x.occupations,
            //        x.positions,
            //        x.political_parties,
            //        x.links,
            //        x.titles,
            //        x.functions,
            //        x.other_information,
            //        x.source_id,
            //        x.Score,
            //    })
            //    .ToList().OrderByDescending(a => a.Score);


            return Ok(new { timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),City= model.Payload?.City,StateProvince = model.Payload?.StateProvince,Country=model.Payload?.Country, DateOfBirth= model.Payload?.DateOfBirth,Name = model.Payload?.Name, Total_hits = results.Count, Max_match_parcentage= results.OrderByDescending(x => x.Score)
                    .Select(x => x.Score)
                    .FirstOrDefault(), Found_records = results.OrderByDescending(a => a.Score) });
        }


        [HttpPost]
        [ApiKeyAuthorize]
        [Route("Downloadfile")]
        public async Task<IActionResult> Downloadfile(DownloadModel model)
        {

            if(string.IsNullOrWhiteSpace(model.Header.ActionName))
            {
                return BadRequest("Missing Action type in header ex: pdf or excel !!");
            }
            if(model.Header.ActionName.ToUpper()== "EXPORT_RESULT_PDF")
            {
                try
                {
                    var doc = new Document();

                    // ===== Font (Arabic/Bangla safe) =====
                    doc.Styles["Normal"].Font.Name = "Arial";
                    doc.Styles["Normal"].Font.Size = 9;

                    var section = doc.AddSection();
                    section.PageSetup.PageFormat = PageFormat.A4;
                    section.PageSetup.Orientation = Orientation.Landscape;

                    // ================= HEADER =================
                    var headerTable = section.AddTable();
                    headerTable.Borders.Visible = false;

                    headerTable.AddColumn(Unit.FromCentimeter(12));
                    headerTable.AddColumn(Unit.FromCentimeter(8));

                    var headerRow = headerTable.AddRow();

                    // Left title
                    var title = headerRow.Cells[0].AddParagraph("Sanction Result");
                    title.Format.Font.Size = 14;
                    title.Format.Font.Bold = true;

                    // Right info
                    //var right = headerRow.Cells[1];
                    //headerRow.Cells[1].Format.Alignment = ParagraphAlignment.Right;

                    //var company = right.AddParagraph("NEC MONEY (PTY.)");
                    //company.Format.Font.Bold = true;
                    //company.Format.Font.Size = 12;
                    //company.Format.Alignment = ParagraphAlignment.Right;
                    //company.Format.Font.Color = Colors.Green;

                    //// ⭐ THIS LINE DOES THE MAGIC
                    //company.Format.SpaceBefore = "1cm";

                    //right.AddParagraph(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    //     .Format.Alignment = ParagraphAlignment.Right;

                    //right.AddParagraph($"Max Match (%): 100")
                    //     .Format.Alignment = ParagraphAlignment.Right;

                    //right.AddParagraph($"Total Record: {list.Count()}")
                    //     .Format.Alignment = ParagraphAlignment.Right;

                    //section.AddParagraph("\n");

                    // ================= SEARCH INFO =================
                    section.AddParagraph("Search").Format.Font.Bold = true;

                    var searchTable = section.AddTable();
                    searchTable.Borders.Visible = false;

                    searchTable.AddColumn(Unit.FromCentimeter(4));
                    searchTable.AddColumn(Unit.FromCentimeter(0.5));
                    searchTable.AddColumn(Unit.FromCentimeter(10));

                    void AddSearchRow(string label, string value)
                    {
                        var r = searchTable.AddRow();
                        r.Cells[0].AddParagraph(label);
                        r.Cells[1].AddParagraph(":");
                        r.Cells[2].AddParagraph(value ?? "");
                    }

                    AddSearchRow("Name", model.Payload.Name);
                    AddSearchRow("Dob", model.Payload.Dob);
                    //AddSearchRow("City", model.Payload.City);
                    //AddSearchRow("State/Prov", model.Payload.State);
                    AddSearchRow("Country", model.Payload.Country);

                    section.AddParagraph("\n");

                    // ================= RESULT TABLE =================
                    var table = section.AddTable();
                    table.Borders.Width = 0.75;
                    table.TopPadding = 5;
                    table.BottomPadding = 5;

                    double[] widths = { 5, 5, 4, 4, 5, 2 };
                    foreach (var w in widths)
                        table.AddColumn(Unit.FromCentimeter(w));

                    // Header row
                    var header = table.AddRow();
                    header.HeadingFormat = true;
                    header.Shading.Color = Colors.LightBlue;
                    header.Format.Font.Bold = true;
                    header.HeightRule = RowHeightRule.AtLeast;

                    string[] headers =
                    {
                        "Source", "Name", "Date of Birth",
                        "Nationality", "Address", "Percentage"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var p = header.Cells[i].AddParagraph(headers[i]);
                        p.Format.Alignment = ParagraphAlignment.Center;
                    }

                    // Data rows
                    foreach (var p in model.Payload.Found_Records)
                    {
                        var row = table.AddRow();
                        row.HeightRule = RowHeightRule.AtLeast;

                        row.Cells[0].AddParagraph(p.Source_Id==null?"": p.Source_Id);
                        row.Cells[1].AddParagraph(p.Name);
                        row.Cells[2].AddParagraph(string.Join(", ", p.Date_Of_Birth?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                        row.Cells[3].AddParagraph("");
                        row.Cells[4].AddParagraph(string.Join(", ", p.Address?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                        row.Cells[5].AddParagraph(p.Score.ToString())
                                      .Format.Alignment = ParagraphAlignment.Right;
                    }

                    // ================= RENDER PDF =================
                    var renderer = new PdfDocumentRenderer(true)
                    {
                        Document = doc
                    };

                    renderer.RenderDocument();

                    byte[] pdfBytes;
                    using (var stream = new MemoryStream())
                    {
                        renderer.PdfDocument.Save(stream, false);
                        pdfBytes = stream.ToArray();
                    }

                    // ================= BASE64 =================
                    string base64Pdf = Convert.ToBase64String(pdfBytes);

                    // ================= RETURN JSON =================
                    return Ok(new
                    {
                        header = new
                        {
                            userId = (string)null,
                            apiKey = "dd326dc19526f75e5b6b777d9e281043",
                            actionName = "EXPORT_RESULT_PDF",
                            serviceName = "SanctionService"
                        },
                        payload = new
                        {
                            base64file = base64Pdf
                        }
                    });

                }
                catch (Exception ex)
                {
                    throw;
                }

            }
            else
            {
                try
                {
                    // ================= CREATE EXCEL =================
                    using var workbook = new ClosedXML.Excel.XLWorkbook();
                    var ws = workbook.Worksheets.Add("Sanction Result");

                    int row = 1;

                    // ================= TITLE =================
                    ws.Cell(row, 1).Value = "Sanction Result";
                    ws.Range(row, 1, row, 6).Merge();
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 1).Style.Font.FontSize = 14;
                    row += 2;

                    // ================= SEARCH INFO =================
                    ws.Cell(row, 1).Value = "Search";
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    row++;

                    void AddSearchRow(string label, string value)
                    {
                        ws.Cell(row, 1).Value = label;
                        ws.Cell(row, 2).Value = ":";
                        ws.Cell(row, 3).Value = value ?? "";
                        row++;
                    }

                    AddSearchRow("Name", model.Payload.Name);
                    AddSearchRow("Dob", model.Payload.Dob);
                    //AddSearchRow("City", model.Payload.City);
                    //AddSearchRow("State/Prov", model.Payload.State);
                    AddSearchRow("Country", model.Payload.Country);
                    row++;

                    // ================= TABLE HEADER =================
                    string[] headers =
                    {
                        "Source", "Name", "Date of Birth",
                        "Nationality", "Address", "Percentage"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cell(row, i + 1).Value = headers[i];
                        ws.Cell(row, i + 1).Style.Font.Bold = true;
                        ws.Cell(row, i + 1).Style.Fill.BackgroundColor =
                            ClosedXML.Excel.XLColor.LightBlue;
                        ws.Cell(row, i + 1).Style.Alignment.Horizontal =
                            ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, i + 1).Style.Border.OutsideBorder =
                            ClosedXML.Excel.XLBorderStyleValues.Thin;
                    }

                    row++;

                    // ================= DATA ROWS =================
                    foreach (var p in model.Payload.Found_Records)
                    {
                        ws.Cell(row, 1).Value = p.Source_Id;
                        ws.Cell(row, 2).Value = p.Name;
                        ws.Cell(row, 3).Value = string.Join(", ",
                            p.Date_Of_Birth?.Where(v => !string.IsNullOrWhiteSpace(v))
                            ?? Enumerable.Empty<string>());
                        ws.Cell(row, 4).Value = "";
                        ws.Cell(row, 5).Value = string.Join(", ", p.Address?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>());
                        ws.Cell(row, 6).Value = p.Score;

                        ws.Range(row, 1, row, 6)
                          .Style.Border.OutsideBorder =
                            ClosedXML.Excel.XLBorderStyleValues.Thin;

                        row++;
                    }

                    ws.Columns().AdjustToContents();

                    // ================= CONVERT TO BASE64 =================
                    byte[] excelBytes;
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        excelBytes = stream.ToArray();
                    }

                    string base64Excel = Convert.ToBase64String(excelBytes);

                    // ================= RETURN JSON =================
                    return Ok(new
                    {
                        header = new
                        {
                            userId = (string)null,
                            apiKey = "dd326dc19526f75e5b6b777d9e281043",
                            actionName = "EXPORT_RESULT_EXCEL",
                            serviceName = "SanctionService"
                        },
                        payload = new
                        {
                            base64file = base64Excel
                        }
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, ex.Message);
                }

            }


            // List<SearchResult?> res = new List<SearchResult?>();
            //var results = await _sanctionService.GetSearchingResultDownload(model.Ids);

        }
        // Search Individual data by requird parameter from nec system.
        [HttpGet]
        [ApiKeyAuthorize]
        [Route("get-checkEntity")]
        public async Task<IActionResult> CheckcheckEntity(string name, string? includes)
        {

            AMLFilter aMLFilter = new AMLFilter();
            aMLFilter.Name = name;

            aMLFilter.SourceId = includes;

            // List<SearchResult?> res = new List<SearchResult?>();
            var res = await _sanctionService.GetSearchSanctionCheckEntity(aMLFilter);

            var selected = res.Select((x, index) => new
                                {
    
                                    x.entity_type,
                                    x.name,
                                    x.source_type,
                                    x.list_date,
                                    x.source_id,
                                    x.sanction_details             
                                })
                                .ToList();

            return Ok(new { timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), Total_hits = res.Count, Found_records = selected });
        }

        //Get details data by id from nec system.
        [HttpGet]
        [ApiKeyAuthorize]
        [Route("get-amlsourcebyId")]
        public async Task<IActionResult> AmlsourcebyId(int Id)
        {
            var res = await _sanctionService.GetSanctionDetailsById(Id);
            return Ok(new { Result = res });
        }

        // Get all source type optional data from nec system.
        [HttpGet]
        [ApiKeyAuthorize]
        [Route("get-allsourceId")]
        public async Task<IActionResult> GetAllSourceId()
        {
            var res = await _sanctionService.GetAllSourceId();

            string[] words = res.Split('|',StringSplitOptions.RemoveEmptyEntries);

            return Ok(new { Result = words,total= words.Length });
        }

        // Get all search data by excel data and download excel format from nec system.
        [HttpPost("upload-and-download-excel")]
        public async Task<IActionResult> UploadXls([FromForm] IFormFile file)                                                                                                 
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");
            if (!Path.GetExtension(file.FileName).Equals(".xls", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only .xls files are supported.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            var workbook = new HSSFWorkbook(ms);
            var sheet = workbook.GetSheetAt(0);

            var result = new List<PersonRow>();

            var headerRow = sheet.GetRow(0);
            if (headerRow == null)
                return BadRequest("Header row missing.");

            for (int r = 1; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null || Excel.IsRowCompletelyEmpty(row)) continue;

                var model = new PersonRow
                {
                    Guid = Excel.GetCell(row, 0),
                    RecordType = Excel.GetCell(row, 1),
                    FullName = Excel.GetCell(row, 2),
                    AddressLine1 = Excel.GetCell(row, 3),
                    AddressLine2 = Excel.GetCell(row, 4),
                    AddressLine3 = Excel.GetCell(row, 5),
                    City = Excel.GetCell(row, 6),
                    State = Excel.GetCell(row, 7),
                    Country = Excel.GetCell(row, 8),
                    PostCode = Excel.GetCell(row, 9),
                    DateOfBirth = Excel.GetCell(row, 10)
                };

                result.Add(model);
            }

            List<SanctionEntity> results = new List<SanctionEntity>();

            foreach (var row in result)
            {
                var lst= await _sanctionService.GetExcelSanctionDetailsBySearch(row.FullName,row.RecordType,row.AddressLine1,row.City,row.State,row.Country,row.DateOfBirth,row.Guid);

                if(lst != null)
                {
                    results.AddRange(lst);
                }
            }
            var people = results;

            var wb = new HSSFWorkbook();
            var sheet2 = wb.CreateSheet("SanctionBulkResult");

            // --- Header row ------------------------------------------------------
            string[] headers =
            [
             "GUID", "EntityType", "Name",
            "Gender", "SourceType", "PepType",
            "TlName", "Alias_names", "GivenNames", "AliasGivenNames","Spouse", "DateofBirth","Parents","Children","Siblings","DateOfBirthRemarks","PlaceOfBirth","PlaceOfBirthRemarks","Address",
            "AddressRemarks","SanctionDetails","Description","Occupations","Positions","PoliticalParties","Links","Titles","Functions","Citizenship",
            "CitizenshipRemarks","OtherInformation","SourceCountry"
            ];

            var headerRow2 = sheet2.CreateRow(0);
            for (int c = 0; c < headers.Length; c++)
            {
                headerRow2.CreateCell(c).SetCellValue(headers[c]);
                sheet2.AutoSizeColumn(c);
            }

            var dateStyle = wb.CreateCellStyle();
            var format = wb.CreateDataFormat();
            dateStyle.DataFormat = format.GetFormat("yyyy-mm-dd");

            int rowIndex = 1;
            foreach (var p in people)
            {
                var row = sheet2.CreateRow(rowIndex++);

                row.CreateCell(0).SetCellValue(p.Guid);
                row.CreateCell(1).SetCellValue(p.entity_type);
                row.CreateCell(2).SetCellValue(p.name);
                row.CreateCell(3).SetCellValue(p.gender);
                row.CreateCell(4).SetCellValue(p.source_type);
                row.CreateCell(5).SetCellValue(p.pep_type);
                row.CreateCell(6).SetCellValue(p.tl_name);
                row.CreateCell(7).SetCellValue(string.Join(", ", p.alias_names?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(8).SetCellValue(string.Join(", ", p.given_names?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(9).SetCellValue(string.Join(", ", p.alias_given_names?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(10).SetCellValue(string.Join(", ", p.spouse?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));

                var dobCell = row.CreateCell(11);
                var dobText = string.Join(", ", p.date_of_birth?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>());
                    dobCell.SetCellValue(dobText ?? ""); // fallback if null
                row.CreateCell(12).SetCellValue(string.Join(", ", p.parents?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(13).SetCellValue(string.Join(", ", p.children?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(14).SetCellValue(string.Join(", ", p.siblings?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(15).SetCellValue(string.Join(", ", p.date_of_birth_remarks?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(16).SetCellValue(string.Join(", ", p.place_of_birth?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(17).SetCellValue(string.Join(", ", p.place_of_birth_remarks?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(18).SetCellValue(string.Join(", ", p.address?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(19).SetCellValue(string.Join(", ", p.address_remarks?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(20).SetCellValue(string.Join(", ", p.sanction_details?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(21).SetCellValue(string.Join(", ", p.description?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(22).SetCellValue(string.Join(", ", p.occupations?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(23).SetCellValue(string.Join(", ", p.positions?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(24).SetCellValue(string.Join(", ", p.political_parties?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(25).SetCellValue(string.Join(", ", p.links?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(26).SetCellValue(string.Join(", ", p.titles?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(27).SetCellValue(string.Join(", ", p.functions?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(28).SetCellValue(string.Join(", ", p.citizenship?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(29).SetCellValue(string.Join(", ", p.citizenship_remarks?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(30).SetCellValue(string.Join(", ", p.other_information?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()));
                row.CreateCell(31).SetCellValue(p.source_country);


            }


            using var ms2 = new MemoryStream();
            wb.Write(ms2, true);        

           var fileName = $"sanction_bulk_result_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xls";
            var content = ms2.ToArray();
            const string mime = "application/vnd.ms-excel";
            return File(content, mime, fileName);


           // return Ok(result);
        }

        // Search data by requird parameter from nec system.
        [Authorize]
        [HttpPost]
        [ApiKeyAuthorize]
        [Route("get-sourceLog")]
        public async Task<IActionResult> AMLSourceLog(UserFilter userFilter)
        {

            var res = await _sanctionService.GetAllSourceLog(userFilter.Payload.DtFrom.ToString(), userFilter.Payload.DtTo.Value.AddDays(1).ToString());

            return Ok(new { Header= new { UserId="null", apiKey="", ActionName= "SELECT", serviceName= "DocStatus" }, payload = res });
        }
        // Search data by requird parameter from nec system.
        [Authorize]
        [HttpPost]
        [ApiKeyAuthorize]
        [Route("get-datastatus-sourceLog")]
        public async Task<IActionResult> AMLDataStatusSourceLog(UserFilter userFilter)
        {

            var res = await _sanctionService.GetAllDataStatusLog(userFilter.Payload.DtFrom.ToString(),userFilter.Payload.DtTo.Value.AddDays(1).ToString());

            return Ok(new { Header = new { UserId = "null", apiKey = "", ActionName = "SELECT", serviceName = "DataDailyStatus" }, payload = res });
        }

        // Get all source type optional data from nec system.
        [HttpPost]
        [Route("get-all-listSources")]
        public async Task<IActionResult> GetListSources()
        {
            var res = await _sanctionService.GetAllSourceList();

            return Ok(new { sources = res });
        }
        //[HttpGet]
        //[Route("get-test")] 
        //public async Task<IActionResult> AMLDataStatusSource()
        //{
        //    string SingleMessage = _Singleton.GetMessage();
        //    int SingleCount = _Singleton.GetCount();
        //    string SingleMessage2 = _Singleton2.GetMessage();
        //    int SingleCount2 = _Singleton2.GetCount();

        //    string addscope = serviceAddScope.GetMessage();
        //    int addscopeCount = serviceAddScope.GetCount();
        //    string addscope2 = serviceAddScope2.GetMessage();
        //    int addscopeCount2 = serviceAddScope2.GetCount();

        //    string TransientMessage = _Transient.GetMessage();
        //    int TransientCount = _Transient.GetCount();
        //    string TransientMessage2 = _Transient2.GetMessage();
        //    int TransientCount2 = _Transient2.GetCount();

        //    var pp = new
        //    {
        //        singleMessage = SingleMessage,
        //        singleCount = SingleCount,
        //        singleMessage2 = SingleMessage2,
        //        singleCount2 = SingleCount2,

        //        transientMessage = TransientMessage,
        //        transientCount = TransientCount,
        //        transientMessage2 = TransientMessage2,
        //        transientCount2 = TransientCount2,
        //    };

        //    return Ok(pp);
        //}

        /// <summary>
        /// Upload SDN XML file and deserialize it
        /// </summary>
        [HttpPost("upload")] 
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

                    foreach (var entry in sdnList.SdnEntries)
                    {

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

        [HttpPost("Common")]
        public async Task<IActionResult> Common(AMLFilter aMLFilter)
        {
            try
            {
                 var res= await _CommonService.GetCommonSearch(aMLFilter);

                //var result2 = res
                //            .Where(x => x.Score >= aMLFilter.TopMatch).OrderByDescending(x => x.Score);




                var result2 = res
                .Where(x => x.Score >= aMLFilter.TopMatch)
                .GroupBy(x => new { x.Guid, x.Id })   // 👈 composite key
                .Select(g => g
                    .OrderByDescending(x => x.Score)
                    .First()).GroupBy(x => x.Guid)
                    .SelectMany(g => g.OrderByDescending(x => x.Score))
                    .ToList();


                //var result = res.Where(x => x?.Score >= aMLFilter.TopMatch).Select(c => new
                //{
                //    Id = c.Id,
                //    FirstName = c.FirstName?.ToString(),
                //    SecondName = c.SecondName?.ToString(),
                //    ThirdName = c.ThirdName?.ToString(),
                //    FourthName = c.FourthName?.ToString(),
                //    EntityType = c.EntityType?.ToString(),
                //    DateOfBirth = c.DateOfBirth?.ToString(),
                //    Address = c.Address?.ToString(),
                //    Country = c.Country?.ToString(),
                //    AliasName = c.Aliases?.ToString(),
                //    Source = c.Source?.ToString(),
                //    score = c.Score,
                //    Type=c.Type
                //}).ToList().OrderByDescending(a => a.score);

                return Ok( new {total= result2.Count(), res= result2 });

            }
            catch (System.Exception ex)
            {
                return BadRequest($"Error reading XML: {ex.Message}");
            }
        }


        [HttpPost("GetCommonById")]
        public async Task<IActionResult> GetCommonById(int id,string sourcetype)
        {
            try
            {
                if(sourcetype== "OFAC")
                {
                    return Ok(new { res = await _OfacService.GetSanctionDetailsById(id)});
                }
                else if(sourcetype=="UK")
                {                   
                   return Ok(new { res = await _UKService.GetSanctionDetailsById(id)}); 
                }
                else
                {
                    return Ok(new { res = await _UNService.GetSanctionDetailsById(id)}); 
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Error reading : {ex.Message}");
            }
        }
    }
}
