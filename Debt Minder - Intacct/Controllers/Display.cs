using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Debt_Minder___Intacct.Controllers
{
    public class Display
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

            [XmlElement("data")]
            public Data Data { get; set; }
        }

        public class Data
        {
            [XmlAttribute("listtype")]
            public string ListType { get; set; }

            [XmlAttribute("totalcount")]
            public int TotalCount { get; set; }

            [XmlAttribute("offset")]
            public int Offset { get; set; }

            [XmlAttribute("count")]
            public int Count { get; set; }

            [XmlAttribute("numremaining")]
            public int NumRemaining { get; set; }

            [XmlElement("SODOCUMENT")]
            public List<SODocument> SODocuments { get; set; }
        }

        public class SODocument
        {
            [XmlElement("CUSTOMER.NAME")]
            public string CustomerName { get; set; }

            [XmlElement("CUSTOMER.TOTALDUE")]
            public decimal CustomerTotalDue { get; set; }

            [XmlElement("TOTALDUE")]
            public decimal TotalDue { get; set; }

            //[XmlElement("DOCNO")]
            public string DOCNO { get; set; }

            [XmlElement("DOCID")]
            public string DOCID { get; set; }

            [XmlElement("EXTERNALREFNO")]
            public string? EXTERNALREFNO { get; set; }

            [XmlElement("ORIGDOCDATE")]
            public string? ORIGDOCDATE { get; set; }

            [XmlElement("CUSTOMER.DISPLAYCONTACT.EMAIL1")]
            public string? EMAIL1 { get; set; }

            [XmlElement("CUSTOMER.DISPLAYCONTACT.EMAIL2")]
            public string? EMAIL2 { get; set; }
        }
    }
}
