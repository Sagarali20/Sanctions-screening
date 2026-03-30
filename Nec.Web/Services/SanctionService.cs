using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Nec.Web.Config;
using Nec.Web.Helpers;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Npgsql;
using NPOI.SS.Formula.Functions;
using Raffinert.FuzzySharp;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Nec.Web.Services
{  
    public class SanctionService : ISanctionService
    {
        public IIDbConnection _dbConnection;
        private readonly ILogger<SanctionService> _logger;

        public SanctionService(IIDbConnection dbConnection, ILogger<SanctionService> logger )
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }
        public bool CreateSanction(SanctionEntity model)
        {
            int resultStatus;
            string storedProcedureName = "SaveAMLSource";
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
                        cmd.Parameters.AddWithValue("@AmlId", model.id);
                        cmd.Parameters.AddWithValue("@SourceId", model.source_id);
                        cmd.Parameters.AddWithValue("@SourceType", model.source_type);
                        cmd.Parameters.AddWithValue("@PepType", model.pep_type);
                        cmd.Parameters.AddWithValue("@EntityType", model.entity_type);
                        cmd.Parameters.AddWithValue("@Gender", model.gender);
                        cmd.Parameters.AddWithValue("@Name", model.name);
                        cmd.Parameters.AddWithValue("@TlName", model.tl_name);
                        cmd.Parameters.AddWithValue("@Alias_names", JsonSerializer.Serialize(model.alias_names));
                        cmd.Parameters.AddWithValue("@LastNames", JsonSerializer.Serialize(model.last_names));
                        cmd.Parameters.AddWithValue("@GivenNames", JsonSerializer.Serialize(model.given_names));
                        cmd.Parameters.AddWithValue("@AliasGivenNames", JsonSerializer.Serialize(model.alias_given_names));
                        cmd.Parameters.AddWithValue("@Spouse", JsonSerializer.Serialize(model.spouse));
                        cmd.Parameters.AddWithValue("@Parents", JsonSerializer.Serialize(model.parents));
                        cmd.Parameters.AddWithValue("@Children", JsonSerializer.Serialize(model.children));
                        cmd.Parameters.AddWithValue("@Siblings", JsonSerializer.Serialize(model.siblings));
                        cmd.Parameters.AddWithValue("@DateOfBirth", JsonSerializer.Serialize(model.date_of_birth));
                        cmd.Parameters.AddWithValue("@PlaceOfBirth", JsonSerializer.Serialize(model.place_of_birth));
                        cmd.Parameters.AddWithValue("@DateOfBirthRemarks", JsonSerializer.Serialize(model.date_of_birth_remarks));
                        cmd.Parameters.AddWithValue("@PlaceOfBirthRemarks", JsonSerializer.Serialize(model.place_of_birth_remarks));
                        cmd.Parameters.AddWithValue("@Address", JsonSerializer.Serialize(model.address));
                        cmd.Parameters.AddWithValue("@AddressRemarks", JsonSerializer.Serialize(model.address_remarks));
                        cmd.Parameters.AddWithValue("@SanctionDetails", JsonSerializer.Serialize(model.sanction_details));
                        cmd.Parameters.AddWithValue("@Description", JsonSerializer.Serialize(model.description));
                        cmd.Parameters.AddWithValue("@Occupations", JsonSerializer.Serialize(model.occupations));
                        cmd.Parameters.AddWithValue("@Positions", JsonSerializer.Serialize(model.positions));
                        cmd.Parameters.AddWithValue("@PoliticalParties", JsonSerializer.Serialize(model.political_parties));
                        cmd.Parameters.AddWithValue("@Links", JsonSerializer.Serialize(model.links));
                        cmd.Parameters.AddWithValue("@Titles", JsonSerializer.Serialize(model.titles));
                        cmd.Parameters.AddWithValue("@ListDate", model.list_date);
                        cmd.Parameters.AddWithValue("@Functions", JsonSerializer.Serialize(model.functions));
                        cmd.Parameters.AddWithValue("@Citizenship", JsonSerializer.Serialize(model.citizenship));
                        cmd.Parameters.AddWithValue("@CitizenshipRemarks", JsonSerializer.Serialize(model.citizenship_remarks));
                        cmd.Parameters.AddWithValue("@OtherInformation", JsonSerializer.Serialize(model.other_information));
                        cmd.Parameters.AddWithValue("@CompanyNumber", JsonSerializer.Serialize(model.company_number));
                        cmd.Parameters.AddWithValue("@NameRemarks", JsonSerializer.Serialize(model.name_remarks));
                        cmd.Parameters.AddWithValue("@Jurisdiction", JsonSerializer.Serialize(model.jurisdiction));
                        cmd.Parameters.AddWithValue("@SourceCountry", model.source_country);

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
                        cmd.ExecuteNonQuery(); 
                        resultStatus = (int)cmd.Parameters["@ResultStatus"].Value;
                        string? errorMessage = cmd.Parameters["@ErrorMessage"].Value?.ToString();

                        transaction.Commit();
                        if (transaction.Connection != null)
                        {
                            transaction.Connection.Close();
                        }

                        if (resultStatus == 1)
                        {
                          //  SaveAMLName(model,6);

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
                    _logger.LogError("Getting error from SaveAMLSource" + ex.Message);
                    transaction.Rollback();
                    return false;
                }
            }
            
        }

        private bool SaveAMLName(SanctionEntity model, int AmlRefid)
        {

            string q = $"insert into AMLNameInfo (Name,AmlId,AmlRefid,CreatedDate) values('{model.name?.Replace("'", "''") ?? null}','{model.id?.Replace("'", "''") ?? null}',{AmlRefid},'{DateTime.Now}');";

            if (model.alias_names is not null  && model.alias_names.Count > 0)
            {
                foreach (var item in model.alias_names)
                {
                    if(item !="nan")
                     q += $"insert into AMLNameInfo (AliasName,AmlId,AmlRefid,CreatedDate) values(N'{item?.Replace("'", "''") ?? null}','{model.id?.Replace("'", "''") ?? null}',{AmlRefid},'{DateTime.Now}');";
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

        public bool CreateSanctionNew(SanctionEntity model)
        {
            int resultStatus;
            string storedProcedureName = "SaveAMLSourceUpdated";
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

                        cmd.Parameters.AddWithValue("@AmlId", model.id);
                        cmd.Parameters.AddWithValue("@SourceId", model.source_id);
                        cmd.Parameters.AddWithValue("@SourceType", model.source_type);
                        cmd.Parameters.AddWithValue("@PepType", model.pep_type);
                        cmd.Parameters.AddWithValue("@EntityType", model.entity_type);
                        cmd.Parameters.AddWithValue("@Gender", model.gender);
                        cmd.Parameters.AddWithValue("@Name", model.name);
                        cmd.Parameters.AddWithValue("@TlName", model.tl_name);
                        cmd.Parameters.AddWithValue("@Alias_names", JsonSerializer.Serialize(model.alias_names));
                        cmd.Parameters.AddWithValue("@LastNames", JsonSerializer.Serialize(model.last_names));
                        cmd.Parameters.AddWithValue("@GivenNames", JsonSerializer.Serialize(model.given_names));
                        cmd.Parameters.AddWithValue("@AliasGivenNames", JsonSerializer.Serialize(model.alias_given_names));
                        cmd.Parameters.AddWithValue("@Spouse", JsonSerializer.Serialize(model.spouse));
                        cmd.Parameters.AddWithValue("@Parents", JsonSerializer.Serialize(model.parents));
                        cmd.Parameters.AddWithValue("@Children", JsonSerializer.Serialize(model.children));
                        cmd.Parameters.AddWithValue("@Siblings", JsonSerializer.Serialize(model.siblings));
                        cmd.Parameters.AddWithValue("@DateOfBirth", JsonSerializer.Serialize(model.date_of_birth));
                        cmd.Parameters.AddWithValue("@PlaceOfBirth", JsonSerializer.Serialize(model.place_of_birth));
                        cmd.Parameters.AddWithValue("@DateOfBirthRemarks", JsonSerializer.Serialize(model.date_of_birth_remarks));
                        cmd.Parameters.AddWithValue("@PlaceOfBirthRemarks", JsonSerializer.Serialize(model.place_of_birth_remarks));
                        cmd.Parameters.AddWithValue("@Address", JsonSerializer.Serialize(model.address));
                        cmd.Parameters.AddWithValue("@AddressRemarks", JsonSerializer.Serialize(model.address_remarks));
                        cmd.Parameters.AddWithValue("@SanctionDetails", JsonSerializer.Serialize(model.sanction_details));
                        cmd.Parameters.AddWithValue("@Description", JsonSerializer.Serialize(model.description));
                        cmd.Parameters.AddWithValue("@Occupations", JsonSerializer.Serialize(model.occupations));
                        cmd.Parameters.AddWithValue("@Positions", JsonSerializer.Serialize(model.positions));
                        cmd.Parameters.AddWithValue("@PoliticalParties", JsonSerializer.Serialize(model.political_parties));
                        cmd.Parameters.AddWithValue("@Links", JsonSerializer.Serialize(model.links));
                        cmd.Parameters.AddWithValue("@Titles", JsonSerializer.Serialize(model.titles));
                        cmd.Parameters.AddWithValue("@ListDate", model.list_date);
                        cmd.Parameters.AddWithValue("@Functions", JsonSerializer.Serialize(model.functions));
                        cmd.Parameters.AddWithValue("@Citizenship", JsonSerializer.Serialize(model.citizenship));
                        cmd.Parameters.AddWithValue("@CitizenshipRemarks", JsonSerializer.Serialize(model.citizenship_remarks));
                        cmd.Parameters.AddWithValue("@OtherInformation", JsonSerializer.Serialize(model.other_information));
                        cmd.Parameters.AddWithValue("@CompanyNumber", JsonSerializer.Serialize(model.company_number));
                        cmd.Parameters.AddWithValue("@NameRemarks", JsonSerializer.Serialize(model.name_remarks));
                        cmd.Parameters.AddWithValue("@Jurisdiction", JsonSerializer.Serialize(model.jurisdiction));
                        cmd.Parameters.AddWithValue("@SourceCountry", model.source_country);
                        cmd.Parameters.AddWithValue("@VersionId", model.VersionId);


                        SqlParameter outParameter = new SqlParameter("@ResultStatus", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outParameter);
                        // Output: ErrorMessage
                        SqlParameter outErrorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, -1)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outErrorParam);
                        cmd.ExecuteNonQuery();
                        resultStatus = (int)cmd.Parameters["@ResultStatus"].Value;
                        string? errorMessage = cmd.Parameters["@ErrorMessage"].Value?.ToString();

                        transaction.Commit();
                        if (transaction.Connection != null)
                        {
                            transaction.Connection.Close();
                        }

                        if (resultStatus == 1)
                        {
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
                    _logger.LogError("Getting error from SaveAMLSourceUpdated" + ex.Message);

                    transaction.Rollback();
                    return false;
                }
            }

        }
        public bool UpdateSanction(SanctionEntity model)
        {
            int resultStatus;
            string storedProcedureName = "UpdateAMLSource";
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
                        cmd.Parameters.AddWithValue("@AmlId", model.id);
                        cmd.Parameters.AddWithValue("@SourceId", model.source_id);
                        cmd.Parameters.AddWithValue("@SourceType", model.source_type);
                        cmd.Parameters.AddWithValue("@PepType", model.pep_type);
                        cmd.Parameters.AddWithValue("@EntityType", model.entity_type);
                        cmd.Parameters.AddWithValue("@Gender", model.gender);
                        cmd.Parameters.AddWithValue("@Name", model.name);
                        cmd.Parameters.AddWithValue("@TlName", model.tl_name);
                        cmd.Parameters.AddWithValue("@Alias_names", JsonSerializer.Serialize(model.alias_names));
                        cmd.Parameters.AddWithValue("@LastNames", JsonSerializer.Serialize(model.last_names));
                        cmd.Parameters.AddWithValue("@GivenNames", JsonSerializer.Serialize(model.given_names));
                        cmd.Parameters.AddWithValue("@AliasGivenNames", JsonSerializer.Serialize(model.alias_given_names));
                        cmd.Parameters.AddWithValue("@Spouse", JsonSerializer.Serialize(model.spouse));
                        cmd.Parameters.AddWithValue("@Parents", JsonSerializer.Serialize(model.parents));
                        cmd.Parameters.AddWithValue("@Children", JsonSerializer.Serialize(model.children));
                        cmd.Parameters.AddWithValue("@Siblings", JsonSerializer.Serialize(model.siblings));
                        cmd.Parameters.AddWithValue("@DateOfBirth", JsonSerializer.Serialize(model.date_of_birth));
                        cmd.Parameters.AddWithValue("@PlaceOfBirth", JsonSerializer.Serialize(model.place_of_birth));
                        cmd.Parameters.AddWithValue("@DateOfBirthRemarks", JsonSerializer.Serialize(model.date_of_birth_remarks));
                        cmd.Parameters.AddWithValue("@PlaceOfBirthRemarks", JsonSerializer.Serialize(model.place_of_birth_remarks));
                        cmd.Parameters.AddWithValue("@Address", JsonSerializer.Serialize(model.address));
                        cmd.Parameters.AddWithValue("@AddressRemarks", JsonSerializer.Serialize(model.address_remarks));
                        cmd.Parameters.AddWithValue("@SanctionDetails", JsonSerializer.Serialize(model.sanction_details));
                        cmd.Parameters.AddWithValue("@Description", JsonSerializer.Serialize(model.description));
                        cmd.Parameters.AddWithValue("@Occupations", JsonSerializer.Serialize(model.occupations));
                        cmd.Parameters.AddWithValue("@Positions", JsonSerializer.Serialize(model.positions));
                        cmd.Parameters.AddWithValue("@PoliticalParties", JsonSerializer.Serialize(model.political_parties));
                        cmd.Parameters.AddWithValue("@Links", JsonSerializer.Serialize(model.links));
                        cmd.Parameters.AddWithValue("@Titles", JsonSerializer.Serialize(model.titles));
                        cmd.Parameters.AddWithValue("@ListDate", model.list_date);
                        cmd.Parameters.AddWithValue("@Functions", JsonSerializer.Serialize(model.functions));
                        cmd.Parameters.AddWithValue("@Citizenship", JsonSerializer.Serialize(model.citizenship));
                        cmd.Parameters.AddWithValue("@CitizenshipRemarks", JsonSerializer.Serialize(model.citizenship_remarks));
                        cmd.Parameters.AddWithValue("@OtherInformation", JsonSerializer.Serialize(model.other_information));
                        cmd.Parameters.AddWithValue("@CompanyNumber", JsonSerializer.Serialize(model.company_number));
                        cmd.Parameters.AddWithValue("@NameRemarks", JsonSerializer.Serialize(model.name_remarks));
                        cmd.Parameters.AddWithValue("@Jurisdiction", JsonSerializer.Serialize(model.jurisdiction));
                        cmd.Parameters.AddWithValue("@SourceCountry", model.source_country);
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
                        cmd.ExecuteNonQuery();
                        resultStatus = (int)cmd.Parameters["@ResultStatus"].Value;
                        string? errorMessage = cmd.Parameters["@ErrorMessage"].Value?.ToString();

                        transaction.Commit();
                        if (transaction.Connection != null)
                        {
                            transaction.Connection.Close();
                        }

                        if (resultStatus == 1)
                        {

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
                    _logger.LogError("Getting error from UpdateAMLSource" + ex.Message);

                    transaction.Rollback();
                    return false;
                }
            }

        }
        public bool DeleteSanction(string id)
        {
            int resultStatus;
            string storedProcedureName = "DeleteAMLSource";
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
                        cmd.Parameters.AddWithValue("@AmlId", id);

                        SqlParameter outParameter = new SqlParameter("@ResultStatus", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outParameter);
                        cmd.ExecuteNonQuery();
                        resultStatus = (int)cmd.Parameters["@ResultStatus"].Value;
                        transaction.Commit();
                        if (transaction.Connection != null)
                        {
                            transaction.Connection.Close();
                        }

                        if (resultStatus == 1)
                        {

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
                    _logger.LogError("Getting error from DeleteAMLSource" + ex.Message);

                    transaction.Rollback();
                    return false;
                }
            }

        }  

        public async Task<List<SanctionEntity>> GetExcelSanctionDetailsBySearch(string name, string entitytype, string address, string city, string state, string country, string dateofbirth,string guid)
        {
            List<SanctionEntity> sanctionEntities = new List<SanctionEntity>();
            string Id = string.Empty;
            try
            {             
                using (SqlConnection conn = _dbConnection.CreateConnectionsql())
                using (SqlCommand cmd = new SqlCommand("GetAMLSourceAdvancedSearch", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@EntityType", entitytype);
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Address", address);
                    cmd.Parameters.AddWithValue("@City", city);
                    cmd.Parameters.AddWithValue("@State", state);
                    cmd.Parameters.AddWithValue("@Country", country);
                    cmd.Parameters.AddWithValue("@DateOfBirth", dateofbirth);
                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            SanctionEntity sanctionEntity = new SanctionEntity();
                            sanctionEntity.Guid = guid;
                            sanctionEntity.id = reader["Id"].ToString();
                            sanctionEntity.AmlId = reader["AmlId"].ToString();
                            sanctionEntity.source_id = reader["SourceId"].ToString();
                            sanctionEntity.source_type = reader["SourceType"].ToString();
                            sanctionEntity.pep_type = reader["PepType"].ToString();
                            sanctionEntity.entity_type = reader["EntityType"].ToString();
                            sanctionEntity.gender = reader["Gender"].ToString();
                            sanctionEntity.name = reader["Name"].ToString();
                            sanctionEntity.tl_name = reader["TlName"].ToString();
                            sanctionEntity.alias_names = JsonSerializer.Deserialize<List<string>>(reader["Alias_names"].ToString());
                            sanctionEntity.last_names = JsonSerializer.Deserialize<List<string>>(reader["LastNames"].ToString());
                            sanctionEntity.given_names = JsonSerializer.Deserialize<List<string>>(reader["GivenNames"].ToString());
                            sanctionEntity.alias_given_names = JsonSerializer.Deserialize<List<string>>(reader["AliasGivenNames"].ToString());
                            sanctionEntity.parents = JsonSerializer.Deserialize<List<string>>(reader["Parents"].ToString());
                            sanctionEntity.date_of_birth = JsonSerializer.Deserialize<List<string>>(reader["DateOfBirth"].ToString());
                            sanctionEntity.address = JsonSerializer.Deserialize<List<string>>(reader["Address"].ToString());
                            sanctionEntity.address_remarks = JsonSerializer.Deserialize<List<string>>(reader["AddressRemarks"].ToString());
                            sanctionEntity.sanction_details = JsonSerializer.Deserialize<List<string>>(reader["SanctionDetails"].ToString());
                            sanctionEntity.description = JsonSerializer.Deserialize<List<string>>(reader["Description"].ToString());
                            sanctionEntity.occupations = JsonSerializer.Deserialize<List<string>>(reader["Occupations"].ToString());
                            sanctionEntity.positions = JsonSerializer.Deserialize<List<string>>(reader["Positions"].ToString());
                            sanctionEntity.political_parties = JsonSerializer.Deserialize<List<string>>(reader["PoliticalParties"].ToString());                  
                            sanctionEntity.links = JsonSerializer.Deserialize<List<string>>(reader["Links"].ToString());
                            sanctionEntity.functions = JsonSerializer.Deserialize<List<string>>(reader["Functions"].ToString());
                            sanctionEntity.citizenship = JsonSerializer.Deserialize<List<string>>(reader["Citizenship"].ToString());
                            sanctionEntity.titles = JsonSerializer.Deserialize<List<string>>(reader["Titles"].ToString());
                            sanctionEntity.other_information = JsonSerializer.Deserialize<List<string>>(reader["OtherInformation"].ToString());
                            sanctionEntities.Add(sanctionEntity);
                        }   
                    }    
                }              
            }
            catch (Exception ex)
            {

            }
            return  sanctionEntities;    

        }

        public async Task<List<SanctionEntity?>> GetSearchSanctionIndividual2(AMLFilter model)
        {

            string id = "";
            List<SanctionEntity> sanctionEntities = new List<SanctionEntity>();
            try
            {
                using (SqlConnection conn = _dbConnection.CreateConnectionsql())
                using (SqlCommand cmd = new SqlCommand("GetAMLSourceByIndividual", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@SearchName", model.Name);
                    cmd.Parameters.AddWithValue("@SourceId", model.SourceId);
                    cmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth);
                    cmd.Parameters.AddWithValue("@Gender", model.Gender);
                    cmd.Parameters.AddWithValue("@IsFuzzy", model.IsFuzzy);

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        Func<string, List<string>> getList = col =>
                        reader[col] != DBNull.Value && !string.IsNullOrWhiteSpace(reader[col]?.ToString())
                            ? JsonSerializer.Deserialize<List<string>>(reader[col].ToString())
                            : null;

                        while (await reader.ReadAsync())
                        {
                          //  SanctionEntity sanctionEntity = new SanctionEntity();

                            var sanctionEntity = new SanctionEntity
                            {
                                AmlId= reader["AmlId"]?.ToString()!,
                                source_id = reader["SourceId"]?.ToString()!,
                                source_type = reader["SourceType"]?.ToString()!,
                                pep_type = reader["PepType"] == DBNull.Value
                                                                ? null
                                                                : reader["ListDate"].ToString(),
                                entity_type = reader["EntityType"]?.ToString(),
                                gender = reader["Gender"]?.ToString(),
                                name = reader["Name"]?.ToString(),
                                alias_names = getList("Alias_names"),
                                last_names = getList("LastNames"),
                                name_remarks = getList("NameRemarks"),
                                given_names = getList("GivenNames"),
                                alias_given_names = getList("AliasGivenNames"),
                                spouse = getList("Spouse"),
                                parents = getList("Parents"),
                              //  children = getList("Children"),
                              //  siblings = getList("Siblings"),
                                date_of_birth = getList("DateOfBirth"),
                                date_of_birth_remarks = getList("DateOfBirthRemarks"),
                                address = getList("Address"),
                                address_remarks = getList("AddressRemarks"),
                                sanction_details = getList("SanctionDetails"),
                                functions = getList("Functions"),
                                description = getList("Description"),
                                occupations = getList("Occupations"),
                                positions = getList("Positions"),
                                political_parties = getList("PoliticalParties"),
                                citizenship = getList("Citizenship"),
                                citizenship_remarks = getList("CitizenshipRemarks"),
                               // titles = getList("Titles"),
                                other_information = getList("OtherInformation"),
                                list_date = reader["ListDate"] == DBNull.Value
                                                                ? (DateTime?)null
                                                                : Convert.ToDateTime(reader["ListDate"]),
                                links = getList("Links"),
                                place_of_birth = getList("PlaceOfBirth")
                            };
                            sanctionEntities.Add(sanctionEntity);
                        }
                    }
                }

            }
            catch (Exception ex)
            {

            }

            string Payload = JsonSerializer.Serialize<AMLFilter>(model);

            if(model.ScreeningId is not null)
            {
                if (model.ScreeningType is null) {

                    model.ScreeningType = "transaction";
                }

               string Query = string.Format("Insert into APIRequestLog values({0},'{1}','{2}','{3}')", model.ScreeningId, model.ScreeningType, Payload, DateTimeOffset.Now);

                using (SqlConnection con = _dbConnection.CreateConnectionsql())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(Query, con))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                           
                        }

                    }
                }

            }
            return sanctionEntities;
        }

        //*** For core Implementation **
        public async Task<List<SanctionEntity?>> GetSearchSanctionIndividual(AMLFilter model)
        {

            string Query = string.Empty;
            string SubQuery = string.Empty;

            model.Name = TokenizeWithoutStopWords(model.Name);

            if (!string.IsNullOrWhiteSpace(model.DateOfBirth))
            {
                SubQuery += string.Format(@" AND (DateOfBirth LIKE '%{0}%' OR DateOfBirth = 'null')",model.DateOfBirth);
            }
            if (!string.IsNullOrWhiteSpace(model.Gender))
            {
                SubQuery += string.Format(@" AND (Gender='{0}' OR Gender='UNKNOWN')", model.Gender);
            }
                Query = string.Format(@"
                                    SELECT Top 50
                                    AmlId,SourceId,SourceType,EntityType,Gender,[Name],Alias_names,DateOfBirth,OtherInformation,ListDate,PlaceOfBirth,'NonSoundex' as [Type]
                                    FROM AMLSource
                                    WHERE CONTAINS(([Name], Alias_names), '""{0}""') {1}
                                    union all
                                    SELECT TOP (1000)
                                        aml.AmlId,
                                        aml.SourceId,
                                        aml.SourceType,
                                        aml.EntityType,
                                        aml.Gender,
                                        aml.[Name],
                                        aml.Alias_names,
                                        aml.DateOfBirth,
                                        aml.OtherInformation,
                                        aml.ListDate,
                                        aml.PlaceOfBirth,
                                        'Soundex' AS [Type]
                                    FROM AMLSource aml
                                    WHERE SOUNDEX([Name]) = SOUNDEX('{0}')
                                    AND NOT EXISTS
                                    (
                                        SELECT 1
                                        FROM AMLSource s
                                        WHERE s.AmlId = aml.AmlId
                                        AND CONTAINS(([Name], Alias_names), '""{0}""') 
                                    ) {1};
                                    ", model.Name,SubQuery);
      

            List<SanctionEntity> results = new List<SanctionEntity>();
            try
            {
                FuzzyNameMatcher fuzzyNameMatcher = new FuzzyNameMatcher();
                DataSet dsResult;
                using (var conn = _dbConnection.CreateConnectionsql())

                using (var cmd = new SqlCommand(Query, conn))
                {
                    await conn.OpenAsync();
                    using (var reader = cmd.ExecuteReader())
                    {
                        Func<string, List<string>> getList = col =>
                        {
                            try
                            {
                                if (reader[col] == DBNull.Value) return null;

                                string val = reader[col]?.ToString();
                                if (string.IsNullOrWhiteSpace(val)) return null;

                                return JsonSerializer.Deserialize<List<string>>(val);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"JSON error in column '{col}'. Value: {reader[col]}", ex);
                            }
                        };

                        while (reader.Read())
                        {
                            var item = new SanctionEntity
                            {
                                AmlId = reader["AmlId"]?.ToString()!,
                                Type = reader["Type"]?.ToString()!,
                                source_id = reader["SourceId"]?.ToString()!,
                                source_type = reader["SourceType"]?.ToString()!,
                                entity_type = reader["EntityType"]?.ToString(),
                                gender = reader["Gender"]?.ToString(),
                                name = reader["Name"]?.ToString(),
                                alias_names = getList("Alias_names"),
                                date_of_birth = getList("DateOfBirth"),
                                other_information = getList("OtherInformation"),
                                list_date = reader["ListDate"] == DBNull.Value
                                                                ? (DateTime?)null
                                                                : Convert.ToDateTime(reader["ListDate"]),
                                place_of_birth = getList("PlaceOfBirth")
                            };

                            if (item.Type == "Soundex"  )
                            {
                               // item.Score = OfacNameMatcher.ComputeScore(model.Name, item.name);
                               // int finalScore = AmlNameMatcher.CalculateScore(model.Name, item.name);
                                item.Score = FuzzySearch.CalculateScore(model.Name, item.name);
                                if (item.Score >= 85 && model.IsFuzzy == true)
                                {
                                    results.Add(item);
                                }
                            }
                            else
                            {
                                //int finalScore = FuzzySearch.CalculateScore(model.Name, item.name);
                                item.Score = 100;
                                results.Add(item);
                            }
                            //results.Add(item);
                            //if (item.AmlId == "2748a58750374a6d")
                            //{
                            //    int ff4 = FuzzySearch.CalculateScore(model.Name, item.name);

                            //}
                        }

                    }
                }

                //results.AddRange(res2);
            }
            catch (Exception ex)
            {
            }
            return results;
        }

        //*** For UI Implementation **
        public async Task<List<SanctionEntity?>> GetSearchSanctionIndividualForUI(AMLFilter model)
        {

            string Query = string.Empty;
            string SubQuery = string.Empty;

            model.Name = TokenizeWithoutStopWords(model.Name);

            if (!string.IsNullOrWhiteSpace(model.DateOfBirth))
            {
                SubQuery += string.Format(@" AND (DateOfBirth LIKE '%{0}%' OR DateOfBirth = 'null')", model.DateOfBirth);
            }
            if (!string.IsNullOrWhiteSpace(model.Gender))
            {
                SubQuery += string.Format(@" AND (Gender='{0}' OR Gender='UNKNOWN')", model.Gender);
            }
            if ((!string.IsNullOrWhiteSpace(model.Type)) && model.Type!="ALL")
            {
                SubQuery += string.Format(@" AND ( EntityType='{0}')", model.Type);
            }
            if (!string.IsNullOrWhiteSpace(model.SourceType) && model.SourceType != "ALL")
            {
                SubQuery += string.Format(@" AND ( SourceType='{0}')", model.SourceType);
            }
            if (!string.IsNullOrWhiteSpace(model.Country))
            {
                SubQuery += string.Format(@" AND ( Address like '%{0}%')", model.Country);
            }
            if (model.Includes != null && model.Includes.Any())
            {
                var includes = string.Join("','", model.Includes);
                SubQuery += $" AND (SourceId IN ('{includes}'))";
            }
            SubQuery += " AND IsDelete = 0 ";

            Query = string.Format(@"
                                    SELECT Top 50 *,'NonSoundex' as [Type]
                                    FROM AMLSource
                                    WHERE CONTAINS(([Name], Alias_names), '""{0}""') {1}
                                    union all
                                    SELECT TOP (1000) *,'Soundex' AS [Type]                                    
                                    FROM AMLSource aml
                                    WHERE SOUNDEX([Name]) = SOUNDEX('{0}')
                                    AND NOT EXISTS
                                    (
                                        SELECT 1
                                        FROM AMLSource s
                                        WHERE s.AmlId = aml.AmlId
                                        AND CONTAINS(([Name], Alias_names), '""{0}""') 
                                    ) {1};
                                    ", model.Name, SubQuery);


            List<SanctionEntity> results = new List<SanctionEntity>();
            try
            {
                FuzzyNameMatcher fuzzyNameMatcher = new FuzzyNameMatcher();
                DataSet dsResult;
                using (var conn = _dbConnection.CreateConnectionsql())

                using (var cmd = new SqlCommand(Query, conn))
                {
                    await conn.OpenAsync();
                    using (var reader = cmd.ExecuteReader())
                    {
                        Func<string, List<string>> getList = col =>
                        {
                            try
                            {
                                if (reader[col] == DBNull.Value) return null;

                                string val = reader[col]?.ToString();
                                if (string.IsNullOrWhiteSpace(val)) return null;

                                return JsonSerializer.Deserialize<List<string>>(val);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"JSON error in column '{col}'. Value: {reader[col]}", ex);
                            }
                        };

                        while (reader.Read())
                        {

                            var item = new SanctionEntity
                            {
                                AmlId = reader["AmlId"]?.ToString()!,
                                source_id = reader["SourceId"]?.ToString()!,
                                source_type = reader["SourceType"]?.ToString()!,
                                Type = reader["Type"]?.ToString()!,
                                pep_type = reader["PepType"] == DBNull.Value
                                    ? null
                                    : reader["ListDate"].ToString(),
                                entity_type = reader["EntityType"]?.ToString(),
                                gender = reader["Gender"]?.ToString(),
                                name = reader["Name"]?.ToString(),
                                alias_names = getList("Alias_names"),
                                last_names = getList("LastNames"),
                                name_remarks = getList("NameRemarks"),
                                given_names = getList("GivenNames"),
                                alias_given_names = getList("AliasGivenNames"),
                                spouse = getList("Spouse"),
                                parents = getList("Parents"),
                                //  children = getList("Children"),
                                //  siblings = getList("Siblings"),
                                date_of_birth = getList("DateOfBirth"),
                                date_of_birth_remarks = getList("DateOfBirthRemarks"),
                                address = getList("Address"),
                                address_remarks = getList("AddressRemarks"),
                                sanction_details = getList("SanctionDetails"),
                                functions = getList("Functions"),
                                description = getList("Description"),
                                occupations = getList("Occupations"),
                                positions = getList("Positions"),
                                political_parties = getList("PoliticalParties"),
                                citizenship = getList("Citizenship"),
                                citizenship_remarks = getList("CitizenshipRemarks"),
                                titles = getList("Titles"),
                                other_information = getList("OtherInformation"),
                                list_date = reader["ListDate"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(reader["ListDate"]),
                                links = getList("Links"),
                                place_of_birth = getList("PlaceOfBirth")
                            };

                            if (item.Type == "Soundex")
                            {

                                item.Score = FuzzySearch.CalculateScore(model.Name, item.name); ;
                                if (item.Score >= model.MatchParcentage)
                                {
                                    results.Add(item);
                                }
                            }
                            else
                            {
                                item.Score = 100;
                                results.Add(item);
                            }
      
                        }

                    }
                }

                //results.AddRange(res2);
            }
            catch (Exception ex)
            {
            }
            try
            {
                string query = @"
                                INSERT INTO UserActivity (UserId, SearchedText, TotalHitCount, IpAddress, DateAdded) 
                                VALUES (@UserId, @SearchedText, @TotalHitCount,@IpAddress,@DateAdded);";

                using (SqlConnection con = _dbConnection.CreateConnectionsql())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        // Use parameters to avoid SQL injection
                        cmd.Parameters.AddWithValue("@UserId", model.UserId);
                        cmd.Parameters.AddWithValue("@SearchedText", model.Name);
                        cmd.Parameters.AddWithValue("@TotalHitCount", results.Count());
                        cmd.Parameters.AddWithValue("@IpAddress", model.IpAddress);
                        cmd.Parameters.AddWithValue("@DateAdded", DateTime.Now);
                        // Return the inserted ID
                        int result = cmd.ExecuteNonQuery();


                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }



            return results;
        }
        //*** For UI pdf **
        public async Task<List<SanctionEntity?>> GetSearchingResultDownload(string ids)
        {

            string result = string.Join(",", ids
                                    .Split(',')
                                    .Select(s => $"'{s}'")
                                   );

            string Query = string.Empty;

            Query = string.Format(@"select * from AMLSource where AmlId in({0})",result);


            List<SanctionEntity> results = new List<SanctionEntity>();
            try
            {
                FuzzyNameMatcher fuzzyNameMatcher = new FuzzyNameMatcher();
                DataSet dsResult;
                using (var conn = _dbConnection.CreateConnectionsql())

                using (var cmd = new SqlCommand(Query, conn))
                {
                    await conn.OpenAsync();
                    using (var reader = cmd.ExecuteReader())
                    {
                        Func<string, List<string>> getList = col =>
                        {
                            try
                            {
                                if (reader[col] == DBNull.Value) return null;

                                string val = reader[col]?.ToString();
                                if (string.IsNullOrWhiteSpace(val)) return null;

                                return JsonSerializer.Deserialize<List<string>>(val);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"JSON error in column '{col}'. Value: {reader[col]}", ex);
                            }
                        };

                        while (reader.Read())
                        {

                            var item = new SanctionEntity
                            {
                                AmlId = reader["AmlId"]?.ToString()!,
                                source_id = reader["SourceId"]?.ToString()!,
                                source_type = reader["SourceType"]?.ToString()!,
                                pep_type = reader["PepType"] == DBNull.Value
                                    ? null
                                    : reader["ListDate"].ToString(),
                                entity_type = reader["EntityType"]?.ToString(),
                                gender = reader["Gender"]?.ToString(),
                                name = reader["Name"]?.ToString(),
                                alias_names = getList("Alias_names"),
                                last_names = getList("LastNames"),
                                name_remarks = getList("NameRemarks"),
                                given_names = getList("GivenNames"),
                                alias_given_names = getList("AliasGivenNames"),
                                spouse = getList("Spouse"),
                                parents = getList("Parents"),
                                //  children = getList("Children"),
                                //  siblings = getList("Siblings"),
                                date_of_birth = getList("DateOfBirth"),
                                date_of_birth_remarks = getList("DateOfBirthRemarks"),
                                address = getList("Address"),
                                address_remarks = getList("AddressRemarks"),
                                sanction_details = getList("SanctionDetails"),
                                functions = getList("Functions"),
                                description = getList("Description"),
                                occupations = getList("Occupations"),
                                positions = getList("Positions"),
                                political_parties = getList("PoliticalParties"),
                                citizenship = getList("Citizenship"),
                                citizenship_remarks = getList("CitizenshipRemarks"),
                                titles = getList("Titles"),
                                other_information = getList("OtherInformation"),
                                list_date = reader["ListDate"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(reader["ListDate"]),
                                links = getList("Links"),
                                place_of_birth = getList("PlaceOfBirth")
                            };

   
                                results.Add(item);
                            

                        }

                    }
                }

                //results.AddRange(res2);
            }
            catch (Exception ex)
            {
            }
            return results;
        }

        public async Task<List<SearchResult?>> GetSearchSanction(AMLFilter model)
        {

            List<SearchResult?> lst = new List<SearchResult?>();                     
            try
            {
                using (SqlConnection conn = _dbConnection.CreateConnectionsql())
                using (SqlCommand cmd = new SqlCommand("GetAMLSourceByName", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@SearchName", model.Name);
                    cmd.Parameters.AddWithValue("@EntityType", model.EntityType);
                    cmd.Parameters.AddWithValue("@SourceType", model.SourceType);
                    // cmd.Parameters.AddWithValue("@SourceCountry", model.SourceCountry);
                    cmd.Parameters.AddWithValue("@SourceId", model.SourceId);
                    cmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth);
                    // cmd.Parameters.AddWithValue("@Nationality", model.Nationality);

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            SearchResult searchResult = new SearchResult
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString()!,
                                Address = string.Join(", ", JsonSerializer.Deserialize<List<string>>(reader["address"].ToString())?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>()) ,
                                EntityType = reader["EntityType"].ToString()!,
                                SourceType = reader["SourceType"].ToString()!,
                                SourceId = reader["SourceId"].ToString()!,
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

        public async Task<string> GetAllSourceId()
        {

            string sourchId = string.Empty;
            try
            {
                string Query = "select distinct SourceId from AMLSource where IsDelete = 0";

                using (var conn = _dbConnection.CreateConnectionsql())
                using (var cmd = new SqlCommand(Query, conn))
                {
                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            sourchId += reader["sourceid"].ToString()+"|";                     
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return sourchId;
        }

        public async Task<Sanction> GetSanctionDetailsById(int id)
        {
            Sanction sanctionEntity = new Sanction();


            try
            {
                string Query = "select * from AMLSource where Id=" + id + " AND IsDelete = 0 ";

                using (var conn = _dbConnection.CreateConnectionsql())
                using (var cmd = new SqlCommand(Query, conn))
                {

                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Sanction sanctionEntity2 = new Sanction
                            {
                                Id = reader["id"].ToString(),
                                AmlId = reader["amlid"].ToString(),
                                SourceId = reader["sourceid"].ToString()!,
                                SourceType = reader["sourcetype"].ToString()!,
                                PepType = reader["peptype"].ToString(),
                                EntityType = reader["entitytype"].ToString(),
                                Gender = reader["gender"].ToString(),
                                Name = reader["name"].ToString(),
                                TlName = reader["tlname"].ToString(),
                                AliasNames = JsonSerializer.Deserialize<List<string>>(reader["alias_names"].ToString()),
                                LastNames = JsonSerializer.Deserialize<List<string>>(reader["lastnames"].ToString()),
                                GivenNames = JsonSerializer.Deserialize<List<string>>(reader["givennames"].ToString()),
                                AliasGivenNames = JsonSerializer.Deserialize<List<string>>(reader["aliasgivennames"].ToString()),
                                Parents = JsonSerializer.Deserialize<List<string>>(reader["parents"].ToString()),
                                DateOfBirth = JsonSerializer.Deserialize<List<string>>(reader["dateofbirth"].ToString()),
                                Address = JsonSerializer.Deserialize<List<string>>(reader["address"].ToString()),
                                AddressRemarks = JsonSerializer.Deserialize<List<string>>(reader["addressremarks"].ToString()),
                                SanctionDetails = JsonSerializer.Deserialize<List<string>>(reader["sanctiondetails"].ToString()),
                                Description = JsonSerializer.Deserialize<List<string>>(reader["description"].ToString()),
                                Occupations = JsonSerializer.Deserialize<List<string>>(reader["occupations"].ToString()),
                                Positions = JsonSerializer.Deserialize<List<string>>(reader["positions"].ToString()),
                                PoliticalParties = JsonSerializer.Deserialize<List<string>>(reader["politicalparties"].ToString()),
                                Links = JsonSerializer.Deserialize<List<string>>(reader["links"].ToString()),
                                Functions = JsonSerializer.Deserialize<List<string>>(reader["functions"].ToString()),
                                Citizenship = JsonSerializer.Deserialize<List<string>>(reader["citizenship"].ToString()),
                                Titles = JsonSerializer.Deserialize<List<string>>(reader["titles"].ToString()),
                                OtherInformation = JsonSerializer.Deserialize<List<string>>(reader["otherinformation"].ToString())
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

        public int CreateAMLLog(AMLSourceLog model)
        {
            try
            {
                string query = @"
                        INSERT INTO AMLSourceLogs (FileName, FileVersion, Total, SourceName, SourceLink, SourceCountry, CreatedDate) 
                        VALUES (@FileName, @FileVersion, @Total,@SourceName,@SourceLink,@SourceCountry, @CreatedDate);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using (SqlConnection con = _dbConnection.CreateConnectionsql())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        // Use parameters to avoid SQL injection
                        cmd.Parameters.AddWithValue("@FileName", model.FileName);
                        cmd.Parameters.AddWithValue("@FileVersion", model.FileVersion);
                        cmd.Parameters.AddWithValue("@Total", model.Total);
                        cmd.Parameters.AddWithValue("@SourceName", model.SourceName);
                        cmd.Parameters.AddWithValue("@SourceLink", model.SourceLink);
                        cmd.Parameters.AddWithValue("@SourceCountry", model.SourceCountry);
                        cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                        // Return the inserted ID
                        object result = cmd.ExecuteScalar();

                        int row = (result != null) ? Convert.ToInt32(result) : 0;

                        if (row > 0)
                        {
                            return row;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public  int CreateAMLDataStatusLog(AMLSourceLog model)
        {
            string query = @"
                        INSERT INTO AMLDataStatusLog (TotalPrivious,TotalDownload, TotalNew, TotalUpdate, TotalDelete,SourceName,SourceLink,SourceCountry,CreatedDate) 
                        VALUES (@TotalPrivious,@TotalDownload, @TotalNew, @TotalUpdate, @TotalDelete,@SourceName,@SourceLink,@SourceCountry,@CreatedDate);";

            using (SqlConnection con = _dbConnection.CreateConnectionsql())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    // Use parameters to avoid SQL injection
                    cmd.Parameters.AddWithValue("@TotalPrivious", model.TotalPrivious);
                    cmd.Parameters.AddWithValue("@TotalDownload", model.TotalData);
                    cmd.Parameters.AddWithValue("@TotalNew", model.TotalNew);
                    cmd.Parameters.AddWithValue("@TotalUpdate", model.TotalUpdate);
                    cmd.Parameters.AddWithValue("@TotalDelete", model.TotalDelete);
                    cmd.Parameters.AddWithValue("@SourceName", model.SourceName);
                    cmd.Parameters.AddWithValue("@SourceLink", model.SourceLink);
                    cmd.Parameters.AddWithValue("@SourceCountry", model.SourceCountry);
                    cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                    // Return the inserted ID
                    int row = cmd.ExecuteNonQuery();

                    if (row > 0)
                    {
                        return row;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }

        public async Task<string?> GetFileVersion()
        {
            string Query = "select Top 1 FileVersion from AMLSourceLogs order by Id desc";
            string? Version = string.Empty;
            try
            {
                using (SqlConnection conn = _dbConnection.CreateConnectionsql())
                using (SqlCommand cmd = new SqlCommand(Query, conn))
                {

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Version = reader["FileVersion"] == DBNull.Value ? null : reader["FileVersion"].ToString();
                        }
                    }
                }
                return Version;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public async Task<int?> TotalDataCount()
        {
            string Query = "select top 1 TotalPrivious,TotalDownload from AMLDataStatusLog order by id desc";
            int? PrevData = 0;
            try
            {
                using (SqlConnection conn = _dbConnection.CreateConnectionsql())
                using (SqlCommand cmd = new SqlCommand(Query, conn))
                {

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if(await reader.ReadAsync())
                        {
                            PrevData = reader["TotalDownload"] == DBNull.Value ? null : Convert.ToInt32(reader["TotalDownload"]);
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

        public async Task<List<AMLSourceLog>> GetAllSourceLog(string from, string to)
        {

            List<AMLSourceLog> list = new List<AMLSourceLog>();
            try
            {

                string Query = string.Format("SELECT * FROM AMLSourceLogs WHERE CreatedDate >= '{0}' AND CreatedDate <= '{1}'; ",from,to);
                using (var conn = _dbConnection.CreateConnectionsql())
                using (var cmd = new SqlCommand(Query, conn))
                {

                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            AMLSourceLog aMLSourceLog = new AMLSourceLog
                            {
                                Id = Convert.ToInt32(reader["Id"].ToString()),
                                SourceName = reader["SourceName"].ToString(),
                                SourceUrl = reader["SourceLink"].ToString()!,
                                SourceCountry = reader["SourceCountry"].ToString()!,
                                TotalRecord = Convert.ToInt32(reader["Total"].ToString()),
                                DateAdded = Convert.ToDateTime(reader["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                            };
                            list.Add(aMLSourceLog);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return list;
        }

        public async Task<List<AMLSourceLog>> GetAllDataStatusLog(string from, string to)
        {
            List<AMLSourceLog> list = new List<AMLSourceLog>();
            try
            {
                string Query = string.Format("SELECT * FROM AMLDataStatusLog WHERE CreatedDate >= '{0}' AND CreatedDate <= '{1}'; ", from, to);
                using (var conn = _dbConnection.CreateConnectionsql())
                using (var cmd = new SqlCommand(Query, conn))
                {
                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            AMLSourceLog aMLSourceLog = new AMLSourceLog
                            {
                                Id = Convert.ToInt32(reader["Id"].ToString()),
                                PrevDataTotal = Convert.ToInt32(reader["TotalPrivious"].ToString()),
                                TotalData = Convert.ToInt32(reader["TotalDownload"].ToString()),
                                TotalNewlyAdded = Convert.ToInt32(reader["TotalNew"].ToString()),
                                TotalDataRemove = Convert.ToInt32(reader["TotalDelete"].ToString()),
                                TotalUpdated = Convert.ToInt32(reader["TotalUpdate"].ToString()),
                                SourceName = reader["SourceName"].ToString(),
                                SourceUrl = reader["SourceLink"].ToString()!,
                                SourceCountry = reader["SourceCountry"].ToString()!,
                                DateAdded = Convert.ToDateTime(reader["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                            };
                            list.Add(aMLSourceLog);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return list;
        }

        public async Task<List<SanctionEntity?>> GetSearchSanctionCheckEntity(AMLFilter model)
        {
            List<SanctionEntity> sanctionEntities = new List<SanctionEntity>();
            try
            {
                using (SqlConnection conn = _dbConnection.CreateConnectionsql())
                using (SqlCommand cmd = new SqlCommand("GetAMLSourceBycheckEntity", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@SearchName", model.Name);
                    cmd.Parameters.AddWithValue("@SourceId", model.SourceId);

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            SanctionEntity sanctionEntity = new SanctionEntity();
                            sanctionEntity.source_id = reader["SourceId"].ToString();
                            sanctionEntity.source_type = reader["SourceType"].ToString();
                            sanctionEntity.entity_type = reader["EntityType"].ToString();
                            sanctionEntity.name = reader["Name"].ToString();
                            sanctionEntity.alias_names = JsonSerializer.Deserialize<List<string>>(reader["Alias_names"].ToString());
                            sanctionEntity.name_remarks = JsonSerializer.Deserialize<List<string>>(reader["NameRemarks"].ToString());
                            sanctionEntity.jurisdiction = JsonSerializer.Deserialize<List<string>>(reader["Jurisdiction"].ToString());
                            sanctionEntity.address = JsonSerializer.Deserialize<List<string>>(reader["Address"].ToString());
                            sanctionEntity.company_number = JsonSerializer.Deserialize<List<string>>(reader["CompanyNumber"].ToString());
                            sanctionEntity.address_remarks = JsonSerializer.Deserialize<List<string>>(reader["AddressRemarks"].ToString());
                            sanctionEntity.sanction_details = JsonSerializer.Deserialize<List<string>>(reader["SanctionDetails"].ToString());
                            sanctionEntity.other_information = JsonSerializer.Deserialize<List<string>>(reader["OtherInformation"].ToString());
                            sanctionEntities.Add(sanctionEntity);
                        }
                    }
                }
                return sanctionEntities;


            }
            catch (Exception ex)
            {
                return sanctionEntities;
            }
        }

        public bool CreateSource(Source model)
        {
            string query = @"
                        INSERT INTO AMLListSource (Source, Name, Description, Link, Country_name, Source_name, LastSseen, CreatedDate) 
                        VALUES (@Source, @Name, @Description,@Link,@Country_name,@Source_name,@LastSseen, @CreatedDate);";

            try
            {
                using (SqlConnection con = _dbConnection.CreateConnectionsql())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        //Use parameters to avoid SQL injection
                        cmd.Parameters.AddWithValue("@Source", (object?)model.source ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Name", (object?)model.name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Description", (object?)model.description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Link", (object?)model.link ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Country_name", (object?)model.country_name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Source_name", (object?)model.source_name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LastSseen", (object?)model.last_seen ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                        // Return the inserted ID
                        int result = cmd.ExecuteNonQuery();

                        if (result > 0)
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

        public async Task<List<Source>> GetAllSourceList()
        {
            List<Source> list = new List<Source>();
            try
            {
                string Query = "SELECT * FROM AMLlistSource;";

                using (var conn = _dbConnection.CreateConnectionsql())
                using (var cmd = new SqlCommand(Query, conn))
                {

                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Source aMLSource = new Source
                            {
                                source = reader["Source"].ToString(),
                                name = reader["Name"].ToString()!,
                                description = reader["Description"].ToString(),
                                link = reader["link"].ToString(),
                                country_name = reader["Country_name"].ToString(),
                                source_name = reader["Source_name"].ToString(),
                                last_seen = long.Parse(reader["lastSseen"].ToString())

                            };

                            list.Add(aMLSource);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return list;



        }

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "THE","AL","OF","EL","LA","LE","DE","DA","AND","INC","LTD","LLC","COMPANY","MD.","MD"
        };

        private static string TokenizeWithoutStopWords(string text)
        {
            return string.Join(" ",
                Normalizer.Normalize(text)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(t => !StopWords.Contains(t))
            );
        }
    }
}
