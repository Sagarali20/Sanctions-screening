using Nec.Web.Interfaces;
using Npgsql;
using System.Data;
using System.Data.SqlClient;

namespace Nec.Web.Config
{

    public class DbConnection : IIDbConnection
    {
        public readonly string? _connectionString;

        public DbConnection(IConfiguration configuration) 
        {
            _connectionString = configuration.GetConnectionString("hsCoonectionString");

        }

        public SqlConnection CreateConnectionsql()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
