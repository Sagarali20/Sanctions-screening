namespace Nec.Web.Helpers
{
    public class DbContext
    {
        public string ConnectionString { get; }

        public DbContext(IConfiguration configuration)
        {
            ConnectionString = configuration.GetConnectionString("hsCoonectionString");
        }

    }
}
