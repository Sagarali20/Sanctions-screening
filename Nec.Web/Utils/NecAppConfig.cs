namespace Nec.Web.Utils
{
    public  class NecAppConfig
    {
        public string? DilisenseUrl { get; }
        public string? APIKey { get; }
        public string? DownloadFilePath { get; }
        public string? OfacDownloadFilePath { get; }
        public string? logPath { get; }
        public string? AccessKey { get; }
        public string? JWTSecret { get; }

        public NecAppConfig(IConfiguration configuration)
        {
            DilisenseUrl = configuration["DilisenseSettings:DilisenseUrl"];
            APIKey = configuration["DilisenseSettings:APIKey"];
            DownloadFilePath = configuration["DilisenseSettings:DownloadFilePath"];
            OfacDownloadFilePath = configuration["OfacseSettings:OfacDownloadFilePath"];
            logPath = configuration["DilisenseSettings:LogPath"];
            AccessKey = configuration["ApiKeySettings:Key"];
            JWTSecret = configuration["JwtSettings:Secret"];       
        }

    }
}
