using ICSharpCode.SharpZipLib.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nec.Web.Config;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Services;
using Nec.Web.Utils;
using NPOI.HSSF.UserModel;

namespace Nec.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommonController : ControllerBase
    {

        private readonly ICommonService _CommonService;
        NecAppConfig _appConfig;
        public CommonController(ICommonService CommonService, NecAppConfig necAppConfig)
        {
            _CommonService = CommonService;
            _appConfig = necAppConfig;
        }
        [HttpPost("upload-and-download-excel")]
        public async Task<IActionResult> UploadXls(IFormFile file)
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
            List<CommonSearchResult> results = new List<CommonSearchResult>();
            try
            {

                foreach (var row in result)
                {

                    var model = new AMLFilter
                    {
                        Name = row.FullName?.Replace("'", "''") ?? "",
                        EntityType = row.RecordType?.Replace("'", "''") ?? "",
                        Address = row.AddressLine1?.Replace("'", "''") ?? "",
                        City = row.City?.Replace("'", "''") ?? "",
                        State = row.State?.Replace("'", "''") ?? "",
                        Country = row.Country?.Replace("'", "''") ?? "",
                        PostCode = row.PostCode?.Replace("'", "''") ?? "",
                        DateOfBirth = row.DateOfBirth?.Replace("'", "''") ?? "",
                        Guid = row.Guid

                    };

                    //var lst = await _CommonService.GetExcelSanctionDetailsBySearch(row.FullName, row.RecordType, row.AddressLine1, row.City, row.State, row.Country, row.DateOfBirth, row.Guid);
                    var lst = await _CommonService.GetCommonSearchForExcel(model);
                    if (lst != null && lst.Count > 0)
                    {
                        results.AddRange(lst);
                    }

                }
            }
            catch (Exception ex)
            {

                throw;
            }
            //var results2 = results
            //.Where(x => x.Score >= 80)
            //.GroupBy(x => x.Id)                // 👈 remove duplicate IDs
            //.Select(g => g
            //    .OrderByDescending(x => x.Score)
            //    .First())
            //.ToList();
            var results2 = results
            .Where(x => x.Score >= 80)
            .GroupBy(x => new { x.Guid, x.Id })   // 👈 composite key
            .Select(g => g
                .OrderByDescending(x => x.Score)
                .First()).GroupBy(x => x.Guid)
                .SelectMany(g => g.OrderByDescending(x => x.Score))
                .ToList(); 




            //var results3 = results2
            //    .GroupBy(x => x.Guid)
            //    .SelectMany(g => g.OrderByDescending(x => x.Score))
            //    .ToList();


            var wb = new HSSFWorkbook();
            var sheet2 = wb.CreateSheet("DilisenseOfacUKUNBulkResult");

            // --- Header row ------------------------------------------------------
            string[] headers =
            [
             "GUID", "EntityType", "FirstName",
            "SecondName", "ThirdName", "FourthName",
            "Aliases", "DateOfBirth", "Address", "Country","Source", "Score"
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
            foreach (var p in results2)
            {
                var row = sheet2.CreateRow(rowIndex++);

                row.CreateCell(0).SetCellValue(p.Guid);
                row.CreateCell(1).SetCellValue(p.EntityType);
                row.CreateCell(2).SetCellValue(p.FirstName);
                row.CreateCell(3).SetCellValue(p.SecondName);
                row.CreateCell(4).SetCellValue(p.ThirdName);
                row.CreateCell(5).SetCellValue(p.FourthName);
                row.CreateCell(6).SetCellValue(p.Aliases);
                row.CreateCell(7).SetCellValue(p.DateOfBirth);
                row.CreateCell(8).SetCellValue(p.Address);
                row.CreateCell(9).SetCellValue(p.Country);
                row.CreateCell(10).SetCellValue(p.DataSource);
                row.CreateCell(11).SetCellValue(Convert.ToDouble(p.Score));

            }


            using var ms2 = new MemoryStream();
            wb.Write(ms2, true);

            var fileName = $"Dilisense_Ofac_UK_UN_bulk_result_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xls";
            var content = ms2.ToArray();
            const string mime = "application/vnd.ms-excel";
            return File(content, mime, fileName);


            // return Ok(result);
        }

    }
}
