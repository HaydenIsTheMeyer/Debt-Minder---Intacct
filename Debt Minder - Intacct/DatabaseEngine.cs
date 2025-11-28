using Microsoft.Win32;
using System.Data;
using System.Data.SqlClient;


namespace Debt_Minder___Intacct
{
    public class DatabaseEngine
    {
        public static SqlConnection DatabaseConnection = new SqlConnection();
        public static string ServerName { get; set; }
        public static string DatabaseName { get; set; }
        public static string UserName { get; set; }
        public static string sPassword { get; set; }
        public static string ConnectionString { get; set; }

        public static void InitialiseConnection()
        {
            sPassword = CryptorEngine.Decrypt((string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\InvoiceRun\DatabaseLogin", "Password", ""), true);
            UserName = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\InvoiceRun\DatabaseLogin", "User", null);
            DatabaseName = "Intacct Debt Minder";//(string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\InvoiceRun\DatabaseLogin", "Database", null);
            ServerName = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\InvoiceRun\DatabaseLogin", "Server", null);
            ConnectionString = $@"data source={ServerName};initial catalog = {DatabaseName};Integrated Security =false ;user={UserName};password={sPassword};MultipleActiveResultSets=True;";
            // string ConnectionString = $@"User ID={UserName};Password={sPassword};Initial Catalog={DatabaseName};Server={ServerName};Encrypt = false;";
            DatabaseConnection = new SqlConnection(ConnectionString);




        }

        public static void ExecuteNonQuery(string query)
        {
            InitialiseConnection();
            try
            {
                DatabaseConnection.Open();
                SqlCommand cmdQuery = new SqlCommand(query, DatabaseConnection);

                cmdQuery.ExecuteNonQuery();
                DatabaseConnection.Close();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
            finally
            {
                if (DatabaseConnection != null)
                {
                    DatabaseConnection.Close();
                }
            }
        }
        public static object ExecuteScalar(string query)
        {
            object value = "";
            InitialiseConnection();
            try
            {
                DatabaseConnection.Open();
                SqlCommand cmdQuery = new SqlCommand(query, DatabaseConnection);

                value = cmdQuery.ExecuteScalar();
                DatabaseConnection.Close();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
            finally
            {
                if (DatabaseConnection != null)
                {
                    DatabaseConnection.Close();
                }
            }
            return value;
        }

        public static DataTable ExecuteDataTable(string Query)
        {
            InitialiseConnection();
            DataTable DataTable = new DataTable();
            try
            {
                DatabaseConnection.Open();
                SqlDataAdapter dtaRecordCount = new SqlDataAdapter(Query, DatabaseConnection);
                dtaRecordCount.SelectCommand.CommandTimeout = 1200;
                dtaRecordCount.Fill(DataTable);
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
            finally
            {
                if (DatabaseConnection != null)
                {
                    DatabaseConnection.Close();
                }
            }
            return DataTable;
        }


        // -----------------------------------------------------------------------------------------------------------------------------------------------------------
        // Sessions
        // -----------------------------------------------------------------------------------------------------------------------------------------------------------

        public static void InsertSession(string Username, Guid guid)
        {
            string query = $@"
			INSERT INTO				[{DatabaseName}].dbo.[KvDM_Sessions] (
									Username,
									LoginTime,
                                    SessionToken) 

			VALUES					('{Username}',
									GETDATE(),
                                    '{guid}')";

            ExecuteNonQuery(query);
        }

        public static object ValidateSession(string Username)
        {
            string query = $@"
            DECLARE @Username NVARCHAR(255);
            DECLARE @Result BIT;

            -- Replace 'username_to_check' with the actual username you want to check
            SET @Username = '{Username}';

            -- Check for the condition
            IF EXISTS (
                SELECT 1
                FROM [{DatabaseName}].dbo.KvDM_Sessions
                WHERE username = @Username
                  AND sessionToken IS NOT NULL
            )
            BEGIN
                SET @Result = 0; -- False
            END
            ELSE
            BEGIN
                SET @Result = 1; -- True
            END

            -- Output the result
            SELECT @Result AS IsValid;
";

            return ExecuteScalar(query);
        }

        public static void RemoveSession(string Username)
        {
            string query = $@"
			UPDATE			[{DatabaseName}].dbo.KvDM_Sessions
			SET				SessionToken = null
			WHERE			Username = '{Username}'";

            ExecuteNonQuery(query);
        }

        // -----------------------------------------------------------------------------------------------------------------------------------------------------------
        // End of Sessions
        // -----------------------------------------------------------------------------------------------------------------------------------------------------------



        public static void UpdateInvoiceLogAfterEmail(string OrderNum, string emailFlag, string emailMessage)
        {
            string query = $@"
			update			[{DatabaseName}].dbo.KvDM_InvoiceLog 
			set				emailFlag = '{emailFlag}',
							emailMessage = '{emailMessage}',
							emailDate = GETDATE()
			where			orderNum = '{OrderNum}'
            AND             [User] = '{SessionEngine.Username}'
					";

            ExecuteNonQuery(query);
        }



        public static DataTable GetActiveEmailTemplate()
        {
            string query = $@"
			SELECT			* 
			FROM			[{DatabaseName}].dbo.KvDM_EmailTemplate
			WHERE			bActive = 1
            AND             [User] = '{SessionEngine.Username}'
            ";

            return ExecuteDataTable(query);
        }

        public static void UpdateEmailLog(string folderPath, string EmailFlag, string EmailMessage)
        {
            string query = $@"
			UPDATE				[{DatabaseName}].dbo.KvDM_EmailLog
			SET					EmailFlag = '{EmailFlag}',
								EmailMessage = '{EmailMessage}',
								EmailDate = GETDATE()
			WHERE				FolderPath = '{folderPath}'
            AND                 [User] = '{SessionEngine.Username}'";

            ExecuteNonQuery(query);
        }

        public static DataTable GetPendingEmails()
        {
            string query = $@"
            select          * 
            from            [{DatabaseName}].dbo.KvDM_EmailLog 
            where           (emailFlag = 'p' 
            or              emailFlag = 'f')
            AND             [User] = '{SessionEngine.Username}'";


            return ExecuteDataTable(query);
        }

        public static void ClearInvoiceSelection()
        {
            string query = $@"
            DELETE FROM [{DatabaseName}].dbo.KvDM_InvoiceSelection WHERE SessionToken = '{SessionEngine.Guid}'";

            ExecuteNonQuery(query);
        }

        public static void ClearEmailSelection()
        {
            string query = $@"
            DELETE FROM [{DatabaseName}].dbo.KvDM_EmailSelection WHERE SessionToken = '{SessionEngine.Guid}'";

            ExecuteNonQuery(query);
        }

        public static void ClearStatementSelection()
        {
            string query = $@"
            DELETE FROM [{DatabaseName}].dbo.KvDM_StatementSelection WHERE SessionToken = '{SessionEngine.Guid}'";

            ExecuteNonQuery(query);
        }

        public static void DeleteInternalEmailSelection()
        {
            string query = $@"
			DELETE FROM			[{DatabaseName}].dbo.KvDM_InternalEmailSelection
			WHERE				[User] = '{SessionEngine.Username}'";


            ExecuteNonQuery(query);
        }


        public static DataTable GetSelectedInternalEmails()
        {
            string query = $@"
			SELECT			Reciepient
			FROM			[{DatabaseName}].dbo.KvDM_InternalEmailSelection
			WHERE			[User] = '{SessionEngine.Username}'";

            return ExecuteDataTable(query);
        }

        public static DataTable GetActiveEmailSMTP()
        {
            string query = $@"
			SELECT			* 
			FROM			[{DatabaseName}].dbo.KvDM_EmailSMTP 
			WHERE           bActive = 1
            AND             [User] = '{SessionEngine.Username}'
";

            return ExecuteDataTable(query);
        }

        public static void InsertEmailLog(string folderPath, string reciepients, string EmailFlag, string EmailMessage)
        {
            InitialiseConnection();
            string query = $@"

            Insert into			[{DatabaseName}].dbo.KvDM_EmailLog (
					            FolderPath,
								Reciepients,
					            EmailFlag, 
					            EmailMessage,
                                EmailDate,
                                [User]) 

            SELECT				'{folderPath}',
								'{reciepients}',
					            '{EmailFlag}',
					            '{EmailMessage}',
                                  GETDATE(),
                                '{SessionEngine.Username}'
            WHERE
            NOT EXISTS			(select 1 from [{DatabaseName}].dbo.KvDM_EmailLog where FolderPath = '{folderPath}' AND EmailDate >= DATEADD(day, -1, GETDATE()) AND [User] = '{SessionEngine.Username}')


";

            ExecuteNonQuery(query);
        }
        public static void InsertEmailTemplate(string TemplateName, string Subject, string Body, bool bActive)
        {

            int Active = 0;
            string UpdateIfNewActive = "";
            if (bActive)
            {
                Active = 1;
                UpdateIfNewActive = $@"
                UPDATE                  [{DatabaseName}].dbo.KvDM_EmailTemplate
                SET                     bActive = 0
                WHERE                   [User] = '{SessionEngine.Username}';";
            }

            string UpdateActive = $@"
            {UpdateIfNewActive}
            UPDATE					[{DatabaseName}].dbo.KvDM_EmailTemplate
		    SET						TemplateName = '{TemplateName}',
                                    [Subject] = '{Subject}',
                                    Body = '{Body}',
                                    bActive = {Active}
            WHERE                   TemplateName = '{TemplateName}'
            AND                     [User] = '{SessionEngine.Username}';";

            string query = $@"
            {UpdateActive}
			INSERT INTO				[{DatabaseName}].dbo.KvDM_EmailTemplate (
									TemplateName, 
									[Subject], 
									Body,
									bActive,
                                    [User])

			SELECT					'{TemplateName}', 
									'{Subject}',
									'{Body}',
									1,
                                    '{SessionEngine.Username}'
			WHERE NOT EXISTS		(SELECT 1 FROM [{DatabaseName}].dbo.KvDM_EmailTemplate WHERE TemplateName = '{TemplateName}' AND [User] = '{SessionEngine.Username}');";

            ExecuteNonQuery(query);
        }

        public static DataTable GetDebtorsContact()
        {
            InitialiseConnection();

            string query = $@"
			SELECT			c.CustomerId,
			CASE 
				WHEN c.Amount > 0
					THEN e.Code + ' : ' + ISNULL(CONVERT(varchar, c.ContactDate, 103), '') + ' : R' + CONVERT(varchar, REPLACE(FORMAT(c.Amount, 'N2'),',',' '))

				WHEN c.Amount is null
					THEN ''

				ELSE e.Code + ' : ' + ISNULL(CONVERT(varchar, c.ContactDate, 103), '')

			END AS [Action],
			ISNULL(CONVERT(varchar, MAX(c.DateCreated), 103), '') as [Contacted],
            			CASE 
    WHEN c.ContactDate < CAST(GETDATE() AS DATE) THEN 1 
    ELSE 0 
END AS ActionDate

			FROM			[{DatabaseName}].dbo.KvDM_DebtorsContact c
			
			INNER JOIN		[{DatabaseName}].dbo.KvDM_DebtorsExcuses e on c.ExcuseID = e.Id
			LEFT JOIN		[{DatabaseName}].dbo.KvDM_DebtorsContactSelection s on c.Id = s.DebtorsContactId 

			WHERE			s.bSelected = 1
			GROUP BY		c.CustomerId,
							c.Amount,
							c.ContactDate,
							e.Code";

            return ExecuteDataTable(query);
        }



        public static DataTable GetExcuses()
        {
            string query = $@"
			SELECT			
							Code + ' - ' + [Description] as Excuse
			FROM			[{DatabaseName}].dbo.KvDM_DebtorsExcuses

";

            return ExecuteDataTable(query);
        }

        public static DataTable GetContactHistory(string CustomerId)
        {
            string query = $@"
			SELECT			c.Id,
							c.CustomerId,
							e.[Description] as [Type],
							e.Code,
							Reference,
					        REPLACE( FORMAT(isNull(OutstandingAmount,0), 'N2'), ',', ' ') OutstandingAmount,
                            REPLACE(Format(Amount , 'N2') , ',' , ' ') Amount,
							ISNULL(CONVERT(varchar,ContactDate, 101), '') ContactDate,
							c.Note,
							ISNULL(s.bSelected , 0 ) Selected
			FROM			[{DatabaseName}].dbo.KvDM_DebtorsContact c

			Left Join		[{DatabaseName}].dbo.KvDM_DebtorsExcuses e on c.ExcuseID = e.Id
            Left Join		[{DatabaseName}].dbo.KvDM_DebtorsContactSelection s on c.Id = s.DebtorsContactId

			WHERE			c.CustomerId = '{CustomerId}'";

            return ExecuteDataTable(query);
        }

        public static void InsertContactHistory(string CustomerId, int ExcuseID, string OrderNum, double Amount, double Outstanding, DateTime ContactDate, string Note)
        {
            string query = $@"

			UPDATE			        [{DatabaseName}].dbo.KvDM_DebtorsContact
			SET				        Reference = '{OrderNum}',
							        Amount = {Amount},
                                    OutstandingAmount = {Outstanding},
							        Note = '{Note}',
							        [User] = '{SessionEngine.Username}',
							        DateCreated = GETDATE()

			WHERE			        CustomerId = '{CustomerId}'
			AND				        ExcuseID = {ExcuseID} 
			AND				        ContactDate = '{ContactDate}'

			INSERT 
			INTO			        [{DatabaseName}].dbo.KvDM_DebtorsContact (
							        CustomerId,
							        ExcuseID,
							        Reference,
                                    Amount,
                                    OutstandingAmount,
							        ContactDate,
							        Note,
							        [User],
							        DateCreated)

			SELECT			        '{CustomerId}',
							        {ExcuseID},
							        '{OrderNum}',
                                    '{Amount}',
                                    '{Outstanding}',
							        '{ContactDate}',
							        '{Note}',
							        '{SessionEngine.Username}',
							        GETDATE()

            WHERE NOT EXISTS		(SELECT 1 FROM [{DatabaseName}].dbo.KvDM_DebtorsContact WHERE CustomerId = '{CustomerId}' AND ExcuseID = {ExcuseID} AND ContactDate = '{ContactDate}')";


            ExecuteNonQuery(query);
        }

        public static int GetExcuseId(string Type)
        {
            string query = $@"
			SELECT			Id 
			FROM			[{DatabaseName}].dbo.KvDM_DebtorsExcuses 
			WHERE			CONCAT(Code, ' - ', [Description]) = '{Type}'";

            object res = ExecuteScalar(query);
            return res == null? 0 : Convert.ToInt32(res);
        }

        public static void DeleteContactHistory(string CustomerId, int ExcuseID, DateTime ContactDate)
        {
            string query = $@"
			DELETE 
			FROM			[{DatabaseName}].dbo.KvDM_DebtorsContact  
			WHERE			CustomerId = '{CustomerId}' 
			AND				ExcuseID = {ExcuseID} 
			AND				ContactDate = '{ContactDate}'";

            ExecuteNonQuery(query);
        }

        public static void InsertContactSelection(string ContactId, string CustomerId)
        {
            string query = $@"
			UPDATE					[{DatabaseName}].[dbo].[KvDM_DebtorsContactSelection]
			SET						bSelected = 0 
			WHERE					CustomerId = '{CustomerId}'
			AND						DebtorsContactId <> {ContactId}

			UPDATE					[{DatabaseName}].[dbo].[KvDM_DebtorsContactSelection]
			SET						bSelected = 1 
			WHERE					CustomerId = '{CustomerId}'
			AND						DebtorsContactId = {ContactId}

			INSERT INTO				[{DatabaseName}].[dbo].[KvDM_DebtorsContactSelection](
									DebtorsContactId, 
									CustomerId, 
									bSelected)

			SELECT					{ContactId},
									'{CustomerId}',
									1

			WHERE NOT EXISTS		(SELECT 1 FROM [{DatabaseName}].[dbo].[KvDM_DebtorsContactSelection] WHERE DebtorsContactId = {ContactId})";

            ExecuteNonQuery(query);
        }

        public static void UpdateContactSelection(int DcLink)
        {
            string query = $@"
			UPDATE					[{DatabaseName}].[dbo].[KvDM_DebtorsContactSelection]
			SET						bSelected = 0 
			WHERE					CustomerId = {DcLink}";

            ExecuteNonQuery(query);
        }

        public static DataTable GetDocHistory(string CustomerId)
        {
            string query = $@"
			SELECT			Id,
							CustomerId,
							DocType,
							FilePath,
							Reference,
							bInclude,
							[User],
							UploadDate
			FROM			[{DatabaseName}].dbo.KvDM_DocumentMapping
			WHERE			CustomerId = '{CustomerId}'
			ORDER BY		DocType";

            return ExecuteDataTable(query);
        }

        public static int InsertDocMapping(string CustomerId, string DocType, string FilePath, string Reference, int bIncluded, int DocId)
        {
            string query = "";

            if (DocId == 0)
            {
                query = $@"
				INSERT 
				INTO				[{DatabaseName}].dbo.KvDM_DocumentMapping (
									CustomerId, 
									DocType,
									FilePath, 
									Reference,
									bInclude,
									[User],
									UploadDate)

				SELECT				'{CustomerId}',
									'{DocType}',
									'{FilePath}',
									'{Reference}',
									{bIncluded},
									'{SessionEngine.Username}',
									GETDATE()

				--WHERE NOT EXISTS	(SELECT 1 FROM [{DatabaseName}].dbo.KvDM_DocumentMapping WHERE CustomerId = '{CustomerId}' AND DocType = '{DocType}' AND Reference = '{Reference}')";


            }
            else
            {
                query = $@"
				UPDATE				[{DatabaseName}].dbo.KvDM_DocumentMapping 

				SET					FilePath = '{FilePath}',
									[User] = '{SessionEngine.Username}',
									UploadDate = GETDATE()

				WHERE				Id = {DocId}";
            }



            ExecuteNonQuery(query);

            return DocId == 0 ? GetLastDocMapping() : DocId;
        }

        public static int GetLastDocMapping()
        {
            string query = $@"
            SELECT          MAX(Id) as Id  
            FROM            [{DatabaseName}].dbo.KvDM_DocumentMapping
    ";

            object res = ExecuteScalar(query);
            return res == null ? 0 : Convert.ToInt32(res);
        }

        public static void InsertDocAttachment(int DocMappingId, string FileName, string ContentType, int FileSize, byte[] FileData)
        {
            string query = $@"
        INSERT INTO [{DatabaseName}].dbo.KVDM_DocumentFiles (
            DocMappingId, 
            FileName, 
            ContentType, 
            FileSize, 
            UploadedDate, 
            FileData)
        VALUES (
            @DocMappingId, 
            @FileName, 
            @ContentType, 
            @FileSize, 
            GETDATE(), 
            @FileData)";

            using (var conn = DatabaseConnection)
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@DocMappingId", DocMappingId);
                cmd.Parameters.AddWithValue("@FileName", FileName);
                cmd.Parameters.AddWithValue("@ContentType", ContentType);
                cmd.Parameters.AddWithValue("@FileSize", FileSize);
                cmd.Parameters.Add("@FileData", SqlDbType.VarBinary).Value = FileData;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        internal static DataTable GetAttachmentById(int id)
        {
            string query = $@"
			SELECT			FileData,
							ContentType,
							[FileName]
			FROM			[{DatabaseName}].dbo.KVDM_DocumentFiles
			WHERE			DocMappingId = {id}";


            return ExecuteDataTable(query);
        }

        public static void DeleteAttachmentHistory(int DocId)
        {
            string query = $@"
			DELETE			
			FROM			[{DatabaseName}].dbo.KVDM_DocumentFiles
			WHERE			DocMappingId = {DocId}

			DELETE
			FROM			[{DatabaseName}].dbo.KvDM_DocumentMapping
			WHERE			Id = {DocId}";

            ExecuteNonQuery(query);
        }

        public static DataTable GetAdditionalDocs()
        {
            string query = $@"
			SELECT			CustomerId,
							DocType,
							FilePath,
							Reference,
							bInclude
			FROM			[{DatabaseName}].dbo.KvDM_DocumentMapping d 


			WHERE			bInclude = 1
			AND				[User] = '{SessionEngine.Username}'";

            return ExecuteDataTable(query);
        }

        public static DataTable GetAttachments(string CustomerId)
        {
            string query = $@"
			SELECT			f.FileData,
							f.FileName

			FROM			[{DatabaseName}].dbo.KvDM_DocumentMapping m

			INNER JOIN		[{DatabaseName}].dbo.KVDM_DocumentFiles f on m.Id = f.DocMappingId
			
			WHERE			m.bInclude = 1 AND m.CustomerId = '{CustomerId}'";

            return ExecuteDataTable(query);
        }
    }
}
