using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
//using Kiteview;
using Microsoft.Win32;
using System.Web;
using System.Text;
using System.Data;
using System.Linq;
using DM_Middle_Ware;

namespace Debt_Minder___Intacct.Controllers
{
    public static class DllInitializer
    {
        public static ILayoutEngine LayoutEngine { get; private set; }
        public static IEmailEngine EmailEngine { get; private set; }
      


        public static void InitializeDll()
        {
            string dllPath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\InvoiceRun\EmailSettings", "LayoutPath", "");
            if (string.IsNullOrWhiteSpace(dllPath) || !File.Exists(dllPath))
            {
                //MessageBox.Show("DLL not found at specified path.", "Error - 0022", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                
                InitializeEmailEngine();
                InitializeLayoutEngine();
                //InitializeSdkEngine();
                
                
                
            }
            catch (Exception ex)
            {
               // MessageBox.Show($"Error loading DLL: {ex.Message}", "Error - 0021", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void InitializeLayoutEngine()
        {
            try
            {
                string dllPath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\InvoiceRun\EmailSettings", "LayoutPath", "");
                Assembly assembly = Assembly.LoadFrom(dllPath);
                
                Type layoutEngineType = assembly.GetType("Debt_Minder_Layouts.LayoutEngine");



                
                if (layoutEngineType != null && typeof(ILayoutEngine).IsAssignableFrom(layoutEngineType))
                {
                    LayoutEngine = Activator.CreateInstance(layoutEngineType) as ILayoutEngine;
                    Console.WriteLine($"LayoutEngine instance assigned: {LayoutEngine != null}");
                }
                else
                {
                    Console.WriteLine("Error: LayoutEngine type not found or does not implement ILayoutEngine.");
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error initializing LayoutEngine: {ex.Message}", "Error - 0031", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void InitializeEmailEngine()
        {
            try
            {

                string dllPath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\InvoiceRun\EmailSettings", "LayoutPath", "");
                Assembly assembly = Assembly.LoadFrom(dllPath);                
                Type emailEngineType = assembly.GetType("Debt_Minder_Layouts.EmailEngines");
                // List all types in the assembly to verify if "Debt_Minder_Layouts.EmailEngine" exists
                Console.WriteLine("Types in the loaded assembly:");


                // Attempt to get the EmailEngine type
                
                if (emailEngineType != null && typeof(IEmailEngine).IsAssignableFrom(emailEngineType))
                {
                    EmailEngine = Activator.CreateInstance(emailEngineType) as IEmailEngine;
                    Console.WriteLine($"EmailEngine instance assigned: {EmailEngine != null}");
                    
                }
                else
                {
                    Console.WriteLine("Error: EmailEngine type not found or does not implement IEmailEngine.");
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error initializing EmailEngine: {ex.Message}", "Error - 0032", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //public static void InitializeSdkEngine()
        //{
        //    try
        //    {

        //        string dllPath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\InvoiceRun\EmailSettings", "LayoutPath", "");
        //        Assembly assembly = Assembly.LoadFrom(dllPath);
        //        Type SdkEngineType = assembly.GetType("Debt_Minder_Layouts.sdkEngine");
        //        // List all types in the assembly to verify if "Debt_Minder_Layouts.EmailEngine" exists
        //        Console.WriteLine("Types in the loaded assembly:");


        //        // Attempt to get the EmailEngine type

        //        if (SdkEngineType != null && typeof(IsdkEngine).IsAssignableFrom(SdkEngineType))
        //        {
        //            sdkEngine = Activator.CreateInstance(SdkEngineType) as IsdkEngine;
        //            Console.WriteLine($"SdkEngine instance assigned: {sdkEngine != null}");

        //        }
        //        else
        //        {
        //            Console.WriteLine("Error: sdkEngine type not found or does not implement sdkEngine.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Error initializing EmailEngine: {ex.Message}", "Error - 0032", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}


    }
}
