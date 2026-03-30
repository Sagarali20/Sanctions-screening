using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Utils;
using System.Xml;
using System.Xml.Serialization;

namespace Nec.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UkSanctionController : ControllerBase
    {
        private readonly IUKService _UKService;
        NecAppConfig _appConfig;
        public UkSanctionController(IUKService UKService, NecAppConfig necAppConfig)
        {
            _UKService = UKService;
            _appConfig = necAppConfig;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadXmlFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a valid XML file.");
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Designations));

                XmlReaderSettings settings = new XmlReaderSettings
                {
                    IgnoreWhitespace = true
                };

                using (var stream = file.OpenReadStream())
                using (XmlReader reader = XmlReader.Create(stream, settings))
                {
                    // Ignore namespaces
                    XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                    ns.Add("", "");

                    Designations data = (Designations)serializer.Deserialize(reader);

                    foreach(var item in data.DesignationList)
                    {
                        _UKService.CreateUKSanction(item);
                    }
                    return Ok(new
                    {
                        Message = "File uploaded and parsed successfully!",
                        DateGenerated = data.DateGenerated,
                        TotalRecords = data.DesignationList.Count,
                        FirstDesignationId = data.DesignationList.FirstOrDefault()?.UniqueID
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.InnerException?.Message ?? ex.Message });
            }
        }

    }
}
