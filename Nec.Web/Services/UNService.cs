using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.EMMA;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Models.Model;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;

namespace Nec.Web.Services
{
    public class UNService : IUNService
    {
        public IIDbConnection _dbConnection;
        public UNService(IIDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public bool CreateUNRefDetails(string query)
        {

            throw new NotImplementedException();

        }

        public bool CreateUNSanction(IndividualModel model)
        {
            int resultStatus;
            int newRecordId=0;

            string storedProcedureName = "InsertUNSanction";
            using (SqlConnection con = _dbConnection.CreateConnectionsql())
            {
                con.Open();
                IDbTransaction transaction = con.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

                try
                {
                    using (SqlCommand cmd = new SqlCommand("", con, (SqlTransaction)transaction))
                    {
                        // Specify that the SqlCommand is a stored procedure
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = storedProcedureName;

                        cmd.Parameters.AddWithValue("@DataId", model.DataId);
                        cmd.Parameters.AddWithValue("@VersionNum", model.VersionNum);
                        cmd.Parameters.AddWithValue("@FirstName", model.FirstName);
                        cmd.Parameters.AddWithValue("@SecondName", model.SecondName);
                        cmd.Parameters.AddWithValue("@ThirdName", model.ThirdName);
                        cmd.Parameters.AddWithValue("@FourthName", model.FourthName);
                        cmd.Parameters.AddWithValue("@UnListType", model.UnListType);
                        cmd.Parameters.AddWithValue("@ReferenceNumber", model.ReferenceNumber);
                        cmd.Parameters.AddWithValue("@ListedOn", model.ListedOn);
                        cmd.Parameters.AddWithValue("@NameOriginalScript", model.NameOriginalScript);
                        cmd.Parameters.AddWithValue("@Gender", model.Gender);
                        cmd.Parameters.AddWithValue("@DateOfBirthYear", model.DateOfBirthYear);
                        cmd.Parameters.AddWithValue("@ListType", model.ListType);
                        cmd.Parameters.AddWithValue("@Nationality", JsonSerializer.Serialize(model.Nationality));
                        cmd.Parameters.AddWithValue("@LastDayUpdated", JsonSerializer.Serialize(model.LastDayUpdated));
                        cmd.Parameters.AddWithValue("@Designation", JsonSerializer.Serialize(model.Designation));
                        cmd.Parameters.AddWithValue("@Title", JsonSerializer.Serialize(model.Title));
                        cmd.Parameters.AddWithValue("@Address", JsonSerializer.Serialize(model.Address));
                        cmd.Parameters.AddWithValue("@Aliases", JsonSerializer.Serialize(model.Aliases));
                        cmd.Parameters.AddWithValue("@IndividualDateOfBirth", JsonSerializer.Serialize(model.IndividualDateOfBirth));
                        cmd.Parameters.AddWithValue("@IndividualPlaceOfBirth", JsonSerializer.Serialize(model.IndividualPlaceOfBirth));
                        cmd.Parameters.AddWithValue("@IndividualDocument", JsonSerializer.Serialize(model.IndividualDocument));
                        cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

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
                        resultStatus = (int)cmd.Parameters["@ResultStatus"].Value;
                        string? errorMessage = cmd.Parameters["@ErrorMessage"].Value?.ToString();

                        transaction.Commit();
                        if (transaction.Connection != null)
                        {
                            transaction.Connection.Close();
                        }

                        if (resultStatus == 1)
                        {
                            SaveUNName(model,newRecordId);
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

        private bool SaveUNName(IndividualModel model, int id)
        {

            string q = $"insert into SanctionNameInfo (FirstName,LastName,ThirdName,FourthName,SourceType,RefId,Created) values('{model.FirstName?.Replace("'", "''") ?? null}','{model.SecondName?.Replace("'", "''") ?? null}','{model.ThirdName?.Replace("'", "''") ?? null}','{model.FourthName?.Replace("'", "''") ?? null}','UN',{id},'{DateTime.Now}');"; 

            if (model.Aliases is not null && model.Aliases.Count > 0)
            {
                foreach (var item in model.Aliases)
                {
                    if(!string.IsNullOrWhiteSpace(item.AliasName))
                    {
                        q += $"insert into SanctionNameInfo (Aliases,SourceType,RefId,Created) values('{item.AliasName?.Replace("'", "''") ?? null}','UN',{id},'{DateTime.Now}');";

                    }
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


        public async Task<IndividualModel> GetSanctionDetailsById(int id)
        {
            IndividualModel individualModel = new IndividualModel();

            try
            {
                string Query = "select * from UNSanction where Id=" + id;

                using (var conn = _dbConnection.CreateConnectionsql())
                using (var cmd = new SqlCommand(Query, conn))
                {

                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            IndividualModel sanctionEntity = new IndividualModel
                            {
                                FirstName = reader["FirstName"].ToString(),        
                                SecondName = reader["SecondName"].ToString(),        
                                ThirdName = reader["ThirdName"].ToString(),        
                                FourthName = reader["FourthName"].ToString(),        
                                UnListType = reader["UnListType"].ToString(),        
                                ReferenceNumber = reader["ReferenceNumber"].ToString(),        
                                ListedOn = reader["ListedOn"].ToString(),        
                                NameOriginalScript = reader["NameOriginalScript"].ToString(),        
                                Gender = reader["Gender"].ToString(),        
                                ListType = reader["ListType"].ToString(),        
                                DateOfBirthYear = reader["DateOfBirthYear"].ToString(),        

                                Nationality = JsonSerializer.Deserialize<NationalityModel>(reader["Nationality"].ToString()),
                                LastDayUpdated = JsonSerializer.Deserialize<LastDayUpdatedModel>(reader["LastDayUpdated"].ToString()),
                                Designation = JsonSerializer.Deserialize<DesignationModel>(reader["Designation"].ToString()),
                                Title = JsonSerializer.Deserialize<TitleModel>(reader["Designation"].ToString()),
                                Address = JsonSerializer.Deserialize<List<AddressModel>>(reader["Address"].ToString()),
                                Aliases = JsonSerializer.Deserialize<List<AliasModel>>(reader["Address"].ToString()),
                                IndividualDateOfBirth = JsonSerializer.Deserialize<List<IndividualDateOfBirthModel>>(reader["IndividualDateOfBirth"].ToString()),
                                IndividualPlaceOfBirth = JsonSerializer.Deserialize<List<IndividualPlaceOfBirthModel>>(reader["IndividualPlaceOfBirth"].ToString()),
                                IndividualDocument = JsonSerializer.Deserialize<List<IndividualDocument>>(reader["IndividualDocument"].ToString())

                            };
                            individualModel = sanctionEntity;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return individualModel;
        }
    }
}
