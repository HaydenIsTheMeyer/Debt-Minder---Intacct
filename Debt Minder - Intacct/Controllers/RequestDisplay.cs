using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Debt_Minder___Intacct.Controllers
{
    public class RequestDisplay
    {
        // using System.Xml.Serialization;
        // XmlSerializer serializer = new XmlSerializer(typeof(Request));
        // using (StringReader reader = new StringReader(xml))
        // {
        //    var test = (Request)serializer.Deserialize(reader);
        // }

        [XmlRoot(ElementName = "control")]
        public class Control
        {

            [XmlElement(ElementName = "senderid")]
            public string Senderid { get; set; }

            [XmlElement(ElementName = "password")]
            public string Password { get; set; }

            [XmlElement(ElementName = "controlid")]
            public string Controlid { get; set; }

            [XmlElement(ElementName = "uniqueid")]
            public bool Uniqueid { get; set; }

            [XmlElement(ElementName = "dtdversion")]
            public string Dtdversion { get; set; }

            [XmlElement(ElementName = "includewhitespace")]
            public bool Includewhitespace { get; set; }
        }

        [XmlRoot(ElementName = "authentication")]
        public class Authentication
        {

            [XmlElement(ElementName = "sessionid")]
            public string Sessionid { get; set; }
        }

        [XmlRoot(ElementName = "select")]
        public class Select
        {

            [XmlElement(ElementName = "field")]
            public List<string> Field { get; set; }
        }

        [XmlRoot(ElementName = "notequalto")]
        public class Notequalto
        {

            [XmlElement(ElementName = "field")]
            public string Field { get; set; }

            [XmlElement(ElementName = "value")]
            public int Value { get; set; }
        }

        [XmlRoot(ElementName = "filter")]
        public class Filter
        {

            [XmlElement(ElementName = "notequalto")]
            public Notequalto Notequalto { get; set; }
        }

        [XmlRoot(ElementName = "query")]
        public class Query
        {

            [XmlElement(ElementName = "object")]
            public string Object { get; set; }

            [XmlElement(ElementName = "select")]
            public Select Select { get; set; }

            [XmlElement(ElementName = "filter")]
            public Filter Filter { get; set; }
        }

        [XmlRoot(ElementName = "function")]
        public class Function
        {

            [XmlElement(ElementName = "query")]
            public Query Query { get; set; }

            [XmlAttribute(AttributeName = "controlid")]
            public string Controlid { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot(ElementName = "content")]
        public class Content
        {

            [XmlElement(ElementName = "function")]
            public Function Function { get; set; }
        }

        [XmlRoot(ElementName = "operation")]
        public class Operation
        {

            [XmlElement(ElementName = "authentication")]
            public Authentication Authentication { get; set; }

            [XmlElement(ElementName = "content")]
            public Content Content { get; set; }
        }

        [XmlRoot(ElementName = "request")]
        public class Request
        {

            [XmlElement(ElementName = "control")]
            public Control Control { get; set; }

            [XmlElement(ElementName = "operation")]
            public Operation Operation { get; set; }
        }


        public static async Task GenerateXmlRequest(string sessionId)
        {
            var request = new Request
            {
                Control = new Control
                {
                    Senderid = "agroserve",
                    Password = "@Agr0s3rv3!@#",
                    Controlid = DateTime.UtcNow.ToString("yyyyMMddHHmmss"), // Timestamp as Control ID
                    Uniqueid = false,
                    Dtdversion = "3.0",
                    Includewhitespace = false
                },
                Operation = new Operation
                {
                    Authentication = new Authentication
                    {
                        Sessionid = sessionId
                    },
                    Content = new Content
                    {
                        Function = new Function
                        {
                            Controlid = Guid.NewGuid().ToString(), // Generate a unique GUID
                            Query = new Query
                            {
                                Object = "SODOCUMENT",
                                Select = new Select
                                {
                                    Field = new List<string>
                                {   "CUSTOMER.CUSTOMERID",
                                    "CUSTOMER.NAME",
                                    "CUSTOMER.TOTALDUE",
                                    "TOTALDUE",
                                    "DOCNO",
                                    "DOCID",
                                    "EXTERNALREFNO",
                                    "ORIGDOCDATE",
                                    "CUSTOMER.DISPLAYCONTACT.EMAIL1",
                                    "CUSTOMER.DISPLAYCONTACT.EMAIL2"
                                }
                                },
                                Filter = new Filter
                                {
                                    Notequalto = new Notequalto
                                    {
                                        Field = "totaldue",
                                        Value = 0
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Serialize to XML
            var xmlSerializer = new XmlSerializer(typeof(Request));
            var xmlSettings = new XmlSerializerNamespaces();
            xmlSettings.Add("", ""); // Remove XML namespaces
            string apiUrl = "https://api.intacct.com/ia/xml/xmlgw.phtml";
            string requestXml;

            using (var stringWriter = new StringWriterWithEncoding(Encoding.UTF8))
            {
                xmlSerializer.Serialize(stringWriter, request, xmlSettings);
                requestXml = stringWriter.ToString();

                using (HttpClient client = new HttpClient())
                {
                    // Create the request message (specify method and URL directly)
                    HttpRequestMessage HttpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl);

                    // Set the Content-Type header
                    HttpRequest.Content = new StringContent(requestXml, Encoding.UTF8, "application/xml");

                    // Send the request synchronously
                    HttpResponseMessage response = client.PostAsync(apiUrl, HttpRequest.Content).Result;  // Use Send instead of SendAsync


                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream xmlStream = response.Content.ReadAsStreamAsync().Result)
                        {
                            // Below is deserialisation
                            XmlSerializer serializer = new XmlSerializer(typeof(Display.Response));


                            // StringReader reader = new StringReader(test);
                            HomeController.res = (Display.Response)serializer.Deserialize(xmlStream);



                        }
                    }

                    //MessageBox.Show(test);


                }
            }











        }
    }

    // Custom StringWriter to enforce UTF-8 encoding
    public class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding encoding;

        public StringWriterWithEncoding(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public override Encoding Encoding => encoding;


    }
}
