using DocumentFormat.OpenXml.EMMA;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;

namespace Nec.Web.Services
{
    public class UKService : IUKService
    {
        public IIDbConnection _dbConnection;
        public UKService(IIDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public bool CreateUKSanction(Designation model)
        {
            int resultStatus;
            int newRecordId=0;

            string storedProcedureName = "InsertUKSanction";
            using (SqlConnection con = _dbConnection.CreateConnectionsql())
            {
                con.Open();
                IDbTransaction transaction = con.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                try
                {
                    using (SqlCommand cmd = new SqlCommand("", con, (SqlTransaction)transaction))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = storedProcedureName;
                        cmd.Parameters.AddWithValue("@LastUpdated", model.LastUpdated);
                        cmd.Parameters.AddWithValue("@DateDesignated", model.DateDesignated);
                        cmd.Parameters.AddWithValue("@UniqueID", model.UniqueID);
                        cmd.Parameters.AddWithValue("@OFSIGroupID", model.OFSIGroupID);
                        cmd.Parameters.AddWithValue("@UNReferenceNumber", model.UNReferenceNumber);    
                        cmd.Parameters.AddWithValue("@Names", JsonSerializer.Serialize(model.Names));
                        cmd.Parameters.AddWithValue("@NonLatinNames", JsonSerializer.Serialize(model.NonLatinNames));
                        cmd.Parameters.AddWithValue("@Titles", JsonSerializer.Serialize(model.Titles));
                        cmd.Parameters.AddWithValue("@RegimeName", model.RegimeName);
                        cmd.Parameters.AddWithValue("@IndividualEntityShip", model.IndividualEntityShip);
                        cmd.Parameters.AddWithValue("@DesignationSource", model.DesignationSource);
                        cmd.Parameters.AddWithValue("@SanctionsImposed", model.SanctionsImposed);
                        cmd.Parameters.AddWithValue("@SanctionsImposedIndicators", JsonSerializer.Serialize(model.SanctionsImposedIndicators));
                        cmd.Parameters.AddWithValue("@OtherInformation", model.OtherInformation);
                        cmd.Parameters.AddWithValue("@UKStatementofReasons", model.UKStatementofReasons);
                        cmd.Parameters.AddWithValue("@IndividualDetails", JsonSerializer.Serialize(model.IndividualDetails));

                        SqlParameter outParameter = new SqlParameter("@ResultStatus", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outParameter);
                        SqlParameter outErrorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, -1)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outErrorParam);
                        var result = cmd.ExecuteScalarAsync().Result;

                        if (result != null && int.TryParse(result.ToString(), out int id))
                        {
                            newRecordId = id;
                        }
                        // cmd.ExecuteNonQuery();
                        resultStatus = (int)cmd.Parameters["@ResultStatus"].Value;
                        string? errorMessage = cmd.Parameters["@ErrorMessage"].Value?.ToString();

                        transaction.Commit();
                        if (transaction.Connection != null)
                        {
                            transaction.Connection.Close();
                        }


                        if (resultStatus == 1)
                        {
                            SaveUKName(model, newRecordId);
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        private bool SaveUKName(Designation model, int id)
        {

            string q = "";

            if (model.Names is not null && model.Names.NameList.Count > 0)
            {
                foreach (var item in model.Names.NameList)
                {
                    q += $"insert into SanctionNameInfo (FirstName,LastName,ThirdName,SourceType,RefId,Created) values('{item.Name1?.Replace("'", "''") ?? null}','{item.Name2?.Replace("'", "''") ?? null}','{item.Name6?.Replace("'", "''") ?? null}','UK',{id},'{DateTime.Now}');";
                }
            }

            try
            {
                using (SqlConnection con = _dbConnection.CreateConnectionsql())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(q, con))
                    {
                        // Return the inserted ID
                        int row = cmd.ExecuteNonQuery();

                        if (row > 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }


        public async Task<Designation> GetSanctionDetailsById(int id)
        {
            Designation designation = new Designation();

            try
            {
                string Query = "select * from UKSanction where Id=" + id;

                using (var conn = _dbConnection.CreateConnectionsql())
                using (var cmd = new SqlCommand(Query, conn))
                {

                    await conn.OpenAsync();


                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Designation sanctionEntity = new Designation
                            {
                                //Names = reader["FirstName"].ToString(),        
                                Names = JsonSerializer.Deserialize<Names>(reader["Names"].ToString()),
                                NonLatinNames = JsonSerializer.Deserialize<NonLatinNames>(reader["NonLatinNames"].ToString()),
                                Titles = JsonSerializer.Deserialize<Titles>(reader["Titles"].ToString()),
                                RegimeName = reader["RegimeName"].ToString(),
                                IndividualEntityShip = reader["IndividualEntityShip"].ToString(),
                                DesignationSource = reader["DesignationSource"].ToString(),
                                SanctionsImposed = reader["SanctionsImposed"].ToString(),
                                SanctionsImposedIndicators = JsonSerializer.Deserialize<SanctionsImposedIndicators>(reader["SanctionsImposedIndicators"].ToString()),
                                OtherInformation = reader["OtherInformation"].ToString(),
                                UKStatementofReasons = reader["UKStatementofReasons"].ToString(),
                                IndividualDetails = JsonSerializer.Deserialize<IndividualDetails>(reader["IndividualDetails"].ToString())
                            };
                            // Flatten the DOBLists and join all dates into a single comma-separated string
                            var dobString = string.Join(", ",
                                JsonSerializer
                                    .Deserialize<IndividualDetails>(reader["IndividualDetails"].ToString())
                                    .IndividualList
                                    .SelectMany(x => x.DOBs.DOBList)   // SelectMany flattens multiple lists
                            );
                            // Output: dd/mm/1960, dd/mm/1962, dd/mm/1965
                            designation = sanctionEntity;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return designation;
        }

    }
}
