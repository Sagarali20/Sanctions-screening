using Nec.Web.Interfaces;
using Nec.Web.Utils;
using Npgsql;
using System.Data;
using System.Data.SqlClient;

namespace Nec.Web.Config
{

    public class DbConnection : IIDbConnection
    {
        public readonly string? _connectionString;
        public readonly string? _connectionStringEft;

        public DbConnection(IConfiguration _configuration) 
        {
            _connectionString = _configuration.GetConnectionString("hsCoonectionString");

           //  _connectionString = EncryptionHelper.Decrypt(_configuration.GetConnectionString("hsCoonectionString"), _configuration.GetValue<string>("Encryption:Key"), _configuration.GetValue<string>("Encryption:IV"));
            _connectionStringEft = EncryptionHelper.Decrypt(_configuration.GetConnectionString("hsCoonectionStringEft"), _configuration.GetValue<string>("Encryption:Key"), _configuration.GetValue<string>("Encryption:IV"));
        }
        public SqlConnection CreateConnectionsql()
        {
            return new SqlConnection(_connectionString);
        }
        public SqlConnection CreateConnectionsqlEft()
        {
            return new SqlConnection(_connectionStringEft);
        }
    }
}
