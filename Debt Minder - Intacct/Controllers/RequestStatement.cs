using System.Xml.Serialization;
using System;

using System.IO;
using System.Net;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Debt_Minder___Intacct.Controllers
{
    public class RequestStatement
    {

        [XmlRoot("request")]
        public class Request
        {
            [XmlElement("control")]
            public Control Control { get; set; }

            [XmlElement("operation")]
            public Operation Operation { get; set; }
        }

        public class Control
        {
            [XmlElement("senderid")]
            public string SenderId { get; set; }

            [XmlElement("password")]
            public string Password { get; set; }

            [XmlElement("controlid")]
            public string ControlId { get; set; }

            [XmlElement("uniqueid")]
            public bool UniqueId { get; set; }

            [XmlElement("dtdversion")]
            public string DtdVersion { get; set; }

            [XmlElement("includewhitespace")]
            public bool IncludeWhitespace { get; set; }
        }

        public class Operation
        {
            [XmlElement("authentication")]
            public Authentication Authentication { get; set; }

            [XmlElement("content")]
            public Content Content { get; set; }
        }

        public class Authentication
        {
            [XmlElement("sessionid")]
            public string SessionId { get; set; }
        }

        public class Content
        {
            [XmlElement("function")]
            public Function Function { get; set; }
        }

        public class Function
        {
            [XmlAttribute("controlid")]
            public string ControlId { get; set; }

            [XmlElement("readByQuery")]
            public ReadByQuery ReadByQuery { get; set; }
        }

        public class ReadByQuery
        {
            [XmlElement("object")]
            public string Object { get; set; }

            [XmlElement("fields")]
            public string Fields { get; set; }

            [XmlElement("query")]
            public string Query { get; set; }

            [XmlElement("pagesize")]
            public int PageSize { get; set; }
        }

        public static async Task CreateRequestXml(string sessionId, string customerName)
        {
            var request = new Request
            {
                Control = new Control
                {
                    SenderId = "agroserve",
                    Password = "@Agr0s3rv3!@#",
                    ControlId = DateTime.UtcNow.Ticks.ToString(),
                    UniqueId = false,
                    DtdVersion = "3.0",
                    IncludeWhitespace = false
                },
                Operation = new Operation
                {
                    Authentication = new Authentication
                    {
                        SessionId = sessionId
                    },
                    Content = new Content
                    {
                        Function = new Function
                        {
                            ControlId = Guid.NewGuid().ToString(),
                            ReadByQuery = new ReadByQuery
                            {
                                Object = "ARINVOICE",
                                Fields = "CUSTOMERID, CONTACT.PRINTAS, CONTACT.MAILADDRESS.ADDRESS1, CONTACT.MAILADDRESS.ADDRESS2, CONTACT.MAILADDRESS.CITY, CONTACT.MAILADDRESS.STATE, CONTACT.MAILADDRESS.ZIP, CUSTOMER.TOTALDUE, WHENCREATED, RECORDID, TOTALENTERED, TOTALPAID, TOTALDUE, CONTACT.EMAIL1",
                                Query = $"CUSTOMERNAME = '{customerName}' AND TOTALDUE not in (0)",
                                PageSize = 100
                            }
                        }
                    }
                }
            };

            //var xmlSerializer = new XmlSerializer(typeof(Request));
            //using (var stringWriter = new StringWriter())
            //{
            //    xmlSerializer.Serialize(stringWriter, request);
            //  //  return stringWriter.ToString();
            //}






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
                            XmlSerializer serializer = new XmlSerializer(typeof(ResponseStatement.Response));


                            // StringReader reader = new StringReader(test);
                            HomeController.StatementRes = (ResponseStatement.Response)serializer.Deserialize(xmlStream);



                        }
                    }

                    //MessageBox.Show(test);


                }
            }
        }

    }


}
