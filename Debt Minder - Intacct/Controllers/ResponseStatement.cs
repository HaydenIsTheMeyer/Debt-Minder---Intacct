using System.Xml.Serialization;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;


namespace Debt_Minder___Intacct.Controllers
{
    public class ResponseStatement
    {

        [XmlRoot("response")]
        public class Response
        {
            [XmlElement("control")]
            public Control Control { get; set; }

            [XmlElement("operation")]
            public Operation Operation { get; set; }
        }

        public class Control
        {
            [XmlElement("status")]
            public string Status { get; set; }

            [XmlElement("senderid")]
            public string SenderId { get; set; }

            [XmlElement("controlid")]
            public string ControlId { get; set; }

            [XmlElement("uniqueid")]
            public bool UniqueId { get; set; }

            [XmlElement("dtdversion")]
            public string DtdVersion { get; set; }
        }

        public class Operation
        {
            [XmlElement("authentication")]
            public Authentication Authentication { get; set; }

            [XmlElement("result")]
            public Result Result { get; set; }
        }

        public class Authentication
        {
            [XmlElement("status")]
            public string Status { get; set; }

            [XmlElement("userid")]
            public string UserId { get; set; }

            [XmlElement("companyid")]
            public string CompanyId { get; set; }

            [XmlElement("locationid")]
            public string LocationId { get; set; }

            [XmlElement("sessiontimestamp")]
            public DateTime SessionTimestamp { get; set; }

            [XmlElement("sessiontimeout")]
            public DateTime SessionTimeout { get; set; }
        }

        public class Result
        {
            [XmlElement("status")]
            public string Status { get; set; }

            [XmlElement("function")]
            public string Function { get; set; }

            [XmlElement("controlid")]
            public string ControlId { get; set; }

            [XmlArray("data")]
            [XmlArrayItem("arinvoice")]
            public List<ARInvoice> Invoices { get; set; }
        }

        public class ARInvoice
        {
            [XmlElement("CUSTOMERID")]
            public string CustomerID { get; set; }

            [XmlElement("CONTACT.PRINTAS")]
            public string ContactPrintAs { get; set; }

            [XmlElement("CONTACT.MAILADDRESS.ADDRESS1")]
            public string Address1 { get; set; }

            [XmlElement("CONTACT.MAILADDRESS.ADDRESS2")]
            public string Address2 { get; set; }

            [XmlElement("CONTACT.MAILADDRESS.CITY")]
            public string City { get; set; }

            [XmlElement("CONTACT.MAILADDRESS.STATE")]
            public string State { get; set; }

            [XmlElement("CONTACT.MAILADDRESS.ZIP")]
            public string Zip { get; set; }

            [XmlElement("CUSTOMER.TOTALDUE")]
            public decimal CustomerTotalDue { get; set; }

            [XmlElement("WHENCREATED")]
            public string WhenCreated { get; set; }

            [XmlElement("RECORDID")]
            public string RecordID { get; set; }

            [XmlElement("TOTALENTERED")]
            public decimal TotalEntered { get; set; }

            [XmlElement("TOTALPAID")]
            public decimal TotalPaid { get; set; }

            [XmlElement("TOTALDUE")]
            public decimal TotalDue { get; set; }

            [XmlElement("CONTACT.EMAIL1")]
            public string Email1 { get; set; }
        }

    }
}
