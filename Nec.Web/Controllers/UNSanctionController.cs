using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Models.Model;
using Nec.Web.Utils;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Nec.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UNSanctionController : ControllerBase
    {

        private readonly IUNService _UNService;
        NecAppConfig _appConfig;
        public UNSanctionController(IUNService UNService, NecAppConfig necAppConfig)
        {
            _UNService = UNService;
            _appConfig = necAppConfig;
        }
        [HttpPost("XMLFileUploadAndMap")]
        public async Task<IActionResult> UploadXmlFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (!file.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only XML files are allowed.");


            //  Read XML content
            using (var stream = file.OpenReadStream())
            {
                XDocument xmlDoc = XDocument.Load(stream);

                var individualElements = xmlDoc.Descendants("INDIVIDUAL");

                foreach (var ind in individualElements)
                {
                    var model = new IndividualModel
                    {
                        DataId = (string)ind.Element("DATAID")!,
                        VersionNum = (string)ind.Element("VERSIONNUM")!,
                        FirstName = (string)ind.Element("FIRST_NAME")!,
                        SecondName = (string)ind.Element("SECOND_NAME")!,
                        ThirdName = (string)ind.Element("THIRD_NAME")!,
                        FourthName = (string)ind.Element("FOURTH_NAME")!,
                        UnListType = (string)ind.Element("UN_LIST_TYPE")!,
                        ReferenceNumber = (string)ind.Element("REFERENCE_NUMBER")!,
                        ListedOn = (string)ind.Element("LISTED_ON")!,
                        Comments = (string)ind.Element("COMMENTS1")!,
                        ListType = (string)ind.Element("LIST_TYPE")?.Element("VALUE")!,
                        NameOriginalScript = (string)ind.Element("NAME_ORIGINAL_SCRIPT")!,
                        Gender = (string)ind.Element("GENDER")!,

                        Nationality = new NationalityModel
                        {
                            Value = ind.Element("NATIONALITY")?
                            .Elements("VALUE")
                            .Select(v => v.Value)
                            .ToList() ?? new List<string>()
                        },

                        LastDayUpdated = new LastDayUpdatedModel
                        {
                            Value = ind.Element("LAST_DAY_UPDATED")?
                            .Elements("VALUE")
                            .Select(v => v.Value)
                            .ToList() ?? new List<string>()
                        },

                        Designation = new DesignationModel
                        {
                            Value = ind.Element("DESIGNATION")?
                            .Elements("VALUE")
                            .Select(v => v.Value)
                            .ToList() ?? new List<string>()
                        },

                        Title = new TitleModel
                        {
                            Value = ind.Element("TITLE")?
                            .Elements("VALUE")
                            .Select(v => v.Value)
                            .ToList() ?? new List<string>()
                        },

                        Address = ind.Elements("INDIVIDUAL_ADDRESS")
                            .Select(addr => new AddressModel
                            {
                                City = (string)addr.Element("CITY")!,
                                Country = (string)addr.Element("COUNTRY")!,
                                Note = (string)addr.Element("NOTE")!,
                                State_Province = (string)addr.Element("STATE_PROVINCE")!,
                                Street = (string)addr.Element("STREET")!
                            })
                            .ToList(),

                        DateOfBirthYear = (string)ind.Element("INDIVIDUAL_DATE_OF_BIRTH")?.Element("YEAR")!,

                        Aliases = ind.Elements("INDIVIDUAL_ALIAS")
                            .Select(a => new AliasModel
                            {
                                Quality = (string)a.Element("QUALITY")! ?? null,
                                AliasName = (string)a.Element("ALIAS_NAME")!,
                                DateOfBirth = (string)a.Element("DATE_OF_BIRTH")!,
                                CityOfBirth = (string)a.Element("CITY_OF_BIRTH")! ?? null,
                                CountryOfBirth = (string)a.Element("COUNTRY_OF_BIRTH")! ?? null,
                                Note = (string)a.Element("NOTE")! ?? null
                            })
                            .ToList(),

                        IndividualDateOfBirth = ind.Elements("INDIVIDUAL_DATE_OF_BIRTH")
                            .Select(a => new IndividualDateOfBirthModel
                            {
                                TypeOfDate = (string)a.Element("TYPE_OF_DATE")! ?? null,
                                Date = (string)a.Element("DATE")!,
                                Year = (string)a.Element("YEAR")!,

                            })
                            .ToList(),

                        IndividualPlaceOfBirth = ind.Elements("INDIVIDUAL_PLACE_OF_BIRTH")
                            .Select(a => new IndividualPlaceOfBirthModel
                            {
                                City = (string)a.Element("CITY")! ?? null,
                                Country = (string)a.Element("COUNTRY")!,
                                State_Province = (string)a.Element("STATE_PROVINCE")!,
                            })
                            .ToList(),

                        IndividualDocument = ind.Elements("INDIVIDUAL_DOCUMENT")
                            .Select(a => new IndividualDocument
                            {
                                TypeOfDocument = (string)a.Element("TYPE_OF_DOCUMENT")! ?? null,
                                TypeOfDocument2 = (string)a.Element("TYPE_OF_DOCUMENT2")!,
                                Number = (string)a.Element("NUMBER")!,
                                IssuingCountry = (string)a.Element("ISSUING_COUNTRY")!,
                                DateOfIssue = (string)a.Element("DATE_OF_ISSUE")!,
                                CityOfIssue = (string)a.Element("CITY_OF_ISSUE")!,
                                Note = (string)a.Element("NOTE")!,
                            })
                            .ToList()
                    };

                    _UNService.CreateUNSanction(model);
                }
            }

            return Ok("Ok");
        }
    }
    
}
