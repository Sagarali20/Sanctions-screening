using DocumentFormat.OpenXml.EMMA;
using Nec.Web.Controllers;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Utils;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;

namespace Nec.Web.Services
{
    public class UKService : IUKService
    {
        public IIDbConnection _dbConnection;
        private readonly ILogger<UKService> _logger;
        private readonly ISanctionService _sanctionService;

        public UKService(IIDbConnection dbConnection, ILogger<UKService> logger, ISanctionService sanctionService)
        {
            _dbConnection = dbConnection;
            _logger = logger;
            _sanctionService = sanctionService;

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
        public bool BulkInsertUKSanction(List<Designation> models)
        {
            int insert = 0, update = 0, deletedRows = 0;

            AMLSourceLog aMLSourceLog = new AMLSourceLog();

            if (models == null || models.Count == 0)
                return true;

            using (SqlConnection con = _dbConnection.CreateConnectionsql())
            {
                con.Open();

                var existingHashes = new Dictionary<string, string>();

                using (var cmd = new SqlCommand(
                    "SELECT UniqueID, HashCheck FROM UKSanction",
                    con))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingHashes[reader["UniqueID"].ToString()] =
                                reader["HashCheck"].ToString();
                        }
                    }
                }
                List<Designation> newRecords = new List<Designation>();

                foreach (var model in models)
                {
                    string Check = HashHelper.ComputeSha256Hash(JsonSerializer.Serialize(model));
                    model.HashCheck = Check;

                    if (existingHashes.TryGetValue(model.UniqueID, out var dbHash))
                    {
                        if (dbHash != Check)
                        {
                            update++;
                            // ********** UPDATE **********
                            using (var cmd = new SqlCommand(@"
                                    UPDATE OfacSanction
                                    SET 
                                          LastUpdated        = @LastUpdated
                                        , DateDesignated   = @DateDesignated
                                        , UNReferenceNumber = @UNReferenceNumber
                                        , Names          = @Names
                                        , NonLatinNames       = @NonLatinNames
                                        , Titles          = @Titles
                                        , RegimeName      = @RegimeName
                                        , IndividualEntityShip          = @IndividualEntityShip
                                        , DesignationSource      = @DesignationSource
                                        , SanctionsImposed           = @SanctionsImposed
                                        , SanctionsImposedIndicators  = @SanctionsImposedIndicators
                                        , OtherInformation = @OtherInformation
                                        , UKStatementofReasons  = @UKStatementofReasons
                                        , IndividualDetails       = @IndividualDetails
                                      
                                    WHERE UniqueID = @UniqueID;
                                ", con))
                            {
                                cmd.Parameters.AddWithValue("@UniqueID", DbVal(model.UniqueID) ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@LastUpdated", DbVal(model.LastUpdated));
                                cmd.Parameters.AddWithValue("@DateDesignated", DbVal(model.DateDesignated));
                                cmd.Parameters.AddWithValue("@UNReferenceNumber", DbVal(model.UNReferenceNumber));
                                cmd.Parameters.AddWithValue("@Names", DbVal(model.Names));
                                cmd.Parameters.AddWithValue("@NonLatinNames", DbVal(model.NonLatinNames));
                                cmd.Parameters.AddWithValue("@Titles", DbVal(JsonSerializer.Serialize(model.Titles)));
                                cmd.Parameters.AddWithValue("@RegimeName", DbVal(JsonSerializer.Serialize(model.RegimeName)));
                                cmd.Parameters.AddWithValue("@IndividualEntityShip", DbVal(JsonSerializer.Serialize(model.IndividualEntityShip)));
                                cmd.Parameters.AddWithValue("@DesignationSource", DbVal(JsonSerializer.Serialize(model.DesignationSource)));
                                cmd.Parameters.AddWithValue("@SanctionsImposed", DbVal(JsonSerializer.Serialize(model.SanctionsImposed)));
                                cmd.Parameters.AddWithValue("@SanctionsImposedIndicators", DbVal(JsonSerializer.Serialize(model.SanctionsImposedIndicators)));
                                cmd.Parameters.AddWithValue("@OtherInformation", DbVal(JsonSerializer.Serialize(model.OtherInformation)));
                                cmd.Parameters.AddWithValue("@UKStatementofReasons", DbVal(JsonSerializer.Serialize(model.UKStatementofReasons)));
                                cmd.Parameters.AddWithValue("@IndividualDetails", DbVal(JsonSerializer.Serialize(model.IndividualDetails)));
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
                    DataTable dt = CreateUKSanctionDataTable(newRecords);

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(con))
                    {
                        bulkCopy.DestinationTableName = "UKSanction";
                        bulkCopy.BatchSize = 5000;
                        bulkCopy.BulkCopyTimeout = 600;

                        bulkCopy.ColumnMappings.Add("LastUpdated", "LastUpdated");
                        bulkCopy.ColumnMappings.Add("DateDesignated", "DateDesignated");
                        bulkCopy.ColumnMappings.Add("UniqueID", "UniqueID");
                        bulkCopy.ColumnMappings.Add("OFSIGroupID", "OFSIGroupID");
                        bulkCopy.ColumnMappings.Add("UNReferenceNumber", "UNReferenceNumber");
                        bulkCopy.ColumnMappings.Add("Names", "Names");
                        bulkCopy.ColumnMappings.Add("NonLatinNames", "NonLatinNames");
                        bulkCopy.ColumnMappings.Add("Titles", "Titles");
                        bulkCopy.ColumnMappings.Add("RegimeName", "RegimeName");
                        bulkCopy.ColumnMappings.Add("IndividualEntityShip", "IndividualEntityShip");
                        bulkCopy.ColumnMappings.Add("DesignationSource", "DesignationSource");
                        bulkCopy.ColumnMappings.Add("SanctionsImposed", "SanctionsImposed");
                        bulkCopy.ColumnMappings.Add("SanctionsImposedIndicators", "SanctionsImposedIndicators");
                        bulkCopy.ColumnMappings.Add("OtherInformation", "OtherInformation");
                        bulkCopy.ColumnMappings.Add("UKStatementofReasons", "UKStatementofReasons");
                        bulkCopy.ColumnMappings.Add("IndividualDetails", "IndividualDetails");
                        bulkCopy.ColumnMappings.Add("HashCheck", "HashCheck");
                        bulkCopy.WriteToServer(dt);

                    }


                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(con))
                    {
                        bulkCopy.DestinationTableName = "SanctionNameInfo";
                        bulkCopy.BatchSize = 5000;
                        bulkCopy.BulkCopyTimeout = 600;

                        bulkCopy.ColumnMappings.Add("UniqueID", "RefId2");
                        //bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
                        //bulkCopy.ColumnMappings.Add("LastName", "LastName");
                        //bulkCopy.ColumnMappings.Add(null, "ThirdName");
                        //bulkCopy.ColumnMappings.Add(null, "FourthName");
                        //bulkCopy.ColumnMappings.Add(null, "Aliases");
                        bulkCopy.ColumnMappings.Add("SearchText", "SearchText");
                        bulkCopy.ColumnMappings.Add("SourceType", "SourceType");

                        bulkCopy.ColumnMappings.Add("CreatedDate", "Created");

                        bulkCopy.WriteToServer(dt);
                    }

                }

                // ********** DELETE RECORDS NOT IN NEW LIST **********

                var incomingUids = new HashSet<string>(models.Select(x => x.UniqueID));

                var deleteIds = existingHashes.Keys
                                             .Where(UniqueID => !incomingUids.Contains(UniqueID))
                                             .ToList();

                if (deleteIds.Any())
                {

                    try
                    {
                        using (SqlCommand deleteCmd = new SqlCommand(
                        "DELETE FROM SanctionNameInfo WHERE SourceType='UKSanction' AND refid2 IN (" +
                        string.Join(",", deleteIds.Select(id => $"'{id.Replace("'", "''")}'")) +
                        "); " +
                        "DELETE FROM UKSanction WHERE UniqueID IN (" +
                        string.Join(",", deleteIds.Select(id => $"'{id.Replace("'", "''")}'")) +
                        ");", con))
                            {
                                deletedRows = Convert.ToInt32(deleteCmd.ExecuteScalar());
                            }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("An error occurred while updating DELETE FROM SanctionNameInfo,UKSanction: " + e.ToString() + " ErroInfo----->>" + e.StackTrace.ToString());
                    }
                }
                aMLSourceLog.TotalNew = insert;
                aMLSourceLog.TotalUpdate = update;
                aMLSourceLog.TotalDelete =deleteIds.Count();
                aMLSourceLog.TotalData = models.Count();
                aMLSourceLog.SourceName = "UKSanction";
                aMLSourceLog.SourceCountry = "U.K.";
                aMLSourceLog.SourceLink = "https://sanctionslist.fcdo.gov.uk/docs/UK-Sanctions-List.xml";
                aMLSourceLog.TotalPrivious = TotalDataCount();
                var res = _sanctionService.CreateAMLDataStatusLog(aMLSourceLog);


                return true;
            }
        }

        private DataTable CreateUKSanctionDataTable(List<Designation> models)
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("LastUpdated", typeof(string));
            dt.Columns.Add("DateDesignated", typeof(string));
            dt.Columns.Add("UniqueID", typeof(string));
            dt.Columns.Add("OFSIGroupID", typeof(string));
            dt.Columns.Add("UNReferenceNumber", typeof(string));
            dt.Columns.Add("Names", typeof(string));
            dt.Columns.Add("NonLatinNames", typeof(string));
            dt.Columns.Add("Titles", typeof(string));
            dt.Columns.Add("RegimeName", typeof(string));
            dt.Columns.Add("IndividualEntityShip", typeof(string));
            dt.Columns.Add("DesignationSource", typeof(string));
            dt.Columns.Add("SanctionsImposed", typeof(string));
            dt.Columns.Add("SanctionsImposedIndicators", typeof(string));
            dt.Columns.Add("OtherInformation", typeof(string));
            dt.Columns.Add("UKStatementofReasons", typeof(string));
            dt.Columns.Add("IndividualDetails", typeof(string));
            dt.Columns.Add("CreatedDate");
            dt.Columns.Add("SearchText");
            dt.Columns.Add("SourceType");
            dt.Columns.Add("RefId2", typeof(string));
            dt.Columns.Add("HashCheck", typeof(string));


            foreach (var model in models)
            {
                dt.Rows.Add(
                    model.LastUpdated,
                    model.DateDesignated,
                    model.UniqueID,
                    model.OFSIGroupID,
                    model.UNReferenceNumber,
                    JsonSerializer.Serialize(model.Names),
                    JsonSerializer.Serialize(model.NonLatinNames),
                    JsonSerializer.Serialize(model.Titles),
                    model.RegimeName,
                    model.IndividualEntityShip,
                    model.DesignationSource,
                    model.SanctionsImposed,
                    JsonSerializer.Serialize(model.SanctionsImposedIndicators),
                    model.OtherInformation,
                    model.UKStatementofReasons,
                    JsonSerializer.Serialize(model.IndividualDetails),
                    DateTime.Now,
                    string.Join(" | ",
                    model.Names.NameList.Select(x =>
                        string.Join(",",
                            new[] { x.Name1, x.Name2, x.Name6 }
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                        )
                    )),
                "UKSanction",
                model.UniqueID,
                model.HashCheck
                );
            }
            return dt;
        }

        public int CreateUKSanctionBulk(Designations model)
        {
            BulkInsertUKSanction(model.DesignationList);
            return 1;
        }
        private int? TotalDataCount()
        {
            string Query = "select top 1 TotalPrivious,TotalDownload from AMLDataStatusLog where SourceName='UKSanction' order by id desc";
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
