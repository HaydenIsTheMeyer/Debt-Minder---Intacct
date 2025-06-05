using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Debt_Minder___Intacct
{
    internal class SessionEngine
    {

        public static string Username { get; set; } = "Kiteview Admin";
        public static Guid Guid { get; set; }

        public static int UserId { get; set; }

        public static string Company { get; set; }

        public static string Version { get; set; }
        public static void CreateSession()
        {
            DatabaseEngine.InsertSession(Username, Guid);
        }

        public static bool ValidateSession()
        {
            bool isValid = Convert.ToBoolean(DatabaseEngine.ValidateSession(Username));
            return isValid;
            
        }

        public static void RemoveSession()
        {
            DatabaseEngine.RemoveSession(Username);
        }

        //public static bool ValidateSession(string Username, Guid guid)
        //{

        //}


    }
}
