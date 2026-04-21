using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Nec.Web.Config;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Models.Model;
using Nec.Web.Services;
using Nec.Web.Utils;
using NPOI.POIFS.Crypt;
using NPOI.SS.Formula.Functions;
using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UserInfo = Nec.Web.Models.UserInfo;

namespace Nec.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        //private readonly string logPath = @"C:\logs\DilisenseLog";
        private readonly ILogger<UserController> _logger;
        private readonly IConfiguration _configuration;
        NecAppConfig _appConfig;


        public UserController(IUserService userService, ILogger<UserController> logger, IConfiguration configuration, NecAppConfig appConfig)
        {
            _userService = userService;
            _logger = logger;
            _configuration = configuration;
            _appConfig = appConfig;
        }
        //[Authorize]
        [HttpPost]
        [ApiKeyAuthorize]
        [Route("process")]
        public async Task<IActionResult> SaveUser(User model)
        {

            //string decrypted = CryptoJsAesDecryptor.Decrypt(model2, "n3cM0n3y#0");
            try
            {


                if (model.Payload?.ActionName?.ToUpper()== "NEW")
                {
                    var user = await _userService.GetUserByUserName(model.Payload.Email);
                    if (user.Email is not null)
                    {
                        return Conflict(new {error= "User Already Exists !!" });
                    }
                    model.Payload.Password = PasswordHasher.HashPassword(model.Payload.Password);
                    _userService.CreateUser(model);

                    return Ok(model);
                }
                else
                {
                    _userService.UpdateUser(model);

                    return Ok(model);
                }

            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing file: {ex.Message}");
            }

        }
        [Authorize]
        [HttpPost]
        [ApiKeyAuthorize]
        [Route("getalluser")]
        public async Task<IActionResult> GetAllUser()
        {

            try
            {
                var dd = await _userService.GetAllUser();

                return Ok(new
                {
                    Header= new
                    {
                        userId="null",
                        apiKey="null",
                        actionName= "SELECT",
                        serviceName= "User"
                    },
                    Payload = dd
                });
            }
            catch (Exception ex)
            {
                return Ok(ex.InnerException);
            }

        }

        [HttpPost]
        [ApiKeyAuthorize]
        [Route("authenticate")]
        public async Task<IActionResult> Authenticate(UserRequest userRequest)
        {

            try
            {
                var user = await _userService.ValidationUser(userRequest.Username);

                if (user.Email == null)
                {
                    return Unauthorized(new { Error = "User not found" });
                }
                if (!PasswordHasher.VerifyPassword(userRequest.Password, user.Password))
                {
                    return Unauthorized(new { Error = "Password is incorrect" });
                }
                var token = createJwt(user);
                return Ok(new
                {
                    Jwttoken = token
                });
            }
            catch (Exception ex)
            {
                return Ok(ex.InnerException);
            }

        }

        [Authorize]
        [HttpPost]
        [ApiKeyAuthorize]
        [Route("GetUserInfo")]
        public async Task<IActionResult> UserInfo(UserInfoRequest userRequest)
        {

            try
            {
                var user = await _userService.GetUserByUserName(userRequest.Payload.LoginName);

                if (user == null)
                {
                    return NotFound(new { Error = "User not found" });
                }
                if (!PasswordHasher.VerifyPassword(userRequest.Payload.Password, user.Password))
                {
                    return BadRequest(new { Error = "User not found" });
                }
                user.Password = null;
                return Ok(new
                {
                    Header= new
                    {
                        UserId="null",
                        ApiKey = "null",
                        ActionName = "SELECT",
                        ServiceName = "User",
                    },
                    payload = user
                });
            }
            catch (Exception ex)
            {
                return Ok(ex.InnerException);
            }

        }
        [Authorize]
        [HttpPost]
        [ApiKeyAuthorize]
        [Route("GetRole")]
        public async Task<IActionResult> GetRole(UserInfoRequest userRequest)
        {

            try
            {
                var roles = await _userService.GetAlLRole();

                var response = new ApiResponse<RolePayload>
                {
                    Header = new HeaderR
                    {
                        UserId = null,
                        ApiKey = null,
                        ActionName = "GET_ROLES",
                        ServiceName = "User"
                    },
                    Payload = new List<RolePayload>
                    {
                        new RolePayload
                        {
                            Roles = roles
                        }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(ex.InnerException);
            }

        }

        [Authorize]
        [HttpPost]
        [ApiKeyAuthorize]
        [Route("user-activity")]
        public async Task<IActionResult> GetListSources(UserFilter userFilter)
        {
            var res = await _userService.GetAllUserActivity(userFilter);

            return Ok(new { Header = new { UserId="null", apiKey="null", ActionName= "SELECT_ACTIVITY", ServiceName= "UsageRecord" }, payload = res });
        }
        private string createJwt(UserInfo user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(_appConfig.JWTSecret!);
            var identity = new ClaimsIdentity(new Claim[]
            {
               // new Claim(ClaimTypes.Role,user.Role),
				new Claim(ClaimTypes.Name,$"{user.FirstName} {user.LastName}")
				//new Claim(ClaimTypes.Name,$"{user.Username}")
            });

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddDays(1),
                //Expires = DateTime.Now.AddSeconds(10),
                SigningCredentials = credentials
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);
        }

        [HttpGet]
        [Route("DownloadLogs")]
        public IActionResult DownloadLogs([FromQuery] string date)
        {
            if (string.IsNullOrEmpty(date))
                return BadRequest("Date is required (format: yyyyMMdd)");

            // Validate date format
            if (!Regex.IsMatch(date, @"^\d{8}$"))
                return BadRequest("Invalid date format. Use yyyyMMdd");

            // Match both main and split logs
            string pattern = $@"Serilog_dilisense{date}(_\d+)?\.log";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            var files = Directory.GetFiles(_appConfig.logPath!)
                                 .Where(f => regex.IsMatch(Path.GetFileName(f)))
                                 .ToList();

            if (!files.Any())
                return NotFound("No log files found for this date");

            using (var memoryStream = new MemoryStream())
            {
                using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in files)
                    {
                        var entry = zip.CreateEntry(Path.GetFileName(file));

                        using (var entryStream = entry.Open())
                        using (var fileStream = new FileStream(
                            file,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.ReadWrite   
                        ))
                        {
                            fileStream.CopyTo(entryStream);
                        }
                    }
                }

                memoryStream.Position = 0;

                string zipName = $"logs_{date}.zip";
                return File(memoryStream.ToArray(), "application/zip", zipName);
            }
        }

        [HttpGet("download")]
        public IActionResult DownloadLogs([FromQuery] string date, [FromQuery] string type = "info")
        {
            if (string.IsNullOrEmpty(date))
                return BadRequest("Date is required (format: yyyyMMdd)");

            if (!Regex.IsMatch(date, @"^\d{8}$"))
                return BadRequest("Invalid date format. Use yyyyMMdd");

            // Determine folder & filename pattern
            string folderPath;
            string pattern;

            if (type.ToLower() == "error")
            {
                folderPath = Path.Combine(_appConfig.logPath!, "error");
                pattern = $@"error-{date}(_\d+)?\.log";
            }
            else
            {
                folderPath = Path.Combine(_appConfig.logPath!, "info");
                pattern = $@"log-{date}(_\d+)?\.log";
            }

            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            if (!Directory.Exists(folderPath))
                return NotFound("Log folder not found");

            var files = Directory.GetFiles(folderPath)
                                 .Where(f => regex.IsMatch(Path.GetFileName(f)))
                                 .OrderBy(f => f)
                                 .ToList();

            if (!files.Any())
                return NotFound("No log files found");

            // ZIP create (safe for multiple files)
            using (var memoryStream = new MemoryStream())
            {
                using (var zip = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    foreach (var file in files)
                    {
                        var entry = zip.CreateEntry(Path.GetFileName(file));

                        using (var entryStream = entry.Open())
                        using (var fileStream = new FileStream(
                            file,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.ReadWrite   
                        ))
                        {
                            fileStream.CopyTo(entryStream);
                        }
                    }
                }

                memoryStream.Position = 0;

                string zipName = $"{type}_logs_{date}.zip";
                return File(memoryStream.ToArray(), "application/zip", zipName);
            }
        }


        [HttpGet("getCheck")]
        public async Task<IActionResult> getCheck()
        {

            try
            {
               
                int a = 0,b=1;
                var c = b / a;

                return Ok("ok");
            }
            catch (Exception ex)
            {
                _logger.LogError("An error: " + ex.ToString());
                return Ok(ex.ToString());
            }

        }

        //private async Task<string> CreateRefreshToken()
        //{
        //    var tokenBytes = RandomNumberGenerator.GetBytes(64);
        //    var RefreshToken = Convert.ToBase64String(tokenBytes);
        //    List<User> model = await _userLoginService.GetAllUser();
        //    var tokeninuser = model.FirstOrDefault(x => x.RefreshToken == RefreshToken);
        //    if (tokeninuser != null)
        //    {
        //        return await CreateRefreshToken();
        //    }
        //    return RefreshToken;
        //}

        private ClaimsPrincipal GetPrincipleFromExpiredToken(string token)
        {
            var key = Encoding.ASCII.GetBytes("veryverysecret.....");

            var tokenValidationParameter = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = false,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = false
            };
            var tokenhandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenhandler.ValidateToken(token, tokenValidationParameter, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("this is invalid token");
            return principal;

        }

    }
}
