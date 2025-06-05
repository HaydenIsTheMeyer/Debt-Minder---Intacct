using System.Xml.Serialization;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using static Debt_Minder___Intacct.Controllers.ResponseAging;

namespace Debt_Minder___Intacct.Controllers
{
    public class CustomStatement
    {


        [XmlRoot("Statement")]
        public class Statement
        {
            [XmlElement("Customer")]
            public Customer Customer { get; set; }

            [XmlElement("StatementDetails")]
            public StatementDetails StatementDetails { get; set; }

            [XmlArray("Entries")]
            [XmlArrayItem("Entry")]
            public List<Entry> Entries { get; set; }


        }

        public class Customer
        {
            [XmlElement("CustomerID")]
            public int CustomerID { get; set; }

            [XmlElement("DisplayPrintAs")]
            public string DisplayPrintAs { get; set; }

            [XmlElement("DisplayAddr1")]
            public string DisplayAddr1 { get; set; }

            [XmlElement("DisplayAddr2")]
            public string DisplayAddr2 { get; set; }

            [XmlElement("DisplayCity")]
            public string DisplayCity { get; set; }

            [XmlElement("DisplayState")]
            public string DisplayState { get; set; }

            [XmlElement("DisplayZip")]
            public string DisplayZip { get; set; }
        }

        public class StatementDetails
        {
            [XmlElement("StatementDate")]
            public string StatementDate { get; set; }

            [XmlElement("TotalDue")]
            public decimal TotalDue { get; set; }

            [XmlElement("Over90")]
            public decimal Over90 { get; set; }

            [XmlElement("in61-90")]
            public decimal NinetyDays { get; set; }

            [XmlElement("in31-60")]
            public decimal SixtyDays { get; set; }

            [XmlElement("in1-30")]
            public decimal ThirtyDays { get; set; }

            [XmlElement("Current")]
            public decimal Current { get; set; }



        }

        public class Entry
        {
            [XmlElement("WhenCreated")]
            public string WhenCreated { get; set; }

            [XmlElement("RecordID")]
            public string RecordID { get; set; }

            [XmlElement("TotalEntered")]
            public decimal TotalEntered { get; set; }

            [XmlElement("TotalPaid")]
            public decimal TotalPaid { get; set; }

            [XmlElement("Balance")]
            public decimal Balance { get; set; }
        }



    }

    public class StatementMapper
    {
        public static string MapResponseToCustomStatement(ResponseStatement.Response response, ResponseAging.ArAging Aging)
        {
            if (response == null || response.Operation?.Result?.Invoices == null)
                throw new ArgumentNullException("Response or Invoices list is null.");

            var firstInvoice = response.Operation.Result.Invoices[0];

            // Map the Customer object
            var customer = new CustomStatement.Customer
            {
                CustomerID = int.TryParse(firstInvoice.CustomerID, out int customerId) ? customerId : 0,
                DisplayPrintAs = firstInvoice.ContactPrintAs,
                DisplayAddr1 = firstInvoice.Address1,
                DisplayAddr2 = firstInvoice.Address2,
                DisplayCity = firstInvoice.City,
                DisplayState = firstInvoice.State,
                DisplayZip = firstInvoice.Zip
            };

            decimal Current = 0;
            decimal Thirty = 0;
            decimal Sixty = 0;
            decimal Ninety = 0;
            foreach(ResponseAging.AgingPeriod ArPeriod in Aging.AgingPeriods)
            {
                if(ArPeriod.Period == "-0")
                {
                    Current = ArPeriod.TotalAmount;

                }
                else if(ArPeriod.Period == "1-30")
                {
                    Thirty = ArPeriod.TotalAmount;
                }
                else if(ArPeriod.Period == "31-60")
                {
                    Sixty = ArPeriod.TotalAmount;
                }
                else if(ArPeriod.Period == "61-90")
                {
                    Ninety = ArPeriod.TotalAmount;
                }
            }

            // Map the StatementDetails object
            var statementDetails = new CustomStatement.StatementDetails
            {
                StatementDate = DateTime.Now.Date.ToString("yyyy/MM/dd"),
                TotalDue = firstInvoice.CustomerTotalDue,
                NinetyDays = Ninety,
                SixtyDays = Sixty,
                ThirtyDays = Thirty,
                Current = Current,
            };

            // Map the list of Entries
            var entries = new List<CustomStatement.Entry>();

            foreach (var invoice in response.Operation.Result.Invoices)
            {
                var entry = new CustomStatement.Entry
                {
                    WhenCreated = invoice.WhenCreated,
                    RecordID = invoice.RecordID,
                    TotalEntered = invoice.TotalEntered,
                    TotalPaid = invoice.TotalPaid,
                    Balance = invoice.TotalDue
                };
                entries.Add(entry);
            }


            // Create the final Statement object
            var statement = new CustomStatement.Statement
            {
                Customer = customer,
                StatementDetails = statementDetails,
                Entries = entries
            };




            var xmlSerializer = new XmlSerializer(typeof(CustomStatement.Statement));
            using (var stringWriter = new StringWriter())
            {
                xmlSerializer.Serialize(stringWriter, statement);
                return stringWriter.ToString();
            }

          //  return statement;
        }
    }
}
