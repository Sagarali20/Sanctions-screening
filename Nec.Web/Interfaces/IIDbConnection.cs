using Npgsql;
using System.Data;
using System.Data.SqlClient;

namespace Nec.Web.Interfaces
{
    public interface IIDbConnection
    {
        SqlConnection CreateConnectionsql();
    }
}
