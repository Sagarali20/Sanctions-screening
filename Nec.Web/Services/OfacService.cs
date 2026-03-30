using Dapper;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Models.DTO;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;
using Id = Nec.Web.Models.Id;

namespace Nec.Web.Services
{
    public class OfacService: IOfacService
    {
        public IIDbConnection _dbConnection;
        public OfacService(IIDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public int  CreateOfacSanction(SdnEntry model)
        {

            int newRecordId = 0;
            string storedProcedureName = "InsertOfacSanction";
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

                        cmd.Parameters.AddWithValue("@Uid", model.Uid);
                        cmd.Parameters.AddWithValue("@FirstName", model.FirstName);
                        cmd.Parameters.AddWithValue("@LastName", model.LastName);
                        cmd.Parameters.AddWithValue("@Title", model.Title);
                        cmd.Parameters.AddWithValue("@SdnType", model.SdnType);
                        cmd.Parameters.AddWithValue("@EntityType", model.EntityType);
                        cmd.Parameters.AddWithValue("@Remarks", JsonSerializer.Serialize(model.Remarks));
                        cmd.Parameters.AddWithValue("@ProgramList", JsonSerializer.Serialize(model.ProgramList));
                        cmd.Parameters.AddWithValue("@AkaList", JsonSerializer.Serialize(model.AkaList));
                        cmd.Parameters.AddWithValue("@AddressList", JsonSerializer.Serialize(model.AddressList));
                        cmd.Parameters.AddWithValue("@IdList", JsonSerializer.Serialize(model.IdList));
                        cmd.Parameters.AddWithValue("@DateOfBirthList", JsonSerializer.Serialize(model.DateOfBirthList));
                        cmd.Parameters.AddWithValue("@PlaceOfBirthList", JsonSerializer.Serialize(model.PlaceOfBirthList));
                        cmd.Parameters.AddWithValue("@NationalityList", JsonSerializer.Serialize(model.NationalityList));
                        cmd.Parameters.AddWithValue("@VesselInfo", JsonSerializer.Serialize(model.VesselInfo));
                        cmd.Parameters.AddWithValue("@DataInfo", model.DataInfoType);
                        cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                        var result =  cmd.ExecuteScalarAsync().Result;

                        if (result != null && int.TryParse(result.ToString(), out int id))
                        {
                            newRecordId = id;
                        }

                        transaction.Commit();
                        if (transaction.Connection != null)
                        {
                            transaction.Connection.Close();
                        }

                        if (newRecordId > 0 )
                        {
                            SaveOfacName(model, newRecordId, model.DataInfoType);
                            return newRecordId;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return 0;
                }

            }
        }
        private bool SaveOfacName(SdnEntry model, int id,string sdntype)
        {

            string q = $"insert into SanctionNameInfo (FirstName,LastName,SourceType,RefId,Created) values('{model.FirstName?.Replace("'", "''") ?? null}','{model.LastName?.Replace("'", "''") ?? null}','Ofac-{sdntype}',{id},'{DateTime.Now}');";

            if (model.AkaList is not null && model.AkaList.Count > 0)
            {
                foreach (var item in model.AkaList)
                {
                    q += $"insert into SanctionNameInfo (FirstName,LastName,SourceType,RefId,Created) values('{item.FirstName?.Replace("'", "''") ?? null}','{item.LastName?.Replace("'", "''") ?? null}','Ofac-{sdntype}',{id},'{DateTime.Now}');";
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

        public bool CreateOfacRefDetails(string query)
        {

            try
            {
                using (SqlConnection con = _dbConnection.CreateConnectionsql())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(query, con))
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

        public async Task<List<OfacResponse?>> GetSearchSanction(OfacFilter model)
        {
            List<OfacResponse?> lst = new List<OfacResponse?>();

            try
            {
                using (SqlConnection conn = _dbConnection.CreateConnectionsql())
                using (SqlCommand cmd = new SqlCommand("GetAMLSourceByName", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@SearchName", model.SearchName);
                    cmd.Parameters.AddWithValue("@Type", model.Type);
                    cmd.Parameters.AddWithValue("@Id", model.Id);
                    cmd.Parameters.AddWithValue("@State", model.State);
                    cmd.Parameters.AddWithValue("@Program", model.Program);
                    cmd.Parameters.AddWithValue("@Address", model.Address);
                    cmd.Parameters.AddWithValue("@City", model.City);
                    cmd.Parameters.AddWithValue("@Country", model.Country);
                    cmd.Parameters.AddWithValue("@List", model.List);

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            OfacResponse searchResult = new OfacResponse
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString()!,
                                Address = string.Join(", ", JsonSerializer.Deserialize<List<string>>(reader["address"].ToString())?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()),
                                //EntityType = reader["EntityType"].ToString()!,
                                //SourceType = reader["SourceType"].ToString()!,
                                //SourceId = reader["SourceId"].ToString()!,
                            };
                            lst.Add(searchResult);
                        }
                    }
                }


            }
            catch (Exception ex)
            {
            }

            return lst;
        }

        public async Task<SdnEntry> GetSanctionDetailsById(int id)
        {
            SdnEntry sanctionEntity = new SdnEntry();


            try
            {
                string Query = "select * from OfacSanction where Id=" + id;

                using (var conn = _dbConnection.CreateConnectionsql())
                using (var cmd = new SqlCommand(Query, conn))
                {

                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            SdnEntry sanctionEntity2 = new SdnEntry
                            {
                                FirstName = reader["FirstName"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                SdnType = reader["SdnType"].ToString(),
                                Remarks = reader["Remarks"].ToString(),
                                ProgramList = JsonSerializer.Deserialize<List<string>>(reader["ProgramList"].ToString()),
                                AkaList = JsonSerializer.Deserialize<List<Aka>>(reader["AkaList"].ToString()),
                                AddressList = JsonSerializer.Deserialize<List<Address>>(reader["AddressList"].ToString()),
                                IdList = JsonSerializer.Deserialize<List<Id>>(reader["IdList"].ToString()),
                                DateOfBirthList = JsonSerializer.Deserialize<List<DateOfBirthItem>>(reader["DateOfBirthList"].ToString()),
                                PlaceOfBirthList = JsonSerializer.Deserialize<List<PlaceOfBirthItem>>(reader["PlaceOfBirthList"].ToString()),
                                NationalityList = JsonSerializer.Deserialize<List<Nationality>>(reader["PlaceOfBirthList"].ToString()),
                                VesselInfo = JsonSerializer.Deserialize<VesselInfo?>(reader["VesselInfo"].ToString()),
                                DataInfoType = reader["DataInfo"].ToString(),
                                //Titles = JsonSerializer.Deserialize<List<string>>(reader["titles"].ToString()),
                                //OtherInformation = JsonSerializer.Deserialize<List<string>>(reader["otherinformation"].ToString())
                            };

                            sanctionEntity = sanctionEntity2;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return sanctionEntity; ;
        }

        public int UpdateOfacSanctionSDN(List<SdnEntry> lst)
        {
            int insert = 0,update=0,delete=0;


            try
            {
                using (var connection = _dbConnection.CreateConnectionsql())
                {
                    connection.Open();

                    using (var deleteCmd = new SqlCommand(
                     "delete from SanctionNameInfo where SourceType='Ofac-SDN'", connection))
                    {
                        //deleteCmd.Parameters.AddWithValue("@Uids", ); // or Table-Valued Parameter
                        deleteCmd.ExecuteNonQuery();
                    }

                    foreach (var model in lst)
                    {
                        model.DataInfoType = "SDN";
                        // ********** CHECK IF EXISTS **********
                        int exists = 0;
                        using (var checkCmd = new SqlCommand("SELECT COUNT(1) FROM OfacSanction WHERE Uid = @Uid AND DataInfo='SDN'", connection))
                        {
                            checkCmd.Parameters.AddWithValue("@Uid", model.Uid);
                            exists = (int)checkCmd.ExecuteScalar();
                        }

                        if (exists > 0)
                        {
                            update++;
                            // ********** UPDATE **********
                            using (var cmd = new SqlCommand(@"
                                    UPDATE OfacSanction
                                    SET 
                                          FirstName        = @FirstName
                                        , LastName         = @LastName
                                        , Title            = @Title
                                        , SdnType          = @SdnType
                                        , EntityType       = @EntityType
                                        , Remarks          = @Remarks
                                        , ProgramList      = @ProgramList
                                        , AkaList          = @AkaList
                                        , AddressList      = @AddressList
                                        , IdList           = @IdList
                                        , DateOfBirthList  = @DateOfBirthList
                                        , PlaceOfBirthList = @PlaceOfBirthList
                                        , NationalityList  = @NationalityList
                                        , VesselInfo       = @VesselInfo
                                        , DataInfo         = @DataInfo
                                    WHERE Uid = @Uid AND DataInfo='SDN';
                                ", connection))
                            {
                                cmd.Parameters.AddWithValue("@Uid", DbVal(model.Uid));
                                cmd.Parameters.AddWithValue("@FirstName", DbVal(model.FirstName));
                                cmd.Parameters.AddWithValue("@LastName", DbVal(model.LastName));
                                cmd.Parameters.AddWithValue("@Title", DbVal(model.Title));
                                cmd.Parameters.AddWithValue("@SdnType", DbVal(model.SdnType));
                                cmd.Parameters.AddWithValue("@EntityType", DbVal(model.EntityType));
                                cmd.Parameters.AddWithValue("@Remarks", DbVal(JsonSerializer.Serialize(model.Remarks)));
                                cmd.Parameters.AddWithValue("@ProgramList", DbVal(JsonSerializer.Serialize(model.ProgramList)));
                                cmd.Parameters.AddWithValue("@AkaList", DbVal(JsonSerializer.Serialize(model.AkaList)));
                                cmd.Parameters.AddWithValue("@AddressList", DbVal(JsonSerializer.Serialize(model.AddressList)));
                                cmd.Parameters.AddWithValue("@IdList", DbVal(JsonSerializer.Serialize(model.IdList)));
                                cmd.Parameters.AddWithValue("@DateOfBirthList", DbVal(JsonSerializer.Serialize(model.DateOfBirthList)));
                                cmd.Parameters.AddWithValue("@PlaceOfBirthList", DbVal(JsonSerializer.Serialize(model.PlaceOfBirthList)));
                                cmd.Parameters.AddWithValue("@NationalityList", DbVal(JsonSerializer.Serialize(model.NationalityList)));
                                cmd.Parameters.AddWithValue("@VesselInfo", DbVal(JsonSerializer.Serialize(model.VesselInfo)));
                                cmd.Parameters.AddWithValue("@DataInfo", DbVal(model.DataInfoType));


                                cmd.ExecuteNonQuery();
                            }
                            SaveOfacName(model, model.Uid, model.DataInfoType);


                        }
                        else
                        {
                            insert++;
                            // ********** INSERT **********
                            int newRecordId = 0;
                            string storedProcedureName = "InsertOfacSanction";
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

                                        cmd.Parameters.AddWithValue("@Uid", model.Uid);
                                        cmd.Parameters.AddWithValue("@FirstName", model.FirstName);
                                        cmd.Parameters.AddWithValue("@LastName", model.LastName);
                                        cmd.Parameters.AddWithValue("@Title", model.Title);
                                        cmd.Parameters.AddWithValue("@SdnType", model.SdnType);
                                        cmd.Parameters.AddWithValue("@EntityType", model.EntityType);
                                        cmd.Parameters.AddWithValue("@Remarks", JsonSerializer.Serialize(model.Remarks));
                                        cmd.Parameters.AddWithValue("@ProgramList", JsonSerializer.Serialize(model.ProgramList));
                                        cmd.Parameters.AddWithValue("@AkaList", JsonSerializer.Serialize(model.AkaList));
                                        cmd.Parameters.AddWithValue("@AddressList", JsonSerializer.Serialize(model.AddressList));
                                        cmd.Parameters.AddWithValue("@IdList", JsonSerializer.Serialize(model.IdList));
                                        cmd.Parameters.AddWithValue("@DateOfBirthList", JsonSerializer.Serialize(model.DateOfBirthList));
                                        cmd.Parameters.AddWithValue("@PlaceOfBirthList", JsonSerializer.Serialize(model.PlaceOfBirthList));
                                        cmd.Parameters.AddWithValue("@NationalityList", JsonSerializer.Serialize(model.NationalityList));
                                        cmd.Parameters.AddWithValue("@VesselInfo", JsonSerializer.Serialize(model.VesselInfo));
                                        cmd.Parameters.AddWithValue("@DataInfo", model.DataInfoType);
                                        cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                                        var result = cmd.ExecuteScalarAsync().Result;

                                        if (result != null && int.TryParse(result.ToString(), out int id))
                                        {
                                            newRecordId = id;
                                        }

                                        transaction.Commit();
                                        if (transaction.Connection != null)
                                        {
                                            transaction.Connection.Close();
                                        }

                                        if (newRecordId > 0)
                                        {
                                            SaveOfacName(model, newRecordId, model.DataInfoType);
                                            return newRecordId;
                                        }
                                        else
                                        {
                                            return 0;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    return 0;
                                }

                            }
                        }
                    }

                    // ********** DELETE RECORDS NOT IN NEW LIST **********
                    var uids = lst.Select(x => x.Uid).ToList();
                    var idList = string.Join(",", uids);

                    using (var deleteCmd = new SqlCommand(
                        $"Delete from OfacSanction where Uid in (select distinct RefId from SanctionNameInfo where SourceType='Ofac-SDN')", connection))
                    {
                        deleteCmd.ExecuteNonQuery();
                    }

                }
            }
            catch (Exception e)
            {
                string m = e.Message;
               
            }

            return lst.Count; 

        }

        public int UpdateOfacSanctionNONSDN(List<SdnEntry> lst)
        {
            int insert = 0, update = 0, delete = 0;


            try
            {
                using (var connection = _dbConnection.CreateConnectionsql())
                {
                    connection.Open();

                    using (var deleteCmd = new SqlCommand(
                     "delete from SanctionNameInfo where SourceType='Ofac-NONSDN'", connection))
                    {
                        //deleteCmd.Parameters.AddWithValue("@Uids", ); // or Table-Valued Parameter
                        deleteCmd.ExecuteNonQuery();
                    }

                    foreach (var model in lst)
                    {
                        model.DataInfoType = "NONSDN";
                        // ********** CHECK IF EXISTS **********
                        int exists = 0;
                        using (var checkCmd = new SqlCommand("SELECT COUNT(1) FROM OfacSanction WHERE Uid = @Uid AND DataInfo='NONSDN'", connection))
                        {
                            checkCmd.Parameters.AddWithValue("@Uid", model.Uid);
                            exists = (int)checkCmd.ExecuteScalar();
                        }

                        if (exists > 0)
                        {
                            update++;
                            // ********** UPDATE **********
                            using (var cmd = new SqlCommand(@"
                                    UPDATE OfacSanction
                                    SET 
                                          FirstName        = @FirstName
                                        , LastName         = @LastName
                                        , Title            = @Title
                                        , SdnType          = @SdnType
                                        , EntityType       = @EntityType
                                        , Remarks          = @Remarks
                                        , ProgramList      = @ProgramList
                                        , AkaList          = @AkaList
                                        , AddressList      = @AddressList
                                        , IdList           = @IdList
                                        , DateOfBirthList  = @DateOfBirthList
                                        , PlaceOfBirthList = @PlaceOfBirthList
                                        , NationalityList  = @NationalityList
                                        , VesselInfo       = @VesselInfo
                                        , DataInfo         = @DataInfo
                                    WHERE Uid = @Uid AND DataInfo='SDN';
                                ", connection))
                            {
                                cmd.Parameters.AddWithValue("@Uid", DbVal(model.Uid));
                                cmd.Parameters.AddWithValue("@FirstName", DbVal(model.FirstName));
                                cmd.Parameters.AddWithValue("@LastName", DbVal(model.LastName));
                                cmd.Parameters.AddWithValue("@Title", DbVal(model.Title));
                                cmd.Parameters.AddWithValue("@SdnType", DbVal(model.SdnType));
                                cmd.Parameters.AddWithValue("@EntityType", DbVal(model.EntityType));
                                cmd.Parameters.AddWithValue("@Remarks", DbVal(JsonSerializer.Serialize(model.Remarks)));
                                cmd.Parameters.AddWithValue("@ProgramList", DbVal(JsonSerializer.Serialize(model.ProgramList)));
                                cmd.Parameters.AddWithValue("@AkaList", DbVal(JsonSerializer.Serialize(model.AkaList)));
                                cmd.Parameters.AddWithValue("@AddressList", DbVal(JsonSerializer.Serialize(model.AddressList)));
                                cmd.Parameters.AddWithValue("@IdList", DbVal(JsonSerializer.Serialize(model.IdList)));
                                cmd.Parameters.AddWithValue("@DateOfBirthList", DbVal(JsonSerializer.Serialize(model.DateOfBirthList)));
                                cmd.Parameters.AddWithValue("@PlaceOfBirthList", DbVal(JsonSerializer.Serialize(model.PlaceOfBirthList)));
                                cmd.Parameters.AddWithValue("@NationalityList", DbVal(JsonSerializer.Serialize(model.NationalityList)));
                                cmd.Parameters.AddWithValue("@VesselInfo", DbVal(JsonSerializer.Serialize(model.VesselInfo)));
                                cmd.Parameters.AddWithValue("@DataInfo", DbVal(model.DataInfoType));


                                cmd.ExecuteNonQuery();
                            }
                            SaveOfacName(model, model.Uid, model.DataInfoType);


                        }
                        else
                        {
                            insert++;
                            // ********** INSERT **********
                            int newRecordId = 0;
                            string storedProcedureName = "InsertOfacSanction";
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

                                        cmd.Parameters.AddWithValue("@Uid", model.Uid);
                                        cmd.Parameters.AddWithValue("@FirstName", model.FirstName);
                                        cmd.Parameters.AddWithValue("@LastName", model.LastName);
                                        cmd.Parameters.AddWithValue("@Title", model.Title);
                                        cmd.Parameters.AddWithValue("@SdnType", model.SdnType);
                                        cmd.Parameters.AddWithValue("@EntityType", model.EntityType);
                                        cmd.Parameters.AddWithValue("@Remarks", JsonSerializer.Serialize(model.Remarks));
                                        cmd.Parameters.AddWithValue("@ProgramList", JsonSerializer.Serialize(model.ProgramList));
                                        cmd.Parameters.AddWithValue("@AkaList", JsonSerializer.Serialize(model.AkaList));
                                        cmd.Parameters.AddWithValue("@AddressList", JsonSerializer.Serialize(model.AddressList));
                                        cmd.Parameters.AddWithValue("@IdList", JsonSerializer.Serialize(model.IdList));
                                        cmd.Parameters.AddWithValue("@DateOfBirthList", JsonSerializer.Serialize(model.DateOfBirthList));
                                        cmd.Parameters.AddWithValue("@PlaceOfBirthList", JsonSerializer.Serialize(model.PlaceOfBirthList));
                                        cmd.Parameters.AddWithValue("@NationalityList", JsonSerializer.Serialize(model.NationalityList));
                                        cmd.Parameters.AddWithValue("@VesselInfo", JsonSerializer.Serialize(model.VesselInfo));
                                        cmd.Parameters.AddWithValue("@DataInfo", model.DataInfoType);
                                        cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                                        var result = cmd.ExecuteScalarAsync().Result;

                                        if (result != null && int.TryParse(result.ToString(), out int id))
                                        {
                                            newRecordId = id;
                                        }

                                        transaction.Commit();
                                        if (transaction.Connection != null)
                                        {
                                            transaction.Connection.Close();
                                        }

                                        if (newRecordId > 0)
                                        {
                                            SaveOfacName(model, newRecordId, model.DataInfoType);
                                            return newRecordId;
                                        }
                                        else
                                        {
                                            return 0;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    return 0;
                                }

                            }
                        }
                    }

                    // ********** DELETE RECORDS NOT IN NEW LIST **********
                    var uids = lst.Select(x => x.Uid).ToList();
                    var idList = string.Join(",", uids);

                    using (var deleteCmd = new SqlCommand(
                        $"Delete from OfacSanction where Uid in (select distinct RefId from SanctionNameInfo where SourceType='Ofac-SDN')", connection))
                    {
                        deleteCmd.ExecuteNonQuery();
                    }

                }
            }
            catch (Exception e)
            {
                string m = e.Message;

            }

            return lst.Count;

        }
        private object DbVal(object value)
        {
            return value ?? DBNull.Value;
        }

        private string GenerateQueryAKA(SdnEntry sdnEntries, int Refid, string sdntype)
        {

            var sb = new StringBuilder();
            foreach (var res in sdnEntries.AkaList)
            {
                string type = res.Type?.Replace("'", "''") ?? "";
                string category = res.Category?.Replace("'", "''") ?? "";
                string firstName = res.FirstName?.Replace("'", "''") ?? "";
                string lastName = res.LastName?.Replace("'", "''") ?? "";
                sb.AppendFormat(
                    "INSERT INTO [dbo].[AkaInfo]([Uid],[Type],[Category],[FirstName],[LastName],[OfacId],[DataInfo],[CreatedDate]) " +
                    "VALUES ({0},'{1}','{2}','{3}','{4}',{5},'{6}','{7:yyyy-MM-dd HH:mm:ss}');",
                    res.Uid, type, category, firstName, lastName, Refid, sdntype, DateTime.Now
                );
            }
            return sb.ToString();
        }
        private string GenerateQueryAddress(SdnEntry sdnEntriesint, int Refid, string sdntype)
        {
            var sb = new StringBuilder();
            foreach (var res in sdnEntriesint.AddressList)
            {
                string address1 = res.Address1?.Replace("'", "''") ?? "";
                string city = res.City?.Replace("'", "''") ?? "";
                string postalCode = res.PostalCode?.Replace("'", "''") ?? "";
                string country = res.Country?.Replace("'", "''") ?? "";
                sb.AppendFormat(
                    "INSERT INTO [dbo].[AddressInfo] ([Uid],[Address1],[City],[PostalCode],[Country],[DataInfo],[OfacId],[CreatedDate]) " +
                    "VALUES ({0},'{1}','{2}','{3}','{4}','{5}',{6},'{7:yyyy-MM-dd HH:mm:ss}');",
                    res.Uid, address1, city, postalCode, country, sdntype, Refid, DateTime.Now
                );
            }
            return sb.ToString();
        }

    }
}
