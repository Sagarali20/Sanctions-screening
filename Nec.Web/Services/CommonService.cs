using Microsoft.Extensions.FileSystemGlobbing;
using Nec.Web.Config;
using Nec.Web.Interfaces;
using Nec.Web.Models;
using Nec.Web.Models.Model;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;

namespace Nec.Web.Services
{
    public class CommonService : ICommonService
    {

        public IIDbConnection _dbConnection;
        public CommonService(IIDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public async Task<List<CommonSearchResult?>> GetCommonSearch(AMLFilter model)
        {

            string OfacSub = string.Empty;
            string UKSub = string.Empty;
            string UNSub = string.Empty;
            string PepDilisenceSub = string.Empty;
            string SoundMatch = string.Empty;

            string Query=string.Empty;  



            if (!string.IsNullOrWhiteSpace(model.EntityType))
            {
                OfacSub += string.Format("AND OS.SdnType='{0}'", model.EntityType);
                UKSub += string.Format("AND IndividualEntityShip='{0}'", model.EntityType);
                PepDilisenceSub += string.Format("AND EntityType='{0}'", model.EntityType);
            }
            if (!string.IsNullOrWhiteSpace(model.DateOfBirth))
            {
                OfacSub += string.Format("AND OS.DateOfBirthList like '%{0}%'", model.DateOfBirth);
                UKSub += string.Format("AND IndividualDetails like '%{0}%'", model.DateOfBirth);
                UNSub += string.Format("AND IndividualDateOfBirth like '%{0}%'", model.DateOfBirth);
                PepDilisenceSub += string.Format("AND DateOfBirth='{0}'", model.DateOfBirth);

            }
            if (!string.IsNullOrWhiteSpace(model.Country))
            {
                OfacSub += string.Format("AND OS.AddressList like '%{0}%'", model.Country);
                UKSub += string.Format("AND IndividualDetails like '%{0}%'", model.Country);
                UNSub += string.Format("AND [Address] like '%{0}%'", model.Country);
                PepDilisenceSub += string.Format("AND Address='{0}'", model.Country);
            }

            if (model.SourceType?.ToLower()== "ofac")
            {

                 Query = string.Format(@"SELECT os.Id, OM.FirstName,OM.LastName,OM.[Type],OS.SdnType as EntityType,os.DateOfBirthList as DateOfBirth,OS.AddressList as Address,null as AliasName,'Ofac' as Source
                                            FROM OfacSanction OS
                                            INNER JOIN
                                            (
                                                SELECT 
                                                    id,
                                                    RefId,
		                                            FirstName,
		                                            LastName,
                                                    STRING_AGG([Type], '|') AS [Type]
                                                FROM
                                                (
                                                    SELECT *, 'NoNSounDex' AS [Type]
                                                    FROM SanctionNameInfo
                                                    WHERE 1=1 AND (FirstName LIKE '{0}%' 
                                                       OR LastName LIKE '{0}%') AND SourceType='Ofac'

                                                    UNION ALL

                                                    SELECT *, 'SounDex' AS [Type]
                                                    FROM SanctionNameInfo
                                                    WHERE (SOUNDEX(FirstName) = SOUNDEX('{0}')
                                                       OR SOUNDEX(LastName) = SOUNDEX('{0}')) AND SourceType='Ofac'
                                                ) x
                                                GROUP BY id, RefId,FirstName,LastName
                                            ) OM
                                                ON OS.Id = OM.RefId where 1=1 {1};

                                            ", model.Name, OfacSub);
            }
            else if (model.SourceType?.ToLower() == "uk")
            {

                Query = string.Format(@"select * from SanctionNameInfo where 1=2;
                            select UK.Id,SNI.FirstName,SNI.LastName,SNI.ThirdName,SNI.[Type],IndividualEntityShip as EntityType,IndividualDetails,null as AliasName,'UK' as Source from UKSanction UK

                            INNER JOIN 
                            (
                            SELECT 
                                id,
                                RefId,
	                            FirstName,
	                            LastName,
	                            ThirdName,
                                STRING_AGG([Type], '|') AS [Type]
                            FROM
                            (
	                            SELECT *, 'NoNSounDex' AS [Type]
	                            FROM SanctionNameInfo
	                            WHERE 1=1 AND (FirstName LIKE '{0}%' 
		                            OR LastName LIKE '{0}%' OR ThirdName LIKE '{0}%') AND SourceType='UK'

	                            UNION ALL

	                            SELECT *, 'SounDex' AS [Type]
	                            FROM SanctionNameInfo
	                            WHERE (SOUNDEX(FirstName) = SOUNDEX('{0}')
		                            OR SOUNDEX(LastName) = SOUNDEX('{0}') OR SOUNDEX(ThirdName) = SOUNDEX('{0}')) AND SourceType='UK'
		                            ) x
                            GROUP BY id, RefId,FirstName,LastName,ThirdName) SNI on SNI.RefId=uk.Id where 1=1 {1}", model.Name, UKSub);
            }

            else if (model.SourceType?.ToLower() == "un" && model.EntityType?.ToLower() != "Entity")
            {

                Query = string.Format(@"select * from SanctionNameInfo where 1=2;select * from SanctionNameInfo where 1=2;

                                        select  UN.Id, UN.FirstName,UN.SecondName,UN.ThirdName,UN.FourthName,SNI.Aliases as AliasName,UN.Aliases,SNI.[Type],UN.IndividualDateOfBirth as DateOfBirth,UN.IndividualPlaceOfBirth as [Address],'Individual' as EntityType,'UN' as Source from UNSanction UN

                                        INNER JOIN 
                                        (
                                        SELECT 
                                            id,
                                            RefId,
	                                        FirstName,
	                                        LastName,
	                                        ThirdName,
	                                        Aliases,
	
                                            STRING_AGG([Type], '|') AS [Type]
                                        FROM
                                        (
	                                        SELECT *, 'NoNSounDex' AS [Type]
	                                        FROM SanctionNameInfo
	                                        WHERE 1=1 AND (FirstName LIKE '{0}%'  
		                                        OR LastName LIKE '{0}%' OR ThirdName LIKE '{0}%' OR FourthName LIKE '{0}%' OR Aliases LIKE '{0}%') AND SourceType='UN' 

	                                        UNION ALL

	                                        SELECT *, 'SounDex' AS [Type]
	                                        FROM SanctionNameInfo
	                                        WHERE (SOUNDEX(FirstName) = SOUNDEX('{0}')
		                                        OR SOUNDEX(LastName) = SOUNDEX('{0}') OR SOUNDEX(ThirdName) = SOUNDEX('{0}') OR SOUNDEX(FourthName) = SOUNDEX('{0}') OR SOUNDEX(Aliases) = SOUNDEX('{0}')) AND SourceType='UN'
		                                        ) x
                                        GROUP BY id, RefId,FirstName,LastName,ThirdName,Aliases
                                        ) SNI on SNI.RefId=UN.Id where 1 = 1 {1}
                                        ", model.Name, UNSub);
            }
            else
            {
                Query = string.Format(@"SELECT *,'NonSoundex' as [Type]
                                        FROM AMLSource
                                        WHERE 1=1 AND ([Name] LIKE '%{0}%' or Alias_names LIKE '%{0}%'  ) 

	                                    union all

                                        select top 1000 *,'Soundex' as [Type]  from AMLSource aml where 1=1  AND  SOUNDEX([Name]) = SOUNDEX('{0}') AND NOT EXISTS (select *  from AMLSource amlsub where [Name] like '%{0}%' and aml.AmlId=amlsub.AmlId)
                                     {1};", model.Name, PepDilisenceSub);

                Query += string.Format(@"WITH Words AS (
	                                    SELECT value AS Word
	                                    FROM STRING_SPLIT('{0}', ' ')
                                    )

                                    SELECT os.Id, OM.FirstName,OM.LastName,OM.[Type],OS.SdnType as EntityType,os.DateOfBirthList as DateOfBirth,OS.AddressList as Address,null as AliasName,'Ofac' as Source
	                                    FROM OfacSanction OS
	                                    inner JOIN
	                                    (
		                                    SELECT 
			                                    id,
			                                    RefId,
			                                    FirstName,
			                                    LastName,
			                                    STRING_AGG([Type], '|') AS [Type]
		                                    FROM
		                                    (
	
			                                    SELECT  s.*, 'SounDex' AS [Type]
			                                    FROM SanctionNameInfo s
			                                    CROSS APPLY Words w
			                                    WHERE s.SourceType = 'Ofac-SDN'
				                                    AND (
					                                    s.FirstName LIKE '%' + w.Word + '%'
					                                    OR s.LastName LIKE '%' + w.Word + '%'
					                                    OR SOUNDEX(s.FirstName) = SOUNDEX(w.Word)
					                                    OR SOUNDEX(s.LastName) = SOUNDEX(w.Word)
					                                    )
		                                    ) x
		                                    GROUP BY id, RefId,FirstName,LastName
	                                    ) OM
		                                    ON OS.Uid = OM.RefId where 1=1 {1};", model.Name, OfacSub);

                Query += string.Format(@"select UK.Id,SNI.FirstName,SNI.LastName,SNI.ThirdName,SNI.[Type],IndividualEntityShip as EntityType,IndividualDetails,null as AliasName,'UK' as Source from UKSanction UK

				                INNER JOIN 
				                (
				                SELECT 
					                id,
					                RefId,
					                FirstName,
					                LastName,
					                ThirdName,
					                STRING_AGG([Type], '|') AS [Type]
				                FROM
				                (
					                SELECT *, 'NoNSounDex' AS [Type]
					                FROM SanctionNameInfo
					                WHERE 1=1 AND (FirstName LIKE '%{0}%' 
						                OR LastName LIKE '%{0}%' OR ThirdName LIKE '%{0}%') AND SourceType='UK'

					                UNION ALL

					                SELECT *, 'SounDex' AS [Type]
					                FROM SanctionNameInfo
					                WHERE (SOUNDEX(FirstName) = SOUNDEX('{0}')
						                OR SOUNDEX(LastName) = SOUNDEX('{0}') OR SOUNDEX(ThirdName) = SOUNDEX('{0}')) AND SourceType='UK'
						                ) x
				                GROUP BY id, RefId,FirstName,LastName,ThirdName) SNI on SNI.RefId=uk.Id where 1=1 {1};", model.Name, UKSub);


                if(model.EntityType?.ToLower() != "Entity")
                {

                    Query += string.Format(@"select  UN.Id, UN.FirstName,UN.SecondName,UN.ThirdName,UN.FourthName,SNI.Aliases as AliasName,UN.Aliases,SNI.[Type],UN.IndividualDateOfBirth as DateOfBirth,UN.IndividualPlaceOfBirth as [Address],'Individual' as EntityType,'UN' as Source from UNSanction UN

                                        INNER JOIN 
                                        (
                                        SELECT 
                                            id,
                                            RefId,
	                                        FirstName,
	                                        LastName,
	                                        ThirdName,
	                                        Aliases,
	
                                            STRING_AGG([Type], '|') AS [Type]
                                        FROM
                                        (
	                                        SELECT *, 'NoNSounDex' AS [Type]
	                                        FROM SanctionNameInfo
	                                        WHERE 1=1 AND (FirstName LIKE '%{0}%'  
		                                        OR LastName LIKE '%{0}%' OR ThirdName LIKE '%{0}%' OR FourthName LIKE '%{0}%' OR Aliases LIKE '%{0}%') AND SourceType='UN' 

	                                        UNION ALL

	                                        SELECT *, 'SounDex' AS [Type]
	                                        FROM SanctionNameInfo
	                                        WHERE (SOUNDEX(FirstName) = SOUNDEX('{0}')
		                                        OR SOUNDEX(LastName) = SOUNDEX('{0}') OR SOUNDEX(ThirdName) = SOUNDEX('{0}') OR SOUNDEX(FourthName) = SOUNDEX('{0}') OR SOUNDEX(Aliases) = SOUNDEX('{0}')) AND SourceType='UN'
		                                        ) x
                                        GROUP BY id, RefId,FirstName,LastName,ThirdName,Aliases
                                        ) SNI on SNI.RefId=UN.Id where 1 = 1 {1}
                                        ", model.Name, UNSub);
                }

            }

                List<CommonSearchResult> results = new List<CommonSearchResult>();

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
                        //Dilisense
                        while (reader.Read())
                        {
                            var item = new CommonSearchResult
                            {
                                Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                                FirstName = reader["Name"]?.ToString(),
                                SecondName = string.Join(", ", (JsonSerializer.Deserialize<List<string>>(reader["lastnames"].ToString())) ?? new List<string>()),
                                ThirdName = string.Join(", ", (JsonSerializer.Deserialize<List<string>>(reader["GivenNames"].ToString())) ?? new List<string>()),
                                FourthName = string.Join(", ", (JsonSerializer.Deserialize<List<string>>(reader["AliasGivenNames"].ToString())) ?? new List<string>()),
                                EntityType = reader["EntityType"]?.ToString(),
                                DateOfBirth = string.Join(", ", (JsonSerializer.Deserialize<List<string>>(reader["dateofbirth"].ToString())) ?? new List<string>()),
                                Address = string.Join(", ", (JsonSerializer.Deserialize<List<string>>(reader["Address"].ToString())) ?? new List<string>()),
                                //Country = string.Join(", ",
                                //                    (JsonSerializer.Deserialize<List<Address>>(reader["Address"].ToString()) ?? new List<Address>())
                                //                        .Where(a => !string.IsNullOrWhiteSpace(a?.Country))
                                //                        .Select(a => a.Country!.Trim())
                                //                        .Distinct(StringComparer.OrdinalIgnoreCase)
                                //                ),
                                Aliases = string.Join(", ", (JsonSerializer.Deserialize<List<string>>(reader["Alias_names"].ToString())) ?? new List<string>()),
                                DataSource = "Dilisense",
                                SourceType = reader["SourceType"]?.ToString(),
                                Guid = model.Guid
                            };
                            var lst = new List<string?>
                            {
                                item.FirstName,
                                item.SecondName,
                            };

                            item.Score = item.Type == "NoNSounDex"  ? 100 : fuzzyNameMatcher.GetBestMatchPercentage(model.Name, lst);
                            results.Add(item);
                        }
                        //Ofac Reader
                        if (reader.NextResult())
                        {

                            while (reader.Read())
                            { 
                                var item = new CommonSearchResult
                                {
                                    Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                                    FirstName = reader["FirstName"]?.ToString(),
                                    SourceType = "SANCTION",
                                    SecondName = reader["LastName"]?.ToString(),
                                    EntityType = reader["EntityType"]?.ToString(),
                                    DateOfBirth = string.Join(", ", JsonSerializer.Deserialize<List<DateOfBirthItem>>(reader["DateOfBirth"].ToString()).Select(a => $"{a.DateOfBirth}")),
                                    Address = string.Join(", ", JsonSerializer.Deserialize<List<Address>>(reader["Address"].ToString()).Select(a => $"{a.Address1}, {a.City}, {a.PostalCode}, {a.Country}")),
                                    Country = string.Join(", ",
                                                        (JsonSerializer.Deserialize<List<Address>>(reader["Address"].ToString()) ?? new List<Address>())
                                                            .Where(a => !string.IsNullOrWhiteSpace(a?.Country))
                                                            .Select(a => a.Country!.Trim())
                                                            .Distinct(StringComparer.OrdinalIgnoreCase)
                                                    ),
                                    Aliases = reader["AliasName"]?.ToString(),
                                    DataSource = reader["Source"]?.ToString(),
                                    Type = reader["Type"]?.ToString(),
                                    Guid = model.Guid
    
                                };
                                var lst = new List<string?>
                                {
                                    item.FirstName,
                                    item.SecondName,
                                };

                                //item.Score = item.Type == "NoNSounDex|SounDex" || item.Type == "NoNSounDex" ? 100 : fuzzyNameMatcher.GetBestMatchPercentage(model.Name, lst);
                                item.Score = OfacNameMatcher.ComputeScore(model.Name, item.FirstName + " " + item.SecondName);
                                //item.Score = fuzzyNameMatcher.GetBestMatchPercentage(model.Name, lst);


                                results.Add(item);
                            }
                        }
                        //UK Reader
                        if (reader.NextResult())
                        { 
                            while (reader.Read())
                            {
                                var item = new CommonSearchResult
                                {
                                    Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                                    FirstName = reader["FirstName"]?.ToString(),
                                    SecondName = reader["LastName"]?.ToString(),
                                    ThirdName = reader["ThirdName"]?.ToString(),
                                    SourceType = "SANCTION",
                                    EntityType = reader["EntityType"]?.ToString(),
                                    DateOfBirth = string.IsNullOrWhiteSpace(reader["IndividualDetails"]?.ToString()) ? string.Empty : SafeExtractDOB(reader["IndividualDetails"].ToString()),
                                    Address = string.IsNullOrWhiteSpace(reader["IndividualDetails"]?.ToString()) ? string.Empty : SafeExtractAddress(reader["IndividualDetails"].ToString()),
                                    Country = string.IsNullOrWhiteSpace(reader["IndividualDetails"]?.ToString()) ? string.Empty : SafeExtractCountry(reader["IndividualDetails"].ToString()),
                                    Aliases = reader["AliasName"]?.ToString(),
                                    DataSource = reader["Source"]?.ToString(),
                                    Type = reader["Type"]?.ToString(),
                                    Guid = model.Guid

                                };
                                var lst = new List<string?>
                                {
                                    item.FirstName,
                                    item.SecondName,
                                    item.ThirdName
                                };
                                item.Score = item.Type == "NoNSounDex|SounDex" || item.Type == "NoNSounDex" ? 100 : fuzzyNameMatcher.GetBestMatchPercentage(model.Name, lst);
                              //  item.Score = OfacNameMatcher.ComputeScore(model.Name, item.FirstName + " " + item.SecondName + item.ThirdName);

                                results.Add(item);
                            }
                        }
                        //UN Reader
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                var item = new CommonSearchResult
                                {
                                    Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                                    FirstName = reader["FirstName"]?.ToString(),
                                    SecondName = reader["SecondName"]?.ToString(),
                                    ThirdName = reader["ThirdName"]?.ToString(),
                                    FourthName = reader["FourthName"]?.ToString(),
                                    SourceType = "SANCTION",
                                    Aliases = string.IsNullOrWhiteSpace(reader["Aliases"]?.ToString()) ? string.Empty : SafeExtractUNAliasName(reader["Aliases"].ToString()),
                                    EntityType = reader["EntityType"]?.ToString(),
                                    DateOfBirth = string.IsNullOrWhiteSpace(reader["DateOfBirth"]?.ToString()) ? string.Empty : SafeExtractDOBUN(reader["DateOfBirth"].ToString()),
                                    Address = string.IsNullOrWhiteSpace(reader["Address"]?.ToString()) ? string.Empty : SafeExtractUNAddress(reader["Address"].ToString()),
                                    Country = string.IsNullOrWhiteSpace(reader["Address"]?.ToString()) ? string.Empty : SafeExtractUNCountry(reader["Address"].ToString()),
                                    DataSource = reader["Source"]?.ToString(),
                                    Type = reader["Type"]?.ToString(),
                                    Guid = model.Guid


                                };
                                var lst = new List<string?>
                                {
                                    item.FirstName,
                                    item.SecondName,
                                    item.ThirdName,
                                    item.FourthName,
                                    item.Aliases
                                };
                                item.Score = item.Type == "SounDex|NoNSounDex" || item.Type == "NoNSounDex" ? 100 : fuzzyNameMatcher.GetBestMatchPercentage(model.Name, lst);
                                // item.Score = OfacNameMatcher.ComputeScore(model.Name, item.FirstName + " " + item.SecondName + " "+ item.ThirdName + " " + item.FourthName + " " + item.Aliases);

                                results.Add(item);
                            }
                        }
                    }
                }
                //Address
                //results = (from DataRow dr in dsResult.Tables[0].Rows
                //           select new CommonSearchResult()
                //           {
                //               Id = dr["Id"] != DBNull.Value ? Convert.ToInt32(dr["Id"]) : 0,
                //               Name = dr["Name"].ToString(),
                //               EntityType = dr["EntityType"].ToString(),
                //               DateOfBirth = dr["DateOfBirth"].ToString(),
                //               Address= string.Join(", ", JsonSerializer.Deserialize<List<Address>>(dr["Address"].ToString()).Select(a => $"{a.Address1}, {a.City}, {a.PostalCode}, {a.Country}")),
                //               Aliases = dr["AliasName"].ToString(),
                //               Source = dr["Source"].ToString(),
                //           }).ToList();

                //var res = (from DataRow dr in dsResult.Tables[1].Rows
                //           select new CommonSearchResult()
                //           {
                //               Id = dr["Id"] != DBNull.Value ? Convert.ToInt32(dr["Id"]) : 0,
                //               Name = dr["Name"].ToString(),
                //               EntityType = dr["EntityType"].ToString(),

                //               DateOfBirth = string.IsNullOrWhiteSpace(dr["DateOfBirth"]?.ToString())? string.Empty : SafeExtractDOB(dr["DateOfBirth"].ToString()),


                //               Address = string.IsNullOrWhiteSpace(dr["DateOfBirth"]?.ToString()) ? string.Empty : SafeExtractAddress(dr["DateOfBirth"].ToString()),
                //               Aliases = dr["AliasName"].ToString(),
                //               Source = dr["Source"].ToString(),
                //           }).ToList();

                //results.AddRange(res);

                //var res2 = (from DataRow dr in dsResult.Tables[2].Rows
                //            select new CommonSearchResult()
                //            {
                //                Id = dr["Id"] != DBNull.Value ? Convert.ToInt32(dr["Id"]) : 0,
                //                Name = dr["Name"].ToString(),
                //                EntityType = dr["EntityType"].ToString(),
                //                DateOfBirth = string.IsNullOrWhiteSpace(dr["DateOfBirth"]?.ToString()) ? string.Empty : SafeExtractDOBUN(dr["DateOfBirth"].ToString()),
                //                Address = string.IsNullOrWhiteSpace(dr["Address"]?.ToString()) ? string.Empty : SafeExtractUNAddress(dr["Address"].ToString()),
                //                Aliases = string.IsNullOrWhiteSpace(dr["AliasName"]?.ToString()) ? string.Empty : SafeExtractUNAliasName(dr["AliasName"].ToString()),
                //                Source = dr["Source"].ToString(),
                //            }).ToList();

                //results.AddRange(res2);
            }
            catch (Exception ex)
            {
            }
            return results;
        }

        public async Task<List<CommonSearchResult?>> GetCommonSearchForExcel(AMLFilter model)
        {

            string OfacSub = string.Empty;
            string UKSub = string.Empty;
            string UNSub = string.Empty;
            string SoundMatch = string.Empty;
            string PepDilisenceSub = string.Empty;


            string Query = string.Empty;



            if (!string.IsNullOrWhiteSpace(model.EntityType))
            {
                OfacSub += string.Format("AND OS.SdnType='{0}'", model.EntityType);
                UKSub += string.Format("AND IndividualEntityShip='{0}'", model.EntityType);
                PepDilisenceSub += string.Format("AND EntityType='{0}'", model.EntityType);

            }
            if (!string.IsNullOrWhiteSpace(model.DateOfBirth))
            {
                OfacSub += string.Format("AND OS.DateOfBirthList like '%{0}%'", model.DateOfBirth);
                UKSub += string.Format("AND IndividualDetails like '%{0}%'", model.DateOfBirth);
                UNSub += string.Format("AND IndividualDateOfBirth like '%{0}%'", model.DateOfBirth);
                PepDilisenceSub += string.Format("AND DateOfBirth like '%{0}%'", model.DateOfBirth);

            }
            if (!string.IsNullOrWhiteSpace(model.Country))
            {
                OfacSub += string.Format("AND OS.AddressList like '%{0}%'", model.Country);
                UKSub += string.Format("AND IndividualDetails like '%{0}%'", model.Country);
                UNSub += string.Format("AND [Address] like '%{0}%'", model.Country);
                PepDilisenceSub += string.Format("AND Address like '%{0}%'", model.Country);

            }
            if (!string.IsNullOrWhiteSpace(model.Address))
            {
                OfacSub += string.Format("AND OS.AddressList like '%{0}%'", model.Address);
                UKSub += string.Format("AND IndividualDetails like '%{0}%'", model.Address);
                UNSub += string.Format("AND [Address] like '%{0}%'", model.Address);
            }
            if (!string.IsNullOrWhiteSpace(model.City))
            {
                OfacSub += string.Format("AND OS.AddressList like '%{0}%'", model.City);
                UNSub += string.Format("AND [Address] like '%{0}%'", model.City);
            }
            if (!string.IsNullOrWhiteSpace(model.PostCode))
            {
                OfacSub += string.Format("AND OS.AddressList like '%{0}%'", model.PostCode);
            }
            if (!string.IsNullOrWhiteSpace(model.State))
            {
                UNSub += string.Format("AND [Address] like '%{0}%'", model.State);
            }


            if (model.SourceType?.ToLower() == "ofac")
            {

                Query = string.Format(@"SELECT os.Id, OM.FirstName,OM.LastName,OM.[Type],OS.SdnType as EntityType,os.DateOfBirthList as DateOfBirth,OS.AddressList as Address,null as AliasName,'Ofac' as Source
                                            FROM OfacSanction OS
                                            INNER JOIN
                                            (
                                                SELECT 
                                                    id,
                                                    RefId,
		                                            FirstName,
		                                            LastName,
                                                    STRING_AGG([Type], '|') AS [Type]
                                                FROM
                                                (
                                                    SELECT *, 'NoNSounDex' AS [Type]
                                                    FROM SanctionNameInfo
                                                    WHERE 1=1 AND (FirstName LIKE '{0}%' 
                                                       OR LastName LIKE '{0}%') AND SourceType='Ofac'

                                                    UNION ALL

                                                    SELECT *, 'SounDex' AS [Type]
                                                    FROM SanctionNameInfo
                                                    WHERE (SOUNDEX(FirstName) = SOUNDEX('{0}')
                                                       OR SOUNDEX(LastName) = SOUNDEX('{0}')) AND SourceType='Ofac'
                                                ) x
                                                GROUP BY id, RefId,FirstName,LastName
                                            ) OM
                                                ON OS.Id = OM.RefId where 1=1 {1};

                                            ", model.Name, OfacSub);
            }
            else if (model.SourceType?.ToLower() == "uk")
            {

                Query = string.Format(@"select * from SanctionNameInfo where 1=2;
                            select UK.Id,SNI.FirstName,SNI.LastName,SNI.ThirdName,SNI.[Type],IndividualEntityShip as EntityType,IndividualDetails,null as AliasName,'UK' as Source from UKSanction UK

                            INNER JOIN 
                            (
                            SELECT 
                                id,
                                RefId,
	                            FirstName,
	                            LastName,
	                            ThirdName,
                                STRING_AGG([Type], '|') AS [Type]
                            FROM
                            (
	                            SELECT *, 'NoNSounDex' AS [Type]
	                            FROM SanctionNameInfo
	                            WHERE 1=1 AND (FirstName LIKE '{0}%' 
		                            OR LastName LIKE '{0}%' OR ThirdName LIKE '{0}%') AND SourceType='UK'

	                            UNION ALL

	                            SELECT *, 'SounDex' AS [Type]
	                            FROM SanctionNameInfo
	                            WHERE (SOUNDEX(FirstName) = SOUNDEX('{0}')
		                            OR SOUNDEX(LastName) = SOUNDEX('{0}') OR SOUNDEX(ThirdName) = SOUNDEX('{0}')) AND SourceType='UK'
		                            ) x
                            GROUP BY id, RefId,FirstName,LastName,ThirdName) SNI on SNI.RefId=uk.Id where 1=1 {1}", model.Name, UKSub);
            }

            else if (model.SourceType?.ToLower() == "un" && model.EntityType?.ToLower() != "Entity")
            {

                Query = string.Format(@"select * from SanctionNameInfo where 1=2;select * from SanctionNameInfo where 1=2;

                                        select  UN.Id, UN.FirstName,UN.SecondName,UN.ThirdName,UN.FourthName,SNI.Aliases as AliasName,UN.Aliases,SNI.[Type],UN.IndividualDateOfBirth as DateOfBirth,UN.IndividualPlaceOfBirth as [Address],'Individual' as EntityType,'UN' as Source from UNSanction UN

                                        INNER JOIN 
                                        (
                                        SELECT 
                                            id,
                                            RefId,
	                                        FirstName,
	                                        LastName,
	                                        ThirdName,
	                                        Aliases,
	
                                            STRING_AGG([Type], '|') AS [Type]
                                        FROM
                                        (
	                                        SELECT *, 'NoNSounDex' AS [Type]
	                                        FROM SanctionNameInfo
	                                        WHERE 1=1 AND (FirstName LIKE '{0}%'  
		                                        OR LastName LIKE '{0}%' OR ThirdName LIKE '{0}%' OR FourthName LIKE '{0}%' OR Aliases LIKE '{0}%') AND SourceType='UN' 

	                                        UNION ALL

	                                        SELECT *, 'SounDex' AS [Type]
	                                        FROM SanctionNameInfo
	                                        WHERE (SOUNDEX(FirstName) = SOUNDEX('{0}')
		                                        OR SOUNDEX(LastName) = SOUNDEX('{0}') OR SOUNDEX(ThirdName) = SOUNDEX('{0}') OR SOUNDEX(FourthName) = SOUNDEX('{0}') OR SOUNDEX(Aliases) = SOUNDEX('{0}')) AND SourceType='UN'
		                                        ) x
                                        GROUP BY id, RefId,FirstName,LastName,ThirdName,Aliases
                                        ) SNI on SNI.RefId=UN.Id where 1 = 1 {1}
                                        ", model.Name, UNSub);
            }
            else
            {
                Query = string.Format(@"SELECT *,'NonSoundex' as [Type]
                                        FROM AMLSource
                                        WHERE 1=1 AND ([Name] LIKE '%{0}%' or Alias_names LIKE '%{0}%') {1}

	                                    union all

                                        select top 1000 *,'Soundex' as [Type]  from AMLSource aml where 1=1  AND  SOUNDEX([Name]) = SOUNDEX('{0}') AND NOT EXISTS (select *  from AMLSource amlsub where [Name] like '%{0}%' and aml.AmlId=amlsub.AmlId)
                                     {1};", model.Name, PepDilisenceSub);

                Query += string.Format(@"WITH Words AS (
	                                    SELECT value AS Word
	                                    FROM STRING_SPLIT('{0}', ' ')
                                    )

                                    SELECT os.Id, OM.FirstName,OM.LastName,OM.[Type],OS.SdnType as EntityType,os.DateOfBirthList as DateOfBirth,OS.AddressList as Address,null as AliasName,'Ofac' as Source
	                                    FROM OfacSanction OS
	                                    inner JOIN
	                                    (
		                                    SELECT 
			                                    id,
			                                    RefId,
			                                    FirstName,
			                                    LastName,
			                                    STRING_AGG([Type], '|') AS [Type]
		                                    FROM
		                                    (
	
			                                    SELECT  s.*, 'SounDex' AS [Type]
			                                    FROM SanctionNameInfo s
			                                    CROSS APPLY Words w
			                                    WHERE s.SourceType = 'Ofac-SDN'
				                                    AND (
					                                    s.FirstName LIKE '%' + w.Word + '%'
					                                    OR s.LastName LIKE '%' + w.Word + '%'
					                                    OR SOUNDEX(s.FirstName) = SOUNDEX(w.Word)
					                                    OR SOUNDEX(s.LastName) = SOUNDEX(w.Word)
					                                    )
		                                    ) x
		                                    GROUP BY id, RefId,FirstName,LastName
	                                    ) OM
		                                    ON OS.Uid = OM.RefId where 1=1 {1};", model.Name, OfacSub);

                Query += string.Format(@"select UK.Id,SNI.FirstName,SNI.LastName,SNI.ThirdName,SNI.[Type],IndividualEntityShip as EntityType,IndividualDetails,null as AliasName,'UK' as Source from UKSanction UK

				                INNER JOIN 
				                (
				                SELECT 
					                id,
					                RefId,
					                FirstName,
					                LastName,
					                ThirdName,
					                STRING_AGG([Type], '|') AS [Type]
				                FROM
				                (
					                SELECT *, 'NoNSounDex' AS [Type]
					                FROM SanctionNameInfo
					                WHERE 1=1 AND (FirstName LIKE '%{0}%' 
						                OR LastName LIKE '%{0}%' OR ThirdName LIKE '%{0}%') AND SourceType='UK'

					                UNION ALL

					                SELECT *, 'SounDex' AS [Type]
					                FROM SanctionNameInfo
					                WHERE (SOUNDEX(FirstName) = SOUNDEX('{0}')
						                OR SOUNDEX(LastName) = SOUNDEX('{0}') OR SOUNDEX(ThirdName) = SOUNDEX('{0}')) AND SourceType='UK'
						                ) x
				                GROUP BY id, RefId,FirstName,LastName,ThirdName) SNI on SNI.RefId=uk.Id where 1=1 {1};", model.Name, UKSub);


                if (model.EntityType?.ToLower() != "Entity")
                {

                    Query += string.Format(@"select  UN.Id, UN.FirstName,UN.SecondName,UN.ThirdName,UN.FourthName,SNI.Aliases as AliasName,UN.Aliases,SNI.[Type],UN.IndividualDateOfBirth as DateOfBirth,UN.IndividualPlaceOfBirth as [Address],'Individual' as EntityType,'UN' as Source from UNSanction UN

                                        INNER JOIN 
                                        (
                                        SELECT 
                                            id,
                                            RefId,
	                                        FirstName,
	                                        LastName,
	                                        ThirdName,
	                                        Aliases,
	
                                            STRING_AGG([Type], '|') AS [Type]
                                        FROM
                                        (
	                                        SELECT *, 'NoNSounDex' AS [Type]
	                                        FROM SanctionNameInfo
	                                        WHERE 1=1 AND (FirstName LIKE '%{0}%'  
		                                        OR LastName LIKE '%{0}%' OR ThirdName LIKE '%{0}%' OR FourthName LIKE '%{0}%' OR Aliases LIKE '%{0}%') AND SourceType='UN' 

	                                        UNION ALL

	                                        SELECT *, 'SounDex' AS [Type]
	                                        FROM SanctionNameInfo
	                                        WHERE (SOUNDEX(FirstName) = SOUNDEX('{0}')
		                                        OR SOUNDEX(LastName) = SOUNDEX('{0}') OR SOUNDEX(ThirdName) = SOUNDEX('{0}') OR SOUNDEX(FourthName) = SOUNDEX('{0}') OR SOUNDEX(Aliases) = SOUNDEX('{0}')) AND SourceType='UN'
		                                        ) x
                                        GROUP BY id, RefId,FirstName,LastName,ThirdName,Aliases
                                        ) SNI on SNI.RefId=UN.Id where 1 = 1 {1}
                                        ", model.Name, UNSub);
                }

            }

            List<CommonSearchResult> results = new List<CommonSearchResult>();

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
                        //Dilisense
                        while (reader.Read())
                        {
                            var item = new CommonSearchResult
                            {
                                Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                                FirstName = reader["Name"]?.ToString(),
                                SecondName = string.Join(", ", (JsonSerializer.Deserialize<List<string>>(reader["lastnames"].ToString())) ?? new List<string>()),
                                ThirdName = string.Join(", ", (JsonSerializer.Deserialize<List<string>>(reader["GivenNames"].ToString())) ?? new List<string>()),
                                FourthName = string.Join(", ", (JsonSerializer.Deserialize<List<string>>(reader["AliasGivenNames"].ToString())) ?? new List<string>()),
                                EntityType = reader["EntityType"]?.ToString(),
                                DateOfBirth = string.Join(", ", (JsonSerializer.Deserialize<List<string>>(reader["dateofbirth"].ToString())) ?? new List<string>()),
                                Address = string.Join(", ", (JsonSerializer.Deserialize<List<string>>(reader["Address"].ToString())) ?? new List<string>()),
                                //Country = string.Join(", ",
                                //                    (JsonSerializer.Deserialize<List<Address>>(reader["Address"].ToString()) ?? new List<Address>())
                                //                        .Where(a => !string.IsNullOrWhiteSpace(a?.Country))
                                //                        .Select(a => a.Country!.Trim())
                                //                        .Distinct(StringComparer.OrdinalIgnoreCase)
                                //                ),
                                Aliases = string.Join(", ", (JsonSerializer.Deserialize<List<string>>(reader["Alias_names"].ToString())) ?? new List<string>()),
                                DataSource = "Dilisense",
                                SourceType = reader["SourceType"]?.ToString(),
                                Guid = model.Guid
                            };
                            var lst = new List<string?>
                            {
                                item.FirstName,
                                item.SecondName,
                            };

                            item.Score = item.Type == "NoNSounDex" ? 100 : fuzzyNameMatcher.GetBestMatchPercentage(model.Name, lst);
                            results.Add(item);
                        }
                        //Ofac Reader
                        if (reader.NextResult())
                        {

                            while (reader.Read())
                            {
                                var item = new CommonSearchResult
                                {
                                    Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                                    FirstName = reader["FirstName"]?.ToString(),
                                    SourceType = "SANCTION",
                                    SecondName = reader["LastName"]?.ToString(),
                                    EntityType = reader["EntityType"]?.ToString(),
                                    DateOfBirth = string.Join(", ", JsonSerializer.Deserialize<List<DateOfBirthItem>>(reader["DateOfBirth"].ToString()).Select(a => $"{a.DateOfBirth}")),
                                    Address = string.Join(", ", JsonSerializer.Deserialize<List<Address>>(reader["Address"].ToString()).Select(a => $"{a.Address1}, {a.City}, {a.PostalCode}, {a.Country}")),
                                    Country = string.Join(", ",
                                                        (JsonSerializer.Deserialize<List<Address>>(reader["Address"].ToString()) ?? new List<Address>())
                                                            .Where(a => !string.IsNullOrWhiteSpace(a?.Country))
                                                            .Select(a => a.Country!.Trim())
                                                            .Distinct(StringComparer.OrdinalIgnoreCase)
                                                    ),
                                    Aliases = reader["AliasName"]?.ToString(),
                                    DataSource = reader["Source"]?.ToString(),
                                    Type = reader["Type"]?.ToString(),
                                    Guid = model.Guid

                                };
                                var lst = new List<string?>
                                {
                                    item.FirstName,
                                    item.SecondName,
                                };

                                //item.Score = item.Type == "NoNSounDex|SounDex" || item.Type == "NoNSounDex" ? 100 : fuzzyNameMatcher.GetBestMatchPercentage(model.Name, lst);
                                item.Score = OfacNameMatcher.ComputeScore(model.Name, item.FirstName + " " + item.SecondName);
                                //item.Score = fuzzyNameMatcher.GetBestMatchPercentage(model.Name, lst);


                                results.Add(item);
                            }
                        }
                        //UK Reader
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                var item = new CommonSearchResult
                                {
                                    Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                                    FirstName = reader["FirstName"]?.ToString(),
                                    SecondName = reader["LastName"]?.ToString(),
                                    ThirdName = reader["ThirdName"]?.ToString(),
                                    SourceType = "SANCTION",
                                    EntityType = reader["EntityType"]?.ToString(),
                                    DateOfBirth = string.IsNullOrWhiteSpace(reader["IndividualDetails"]?.ToString()) ? string.Empty : SafeExtractDOB(reader["IndividualDetails"].ToString()),
                                    Address = string.IsNullOrWhiteSpace(reader["IndividualDetails"]?.ToString()) ? string.Empty : SafeExtractAddress(reader["IndividualDetails"].ToString()),
                                    Country = string.IsNullOrWhiteSpace(reader["IndividualDetails"]?.ToString()) ? string.Empty : SafeExtractCountry(reader["IndividualDetails"].ToString()),
                                    Aliases = reader["AliasName"]?.ToString(),
                                    DataSource = reader["Source"]?.ToString(),
                                    Type = reader["Type"]?.ToString(),
                                    Guid = model.Guid

                                };
                                var lst = new List<string>
                                {
                                    item.FirstName,
                                    item.SecondName,
                                    item.ThirdName
                                };
                                item.Score = item.Type == "NoNSounDex|SounDex" || item.Type == "NoNSounDex" ? 100 : fuzzyNameMatcher.GetBestMatchPercentage(model.Name, lst);
                                //  item.Score = OfacNameMatcher.ComputeScore(model.Name, item.FirstName + " " + item.SecondName + item.ThirdName);

                                results.Add(item);
                            }
                        }
                        //UN Reader
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                var item = new CommonSearchResult
                                {
                                    Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                                    FirstName = reader["FirstName"]?.ToString(),
                                    SecondName = reader["SecondName"]?.ToString(),
                                    ThirdName = reader["ThirdName"]?.ToString(),
                                    FourthName = reader["FourthName"]?.ToString(),
                                    SourceType = "SANCTION",
                                    Aliases = string.IsNullOrWhiteSpace(reader["Aliases"]?.ToString()) ? string.Empty : SafeExtractUNAliasName(reader["Aliases"].ToString()),
                                    EntityType = reader["EntityType"]?.ToString(),
                                    DateOfBirth = string.IsNullOrWhiteSpace(reader["DateOfBirth"]?.ToString()) ? string.Empty : SafeExtractDOBUN(reader["DateOfBirth"].ToString()),
                                    Address = string.IsNullOrWhiteSpace(reader["Address"]?.ToString()) ? string.Empty : SafeExtractUNAddress(reader["Address"].ToString()),
                                    Country = string.IsNullOrWhiteSpace(reader["Address"]?.ToString()) ? string.Empty : SafeExtractUNCountry(reader["Address"].ToString()),
                                    DataSource = reader["Source"]?.ToString(),
                                    Type = reader["Type"]?.ToString(),
                                    Guid = model.Guid


                                };
                                var lst = new List<string?>
                                {
                                    item.FirstName,
                                    item.SecondName,
                                    item.ThirdName,
                                    item.FourthName,
                                    item.Aliases
                                };
                                item.Score = item.Type == "SounDex|NoNSounDex" || item.Type == "NoNSounDex" ? 100 : fuzzyNameMatcher.GetBestMatchPercentage(model.Name, lst);
                                // item.Score = OfacNameMatcher.ComputeScore(model.Name, item.FirstName + " " + item.SecondName + " "+ item.ThirdName + " " + item.FourthName + " " + item.Aliases);

                                results.Add(item);
                            }
                        }
                    }
                }
                //Address
                //results = (from DataRow dr in dsResult.Tables[0].Rows
                //           select new CommonSearchResult()
                //           {
                //               Id = dr["Id"] != DBNull.Value ? Convert.ToInt32(dr["Id"]) : 0,
                //               Name = dr["Name"].ToString(),
                //               EntityType = dr["EntityType"].ToString(),
                //               DateOfBirth = dr["DateOfBirth"].ToString(),
                //               Address= string.Join(", ", JsonSerializer.Deserialize<List<Address>>(dr["Address"].ToString()).Select(a => $"{a.Address1}, {a.City}, {a.PostalCode}, {a.Country}")),
                //               Aliases = dr["AliasName"].ToString(),
                //               Source = dr["Source"].ToString(),
                //           }).ToList();

                //var res = (from DataRow dr in dsResult.Tables[1].Rows
                //           select new CommonSearchResult()
                //           {
                //               Id = dr["Id"] != DBNull.Value ? Convert.ToInt32(dr["Id"]) : 0,
                //               Name = dr["Name"].ToString(),
                //               EntityType = dr["EntityType"].ToString(),

                //               DateOfBirth = string.IsNullOrWhiteSpace(dr["DateOfBirth"]?.ToString())? string.Empty : SafeExtractDOB(dr["DateOfBirth"].ToString()),


                //               Address = string.IsNullOrWhiteSpace(dr["DateOfBirth"]?.ToString()) ? string.Empty : SafeExtractAddress(dr["DateOfBirth"].ToString()),
                //               Aliases = dr["AliasName"].ToString(),
                //               Source = dr["Source"].ToString(),
                //           }).ToList();

                //results.AddRange(res);

                //var res2 = (from DataRow dr in dsResult.Tables[2].Rows
                //            select new CommonSearchResult()
                //            {
                //                Id = dr["Id"] != DBNull.Value ? Convert.ToInt32(dr["Id"]) : 0,
                //                Name = dr["Name"].ToString(),
                //                EntityType = dr["EntityType"].ToString(),
                //                DateOfBirth = string.IsNullOrWhiteSpace(dr["DateOfBirth"]?.ToString()) ? string.Empty : SafeExtractDOBUN(dr["DateOfBirth"].ToString()),
                //                Address = string.IsNullOrWhiteSpace(dr["Address"]?.ToString()) ? string.Empty : SafeExtractUNAddress(dr["Address"].ToString()),
                //                Aliases = string.IsNullOrWhiteSpace(dr["AliasName"]?.ToString()) ? string.Empty : SafeExtractUNAliasName(dr["AliasName"].ToString()),
                //                Source = dr["Source"].ToString(),
                //            }).ToList();

                //results.AddRange(res2);
            }
            catch (Exception ex)
            {
            }
            return results;
        }



        private static string SafeExtractDOB(string json)
        {
            try
            {
                var data = JsonSerializer.Deserialize<IndividualDetails>(json);
                if (data?.IndividualList == null)
                    return string.Empty;

                var dobList = data.IndividualList
                    .Where(x => x.DOBs?.DOBList != null)
                    .SelectMany(x => x.DOBs.DOBList)
                    .ToList();

                return dobList.Any() ? string.Join(", ", dobList) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string SafeExtractAddress(string json)
        {
            try
            {
                var data = JsonSerializer.Deserialize<IndividualDetails>(json);
                if (data?.IndividualList == null)
                    return string.Empty;

                var AddList = data.IndividualList
                    .Where(x => x.BirthDetails?.LocationList != null)
                    .SelectMany(x => x.BirthDetails.LocationList)
                    .ToList();

                string dd = string.Join("; ", AddList.Select(x => $"{x.TownOfBirth}, {x.CountryOfBirth}"));

                return dd;
            }
            catch
            {
                return string.Empty;
            }
        }
        private static string SafeExtractCountry(string json)
        {
            try
            {
                var data = JsonSerializer.Deserialize<IndividualDetails>(json);
                if (data?.IndividualList == null)
                    return string.Empty;

                var AddList = data.IndividualList
                    .Where(x => x.BirthDetails?.LocationList != null)
                    .SelectMany(x => x.BirthDetails.LocationList)
                    .ToList();

                string dd = string.Join("; ",
                   AddList
                       .Where(x => !string.IsNullOrWhiteSpace(x.CountryOfBirth))   // skip null or empty
                       .Select(x => x.CountryOfBirth.Trim())
                       .Distinct(StringComparer.OrdinalIgnoreCase)
               );


                return dd;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string SafeExtractDOBUN(string json)
        {
            try
            {
                var data = JsonSerializer.Deserialize<List<IndividualDateOfBirthModel>>(json);
                if (data == null)
                    return string.Empty;

                string dd = string.Join("; ", data.Select(x => $"{x.Date}, {x.Year}"));

                return dd;
            }
            catch
            {
                // If invalid JSON or unexpected format — safely ignore
                return string.Empty;
            }
        }

        private static string SafeExtractUNAliasName(string json)
        {
            try
            {
                var data = JsonSerializer.Deserialize<List<AliasModel>>(json);
                if (data == null)
                    return string.Empty;

                string dd = string.Join("; ", data.Select(x => $"{x.AliasName}"));

                return dd;
            }
            catch
            {
                // If invalid JSON or unexpected format — safely ignore
                return string.Empty;
            }
        }

        private static string SafeExtractUNAddress(string json)
        {
            try
            {
                var data = JsonSerializer.Deserialize<List<AddressModel>>(json);
                if (data == null)
                    return string.Empty;

                string dd = string.Join("; ", data.Select(x => $"{x.City}, {x.Country}, {x.Note}, {x.State_Province}, {x.Street}"));

                return dd;
            }
            catch
            {
                // If invalid JSON or unexpected format — safely ignore
                return string.Empty;
            }
        }
        private static string SafeExtractUNCountry(string json)
        {
            try
            {
                var data = JsonSerializer.Deserialize<List<AddressModel>>(json);
                if (data == null)
                    return string.Empty;

                string dd = string.Join("; ", data.Select(x => $"{x.Country}"));

                return dd;
            }
            catch
            {
                // If invalid JSON or unexpected format — safely ignore
                return string.Empty;
            }
        }

        protected DataSet GetDataSet(SqlCommand command)
        {
            return GetDataSet(command, "Default");
        }
        public DataSet GetDataSet(SqlCommand command, string tablename)
        {
            DataSet dataset = new DataSet();
            dataset.Tables.Add(new DataTable(tablename));

            SqlDataAdapter dataadapter = new SqlDataAdapter(command);
            dataadapter.Fill(dataset, tablename);

            return dataset;
        }
    }
}
