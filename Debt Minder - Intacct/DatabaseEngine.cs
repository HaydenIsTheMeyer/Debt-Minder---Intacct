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
            DatabaseName = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\InvoiceRun\DatabaseLogin", "Database", null);
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
    }
}
