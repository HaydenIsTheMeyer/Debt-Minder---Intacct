using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Debt_Minder___Intacct.Controllers
{
    public class pdfResponse
    {



        // using System.Xml.Serialization;
        // XmlSerializer serializer = new XmlSerializer(typeof(Response));
        // using (StringReader reader = new StringReader(xml))
        // {
        //    var test = (Response)serializer.Deserialize(reader);
        // }

        [XmlRoot(ElementName = "control")]
        public class Control
        {

            [XmlElement(ElementName = "status")]
            public string Status { get; set; }

            [XmlElement(ElementName = "senderid")]
            public string Senderid { get; set; }

            [XmlElement(ElementName = "controlid")]
            public string? Controlid { get; set; }

            [XmlElement(ElementName = "uniqueid")]
            public bool Uniqueid { get; set; }

            [XmlElement(ElementName = "dtdversion")]
            public double Dtdversion { get; set; }
        }

        [XmlRoot(ElementName = "authentication")]
        public class Authentication
        {

            [XmlElement(ElementName = "status")]
            public string Status { get; set; }

            [XmlElement(ElementName = "userid")]
            public string Userid { get; set; }

            [XmlElement(ElementName = "companyid")]
            public string Companyid { get; set; }

            [XmlElement(ElementName = "locationid")]
            public string? Locationid { get; set; }

            [XmlElement(ElementName = "sessiontimestamp")]
            public DateTime Sessiontimestamp { get; set; }

            [XmlElement(ElementName = "sessiontimeout")]
            public DateTime Sessiontimeout { get; set; }
        }

        [XmlRoot(ElementName = "sodocument")]
        public class Sodocument
        {

            [XmlElement(ElementName = "docid")]
            public string Docid { get; set; }

            [XmlElement(ElementName = "pdfdata")]
            public string Pdfdata { get; set; }
        }

        [XmlRoot(ElementName = "data")]
        public class Data
        {

            [XmlElement(ElementName = "sodocument")]
            public Sodocument Sodocument { get; set; }

            [XmlAttribute(AttributeName = "listtype")]
            public string Listtype { get; set; }

            [XmlAttribute(AttributeName = "count")]
            public int Count { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot(ElementName = "result")]
        public class Result
        {

            [XmlElement(ElementName = "status")]
            public string Status { get; set; }

            [XmlElement(ElementName = "function")]
            public string Function { get; set; }

            [XmlElement(ElementName = "controlid")]
            public string Controlid { get; set; }

            [XmlElement(ElementName = "data")]
            public Data Data { get; set; }
        }

        [XmlRoot(ElementName = "operation")]
        public class Operation
        {

            [XmlElement(ElementName = "authentication")]
            public Authentication Authentication { get; set; }

            [XmlElement(ElementName = "result")]
            public List<Result> Result { get; set; }
        }

        [XmlRoot(ElementName = "response")]
        public class Response
        {

            [XmlElement(ElementName = "control")]
            public Control Control { get; set; }

            [XmlElement(ElementName = "operation")]
            public Operation Operation { get; set; }
        }

    }
}
