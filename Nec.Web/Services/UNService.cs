using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.EMMA;
using Nec.Web.Controllers;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Models.Model;
using Nec.Web.Utils;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;

namespace Nec.Web.Services
{
    public class UNService : IUNService
    {
        public IIDbConnection _dbConnection;
        private readonly ILogger<UNService> _logger;
        private readonly ISanctionService _sanctionService;


        public UNService(IIDbConnection dbConnection, ILogger<UNService> logger, ISanctionService sanctionService)
        {
            _dbConnection = dbConnection;
            _logger = logger;
            _sanctionService = sanctionService;
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

        public bool CreateUNSanction(List<IndividualModel> models)
        {
            int insert = 0, update = 0, deletedRows = 0;

            AMLSourceLog aMLSourceLog = new AMLSourceLog();

            if (models == null || models.Count == 0)
                return true;

            using (SqlConnection con = _dbConnection.CreateConnectionsql())
            {
                con.Open();

                var existingHashes = new Dictionary<int, string>();

                using (var cmd = new SqlCommand(
                    "SELECT DataId, HashCheck FROM UNSanction",
                    con))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingHashes[Convert.ToInt32( reader["DataId"].ToString())] =
                                reader["HashCheck"].ToString();
                        }
                    }
                }
                List<IndividualModel> newRecords = new List<IndividualModel>();

                foreach (var model in models)
                {
                    string Check = HashHelper.ComputeSha256Hash(JsonSerializer.Serialize(model));
                    model.HashCheck = Check;

                    if (existingHashes.TryGetValue(model.DataId, out var dbHash))
                    {
                        if (dbHash != Check)
                        {
                            update++;
                            // ********** UPDATE **********
                            using (var cmd = new SqlCommand(@"
                                    UPDATE UNSanction
                                    SET 
                                          FirstName        = @FirstName
                                        , SecondName   = @SecondName
                                        , ThirdName = @ThirdName
                                        , FourthName          = @FourthName
                                        , UnListType       = @UnListType
                                        , ReferenceNumber          = @ReferenceNumber
                                        , ListedOn      = @ListedOn
                                        , NameOriginalScript          = @NameOriginalScript
                                        , Gender      = @Gender
                                        , ListType           = @ListType
                                        , DateOfBirthYear  = @DateOfBirthYear
                                        , Nationality = @Nationality
                                        , Designation  = @Designation
                                        , Title       = @Title
                                        , Address       = @Address
                                        , Aliases       = @Aliases
                                        , IndividualDateOfBirth       = @IndividualDateOfBirth
                                        , IndividualPlaceOfBirth       = @IndividualPlaceOfBirth
                                        , IndividualDocument       = @IndividualDocument
                                      
                                    WHERE DataId = @DataId;
                                ", con))
                            {
                                cmd.Parameters.AddWithValue("@DataId", DbVal(model.DataId) ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@FirstName", DbVal(model.FirstName));
                                cmd.Parameters.AddWithValue("@SecondName", DbVal(model.SecondName));
                                cmd.Parameters.AddWithValue("@ThirdName", DbVal(model.ThirdName));
                                cmd.Parameters.AddWithValue("@FourthName", DbVal(model.FourthName));
                                cmd.Parameters.AddWithValue("@UnListType", DbVal(model.UnListType));
                                cmd.Parameters.AddWithValue("@ReferenceNumber", DbVal(model.ReferenceNumber));
                                cmd.Parameters.AddWithValue("@ListedOn", DbVal(model.ListedOn));
                                cmd.Parameters.AddWithValue("@NameOriginalScript", DbVal(model.NameOriginalScript));
                                cmd.Parameters.AddWithValue("@Gender", DbVal(model.Gender));
                                cmd.Parameters.AddWithValue("@ListType", DbVal(model.ListType));
                                cmd.Parameters.AddWithValue("@DateOfBirthYear", DbVal(model.DateOfBirthYear));
                                cmd.Parameters.AddWithValue("@Nationality", DbVal(JsonSerializer.Serialize(model.Nationality)));
                                cmd.Parameters.AddWithValue("@Designation", DbVal(JsonSerializer.Serialize(model.Designation)));
                                cmd.Parameters.AddWithValue("@Title", DbVal(JsonSerializer.Serialize(model.Title)));
                                cmd.Parameters.AddWithValue("@Address", DbVal(JsonSerializer.Serialize(model.Address)));
                                cmd.Parameters.AddWithValue("@Aliases", DbVal(JsonSerializer.Serialize(model.Aliases)));
                                cmd.Parameters.AddWithValue("@IndividualDateOfBirth", DbVal(JsonSerializer.Serialize(model.IndividualDateOfBirth)));
                                cmd.Parameters.AddWithValue("@IndividualPlaceOfBirth", DbVal(JsonSerializer.Serialize(model.IndividualPlaceOfBirth)));
                                cmd.Parameters.AddWithValue("@IndividualDocument", DbVal(JsonSerializer.Serialize(model.IndividualDocument)));
                                cmd.ExecuteNonQuery();
                            }

                        }
                    }
                    else
                    {
                        insert++;
                        newRecords.Add(model);

                    }
                }
                if (newRecords.Any())
                {
                    try
                    {
                        DataTable dt = CreateUNSanctionDataTable(newRecords);

                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(con))
                        {
                            bulkCopy.DestinationTableName = "UNSanction";
                            bulkCopy.BatchSize = 5000;
                            bulkCopy.BulkCopyTimeout = 600;
                            bulkCopy.ColumnMappings.Add("DataId", "DataId");
                            bulkCopy.ColumnMappings.Add("VersionNum", "VersionNum");
                            bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
                            bulkCopy.ColumnMappings.Add("SecondName", "SecondName");
                            bulkCopy.ColumnMappings.Add("ThirdName", "ThirdName");
                            bulkCopy.ColumnMappings.Add("FourthName", "FourthName");
                            bulkCopy.ColumnMappings.Add("UnListType", "UnListType");
                            bulkCopy.ColumnMappings.Add("ReferenceNumber", "ReferenceNumber");
                            bulkCopy.ColumnMappings.Add("ListedOn", "ListedOn");
                            bulkCopy.ColumnMappings.Add("NameOriginalScript", "NameOriginalScript");
                            bulkCopy.ColumnMappings.Add("Gender", "Gender");
                            bulkCopy.ColumnMappings.Add("ListType", "ListType");
                            bulkCopy.ColumnMappings.Add("DateOfBirthYear", "DateOfBirthYear");
                            bulkCopy.ColumnMappings.Add("Nationality", "Nationality");
                            bulkCopy.ColumnMappings.Add("LastDayUpdated", "LastDayUpdated");
                            bulkCopy.ColumnMappings.Add("Designation", "Designation");
                            bulkCopy.ColumnMappings.Add("Title", "Title");
                            bulkCopy.ColumnMappings.Add("Address", "Address");
                            bulkCopy.ColumnMappings.Add("Aliases", "Aliases");
                            bulkCopy.ColumnMappings.Add("IndividualDateOfBirth", "IndividualDateOfBirth");
                            bulkCopy.ColumnMappings.Add("IndividualPlaceOfBirth", "IndividualPlaceOfBirth");
                            bulkCopy.ColumnMappings.Add("IndividualDocument", "IndividualDocument");
                            bulkCopy.ColumnMappings.Add("HashCheck", "HashCheck");
                            bulkCopy.ColumnMappings.Add("CreatedDate", "CreatedDate");

                            bulkCopy.WriteToServer(dt);

                        }
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(con))
                        {
                            bulkCopy.DestinationTableName = "SanctionNameInfo";
                            bulkCopy.BatchSize = 5000;
                            bulkCopy.BulkCopyTimeout = 600;

                            bulkCopy.ColumnMappings.Add("DataId", "RefId");
                            bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
                            bulkCopy.ColumnMappings.Add("SecondName", "LastName");
                            bulkCopy.ColumnMappings.Add("ThirdName", "ThirdName");
                            bulkCopy.ColumnMappings.Add("FourthName", "FourthName");
                            bulkCopy.ColumnMappings.Add("Aliases", "Aliases");
                            bulkCopy.ColumnMappings.Add("SearchText", "SearchText");
                            bulkCopy.ColumnMappings.Add("SourceType", "SourceType");
                            bulkCopy.ColumnMappings.Add("CreatedDate", "Created");

                            bulkCopy.WriteToServer(dt);
                        }
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }

                }

                // ********** DELETE RECORDS NOT IN NEW LIST **********

                var incomingUids = new HashSet<int>(models.Select(x => x.DataId));

                var deleteIds = existingHashes.Keys
                                             .Where(DataId => !incomingUids.Contains(DataId))
                                             .ToList();

                if (deleteIds.Any())
                {

                    try
                    {
                        using (SqlCommand deleteCmd = new SqlCommand(
                                "DELETE FROM SanctionNameInfo WHERE SourceType='UN' AND RefId IN (" + string.Join(",", deleteIds) + "); DELETE FROM UNSanction WHERE DataId IN (" + string.Join(",", deleteIds) + ");", con))
                        {
                            deletedRows = Convert.ToInt32(deleteCmd.ExecuteScalar());
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("An error occurred while updating DELETE FROM SanctionNameInfo,UNSanction: " + e.ToString() + " ErroInfo----->>" + e.StackTrace.ToString());
                    }
                }
                aMLSourceLog.TotalNew = insert;
                aMLSourceLog.TotalUpdate = update;
                aMLSourceLog.TotalDelete = deleteIds.Count();
                aMLSourceLog.TotalData = models.Count();
                aMLSourceLog.SourceName = "UNSanction";
                aMLSourceLog.SourceCountry = "U.N.";
                aMLSourceLog.SourceLink = "https://scsanctions.un.org/resources/xml/en/consolidated.xml";
                aMLSourceLog.TotalPrivious = TotalDataCount();
                var res = _sanctionService.CreateAMLDataStatusLog(aMLSourceLog);


                return true;
            }


        }

        private DataTable CreateUNSanctionDataTable(List<IndividualModel> models)
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("DataId", typeof(int));
            dt.Columns.Add("VersionNum", typeof(string));
            dt.Columns.Add("FirstName", typeof(string));
            dt.Columns.Add("SecondName", typeof(string));
            dt.Columns.Add("ThirdName", typeof(string));
            dt.Columns.Add("FourthName", typeof(string));
            dt.Columns.Add("UnListType", typeof(string));
            dt.Columns.Add("ReferenceNumber", typeof(string));
            dt.Columns.Add("ListedOn", typeof(string));
            dt.Columns.Add("NameOriginalScript", typeof(string));
            dt.Columns.Add("Gender", typeof(string));
            dt.Columns.Add("DateOfBirthYear", typeof(string));
            dt.Columns.Add("ListType", typeof(string));
            dt.Columns.Add("Nationality", typeof(string));
            dt.Columns.Add("LastDayUpdated", typeof(string));
            dt.Columns.Add("Designation", typeof(string));
            dt.Columns.Add("Title", typeof(string));
            dt.Columns.Add("Address", typeof(string));
            dt.Columns.Add("Aliases", typeof(string));
            dt.Columns.Add("IndividualDateOfBirth", typeof(string));
            dt.Columns.Add("IndividualPlaceOfBirth", typeof(string));
            dt.Columns.Add("IndividualDocument", typeof(string));
            dt.Columns.Add("CreatedDate", typeof(DateTime));
            dt.Columns.Add("SearchText", typeof(string));
            dt.Columns.Add("HashCheck", typeof(string));
            dt.Columns.Add("SourceType", typeof(string));

            foreach (var model in models)
            {
                dt.Rows.Add(
                    model.DataId,
                    model.VersionNum,
                    model.FirstName,
                    model.SecondName,
                    model.ThirdName,
                    model.FourthName,
                    model.UnListType,
                    model.ReferenceNumber,
                    model.ListedOn,
                    model.NameOriginalScript,
                    model.Gender,
                    model.DateOfBirthYear,
                    model.ListType,
                    JsonSerializer.Serialize(model.Nationality),
                    JsonSerializer.Serialize(model.LastDayUpdated),
                    JsonSerializer.Serialize(model.Designation),
                    JsonSerializer.Serialize(model.Title),
                    JsonSerializer.Serialize(model.Address),
                    JsonSerializer.Serialize(model.Aliases),
                    JsonSerializer.Serialize(model.IndividualDateOfBirth),
                    JsonSerializer.Serialize(model.IndividualPlaceOfBirth),
                    JsonSerializer.Serialize(model.IndividualDocument),
                    DateTime.Now,
                    model.SearchText = model.FirstName + " " + model.SecondName + " " + model.ThirdName + " " + model.FourthName + " " +
                    string.Join(" ",
                      model.Aliases?
                    .Where(x => !string.IsNullOrWhiteSpace(x.AliasName))
                    .Select(x => x.AliasName) ?? Enumerable.Empty<string>()),
                    model.HashCheck,
                    "UNSanction"
                    );
            }

            return dt;
        }
        private int? TotalDataCount()
        {
            string Query = "select top 1 TotalPrivious,TotalDownload from AMLDataStatusLog where SourceName='UN' order by id desc";
            int? PrevData = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.CreateConnectionsql())
                using (SqlCommand cmd = new SqlCommand(Query, conn))
                {
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            PrevData = reader["TotalDownload"] == DBNull.Value
                                ? (int?)null
                                : Convert.ToInt32(reader["TotalDownload"]);
                        }
                    }
                }

                return PrevData;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        private object DbVal(object value)
        {
            return value ?? DBNull.Value;
        }
    }
}
