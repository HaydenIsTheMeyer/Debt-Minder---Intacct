using System.Xml.Serialization;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;


namespace Debt_Minder___Intacct.Controllers
{
    public class ResponseAging
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
            public Result Authentication { get; set; }

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
            [XmlElement("araging")]
            public ArAging ArAging { get; set; }
        }

        public class ArAging
        {
            [XmlElement("customerid")]
            public string CustomerId { get; set; }

            [XmlElement("aging")]
            public List<AgingPeriod> AgingPeriods { get; set; }
        }

        public class AgingPeriod
        {
            [XmlElement("agingperiod")]
            public string Period { get; set; }

            [XmlElement("totalamount")]
            public decimal TotalAmount { get; set; }

            [XmlElement("agingdetails")]
            public AgingDetails AgingDetails { get; set; }
        }

        public class AgingDetails
        {
            [XmlElement("agingdetail")]
            public List<AgingDetail> Details { get; set; }
        }

        public class AgingDetail
        {
            [XmlElement("invoiceno")]
            public string InvoiceNo { get; set; }

            [XmlElement("totaldue")]
            public string TotalDue { get; set; }

            [XmlElement("agingdate")]
            public string AgingDate { get; set; }

            [XmlElement("age")]
            public string Age { get; set; }
        }



    }
}
