using DocumentFormat.OpenXml.EMMA;
using Nec.Web.Config;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Models.Model;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;

namespace Nec.Web.Services
{
    public class UserService : IUserService
    {
        private IIDbConnection _dbConnection;

        public UserService(IIDbConnection iDbConnection) 
        {
            _dbConnection= iDbConnection;
        }

        public bool CreateUser(User model) 
        {

            try
            {
                string query = @"
                                INSERT INTO UserInfo
                                (FirstName, LastName, Email, Phone, Password, Country, ZipCode, City, Address, Role, IsAllow, CreatedBy,UpdatedBy, CreatedDate)
                                VALUES
                                (@FirstName, @LastName, @Email, @Phone, @Password, @Country, @ZipCode, @City, @Address, @Role, @IsAllow, @CreatedBy,@UpdatedBy, @CreatedDate);

                                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using (SqlConnection con = _dbConnection.CreateConnectionsql())
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        // Parameters (Prevents SQL Injection)

                        cmd.Parameters.AddWithValue("@FirstName", model.Payload?.FirstName);
                        cmd.Parameters.AddWithValue("@LastName", model.Payload?.LastName);
                        cmd.Parameters.AddWithValue("@Email", model.Payload?.Email);
                        cmd.Parameters.AddWithValue("@Phone", (object?)model.Payload?.Phone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Password", model.Payload?.Password);
                        cmd.Parameters.AddWithValue("@Country", (object?)model.Payload?.UserInfo?.Country ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ZipCode", (object?)model.Payload?.UserInfo?.Zip ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@City", (object?)model.Payload?.UserInfo?.City ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", (object?)model.Payload?.UserInfo?.Address1 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Role", JsonSerializer.Serialize((object?)model.Payload?.Roles));
                        cmd.Parameters.AddWithValue("@IsAllow", model.Payload?.IsAllowLogin);
                        cmd.Parameters.AddWithValue("@CreatedBy", (object?)model.Payload?.UserModifiedId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UpdatedBy", (object?)model.Payload?.UserModifiedId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                        object result = cmd.ExecuteScalar();

                        int insertedId = (result != null) ? Convert.ToInt32(result) : 0;

                        return insertedId > 0 ? true:false;
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public bool UpdateUser(User model)
        {

            try
            {
                string query = @"
                                UPDATE UserInfo 
                                SET FirstName = @FirstName,
                                    LastName = @LastName,
                                    Phone = @Phone,
                                    Country = @Country,
                                    ZipCode = @ZipCode,
                                    City = @City,
                                    [Address] = @Address,
                                    [Role] = @Role,
                                    [IsAllow] = @IsAllow,
                                    [UpdatedBy] = @UpdatedBy
                                WHERE Id = @Id";

                using (SqlConnection con = _dbConnection.CreateConnectionsql())
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        // Parameters (Prevents SQL Injection)

                        cmd.Parameters.AddWithValue("@FirstName", model.Payload?.FirstName);
                        cmd.Parameters.AddWithValue("@LastName", model.Payload?.LastName);
                        cmd.Parameters.AddWithValue("@Phone", (object?)model.Payload?.Phone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Country", (object?)model.Payload?.UserInfo?.Country ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ZipCode", (object?)model.Payload?.UserInfo?.Zip ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@City", (object?)model.Payload?.UserInfo?.City ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", (object?)model.Payload?.UserInfo?.Address1 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Role", JsonSerializer.Serialize((object?)model.Payload?.Roles));
                        cmd.Parameters.AddWithValue("@IsAllow", model.Payload?.IsAllowLogin);
                        cmd.Parameters.AddWithValue("@UpdatedBy", (object?)model.Payload?.UserModifiedId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", model.Payload?.UserId);

                        object result = cmd.ExecuteScalar();

                        int insertedId = (result != null) ? Convert.ToInt32(result) : 0;

                        return insertedId > 0 ? true : false;
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }



        public async Task<List<Payload3>> GetAllUser()
        {

            List<Payload3> payload = new List<Payload3>();
            string  Query = "Select * from UserInfo";

            try
            {
                using (var conn = _dbConnection.CreateConnectionsql())

                using (var cmd = new SqlCommand(Query, conn))
                {
                    await conn.OpenAsync();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new Payload3
                            {
                                UserId =Convert.ToInt32(reader["Id"]),
                                FirstName = reader["FirstName"]?.ToString()!,
                                LastName = reader["LastName"]?.ToString()!,
                                Email = reader["Email"]?.ToString()!,
                                Phone = reader["Phone"]?.ToString(),
                                IsAllowLogin = Convert.ToInt32((reader["IsAllow"])),
                                Roles = JsonSerializer.Deserialize<List<Role3>>(reader["Role"].ToString()),
                                UserInfo= new UserInfo3
                                {
                                    Country= reader["Country"]?.ToString()!,
                                    Address1 = reader["Address"]?.ToString()!,
                                    CompanyName="null",
                                    City= reader["City"]?.ToString()!,
                                    Zip = reader["ZipCode"]?.ToString()!
                                }
                            };
                            payload.Add(item);
                        }

                    }
                }


            }
            catch (Exception ex)
            {
            }
            return payload;
        }

        public async Task<Payload3> GetUserByUserName(string username)
        {
            Payload3 payload = new Payload3();
            List<Role3> Roles = new List<Role3>();

            string query = @"select top 1 * from UserInfo where Email=@username;";

            try
            {
                using (var conn = _dbConnection.CreateConnectionsql())

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    await conn.OpenAsync();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                        {
                            var item = new Payload3
                            {
                                UserId = Convert.ToInt32(reader["Id"]),
                                FirstName = reader["FirstName"]?.ToString()!,
                                LastName = reader["LastName"]?.ToString()!,
                                Email = reader["Email"]?.ToString()!,
                                Phone = reader["Phone"]?.ToString(),
                                IsAllowLogin = Convert.ToInt32((reader["IsAllow"])),
                                LoginName= reader["Email"]?.ToString(),
                                Password= reader["Password"]?.ToString(),
                                Roles = JsonSerializer.Deserialize<List<Role3>>(reader["Role"].ToString()),
                                UserInfo = new UserInfo3
                                {
                                    Country = reader["Country"]?.ToString()!,
                                    Address1 = reader["Address"]?.ToString()!,
                                    CompanyName = "null",
                                    City = reader["City"]?.ToString()!,
                                    Zip = reader["ZipCode"]?.ToString()!,
                                    ApiKey=""
                                }
                            };
                            payload= item;
                        }

                    }
                }

                var roleIds = string.Join(",", payload.Roles.Select(r => r.RoleId));

                string query2 = @"SELECT  *
                                  FROM UserRole
                                  WHERE RoleId IN (
                                      SELECT value FROM STRING_SPLIT(@RoleIds, ',')
                                  );";


                using (var conn = _dbConnection.CreateConnectionsql())


                using (var cmd = new SqlCommand(query2, conn))
                {
                    cmd.Parameters.AddWithValue("@RoleIds", roleIds);

                    await conn.OpenAsync();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new Role3
                            {
                                RoleId = Convert.ToInt32(reader["Id"]),
                                RoleName = reader["RoleName"]?.ToString()!
                            };
                            Roles.Add(item);
                        }

                    }
                }

                payload.Roles = Roles;

            }
            catch (Exception ex)
            {
            }
            return payload;
        }

        public async Task<List<UserActivity>> GetAllUserActivity(UserFilter userFilter)
        {

            List<UserActivity> lst = new List<UserActivity>();  
         
            string query = @"select * from UserActivity where 1=1 ";

            if( userFilter.Payload?.UserKey !=0 )
            {
                query += string.Format("AND userid={0}",userFilter.Payload?.UserKey);
            }
            if(userFilter.Payload.DtFrom.HasValue && userFilter.Payload.DtTo.HasValue)
            {
                query += string.Format("AND DateAdded BETWEEN '{0}' AND '{1}'", userFilter.Payload?.DtFrom, userFilter.Payload?.DtTo.Value.AddDays(1));
            }
            try
            {
                using (var conn = _dbConnection.CreateConnectionsql())

                using (var cmd = new SqlCommand(query, conn))
                {
                    await conn.OpenAsync();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new UserActivity
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                SearchedText = reader["SearchedText"]?.ToString()!,
                                TotalHitCount = Convert.ToInt32(reader["TotalHitCount"]),
                                IpAddress = reader["IpAddress"]?.ToString()!,
                                DateAdded = Convert.ToDateTime(reader["DateAdded"]).ToString("yyyy-MM-dd"),
                            };
                            lst.Add(item);
                        }

                    }
                }

            }
            catch (Exception ex)
            {
            }

            return lst;
        }

        public async Task<UserInfo> ValidationUser(string username)
        {

            UserInfo model = new UserInfo();
            string query = @"select top 1 * from UserInfo where Email=@username and IsAllow = 1;";

            try
            {
                using (SqlConnection con = _dbConnection.CreateConnectionsql())
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        await con.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                model = new UserInfo
                                {
                                    FirstName = reader["FirstName"]?.ToString()!,
                                    LastName = reader["LastName"]?.ToString()!, 
                                    Email = reader["Email"]?.ToString()!,
                                    Phone = reader["Phone"]?.ToString(),
                                    Password = reader["Password"]?.ToString(),
                                //    IsAllow = Convert.ToInt32((reader["IsAllow"])), 
                                    //Roles = JsonSerializer.Deserialize<List<Role3>>(reader["Role"].ToString()),
                                    Role = reader["Role"].ToString(),
                                };
                                
                            }
                        }


                    }
                }
                return model;
            }
            catch (Exception ex)
            {

                return model;
            }

        }
        //public async Task<List<User>> GetAllUser()
        //{
        //    List<User> users = new List<User>();

        //    string Query = string.Empty;
        //    string SubQuery = string.Empty;


        //    if (model.Payload.DtTo.HasValue)
        //    {
        //        SubQuery += string.Format(@" AND (DateOfBirth LIKE '%{0}%' OR DateOfBirth = 'null')", model.Payload.DtFrom, model.Payload.DtTo);
        //    }
        //    if (!string.IsNullOrWhiteSpace(model.Payload.UserKey.ToString()))
        //    {
        //        SubQuery += string.Format(@" AND (Gender='{0}' OR Gender='UNKNOWN')", model.Gender);
        //    }
        //    Query = string.Format(@"

        //                            ", model.Name, SubQuery);


        //    List<SanctionEntity> results = new List<SanctionEntity>();
        //    try
        //    {
        //        FuzzyNameMatcher fuzzyNameMatcher = new FuzzyNameMatcher();
        //        DataSet dsResult;
        //        using (var conn = _dbConnection.CreateConnectionsql())

        //        using (var cmd = new SqlCommand(Query, conn))
        //        {
        //            await conn.OpenAsync();
        //            using (var reader = cmd.ExecuteReader())
        //            {

        //                while (reader.Read())
        //                {
        //                    var item = new SanctionEntity
        //                    {
        //                        AmlId = reader["AmlId"]?.ToString()!,
        //                        Type = reader["Type"]?.ToString()!,
        //                        source_id = reader["SourceId"]?.ToString()!,
        //                        source_type = reader["SourceType"]?.ToString()!,
        //                        entity_type = reader["EntityType"]?.ToString(),
        //                        gender = reader["Gender"]?.ToString(),
        //                        name = reader["Name"]?.ToString(),
        //                        list_date = reader["ListDate"] == DBNull.Value
        //                                                        ? (DateTime?)null
        //                                                        : Convert.ToDateTime(reader["ListDate"]),
        //                        place_of_birth = getList("PlaceOfBirth")
        //                    };
        //                    results.Add(item);
        //                }

        //            }
        //        }

        //        //results.AddRange(res2);
        //    }
        //    catch (Exception ex)
        //    {
        //    }

        //    return users;
        //}

        public async Task<List<UserRole>> GetAlLRole()
        {

            List<UserRole> lst = new List<UserRole>();

            string query = @"select * from UserRole where 1=1 ";

            try
            {
                using (var conn = _dbConnection.CreateConnectionsql())

                using (var cmd = new SqlCommand(query, conn))
                {
                    await conn.OpenAsync();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new UserRole
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Description = reader["Description"]?.ToString()!,
                                RoleId = Convert.ToInt32(reader["RoleId"]),
                                RoleVer = reader["RoleVer"]?.ToString()!,
                                RoleName = reader["RoleName"]?.ToString(),
                            };
                            lst.Add(item);
                        }

                    }
                }

            }
            catch (Exception ex)
            {
            }

            return lst;
        }

    }
}
