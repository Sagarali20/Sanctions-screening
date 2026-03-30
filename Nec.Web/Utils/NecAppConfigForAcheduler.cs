namespace Nec.Web.Utils
{
    public class NecAppConfigForAcheduler
    {
        public string? DilisenseUrl { get; }
        public string? APIKey { get; }
        public string? DownloadFilePath { get; }
        public NecAppConfigForAcheduler(IConfiguration configuration)
        {
            DilisenseUrl = configuration["DilisenseSettings:DilisenseUrl"];
            APIKey = configuration["DilisenseSettings:APIKey"];
            DownloadFilePath = configuration["DilisenseSettings:DownloadFilePath"];
        }

    }
}
