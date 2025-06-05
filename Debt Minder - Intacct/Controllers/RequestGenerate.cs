using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Debt_Minder___Intacct.Controllers
{
    public class RequestGenerate
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

        [XmlRoot(ElementName = "SODOCUMENT")]
        public class SODOCUMENT
        {

            [XmlElement(ElementName = "DOCID")]
            public string DOCID { get; set; }
        }

        [XmlRoot(ElementName = "retrievepdf")]
        public class Retrievepdf
        {

            [XmlElement(ElementName = "SODOCUMENT")]
            public SODOCUMENT SODOCUMENT { get; set; }
        }

        [XmlRoot(ElementName = "function")]
        public class Function
        {

            [XmlElement(ElementName = "retrievepdf")]
            public Retrievepdf Retrievepdf { get; set; }

            [XmlAttribute(AttributeName = "controlid")]
            public string Controlid { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot(ElementName = "content")]
        public class Content
        {

            [XmlElement(ElementName = "function")]
            public List<Function> Function { get; set; }
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

        public static void GenerateXmlRequest(string sessionId, List<string> docIds)
        {
            var request = new Request
            {
                Control = new Control
                {
                    Senderid = "agroserve",
                    Password = "@Agr0s3rv3!@#",
                    Controlid = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
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
                        Function = new List<Function>()
                    }
                }
            };

            // Add functions for each DOCID
            foreach (var docId in docIds)
            {
                request.Operation.Content.Function.Add(new Function
                {
                    Controlid = Guid.NewGuid().ToString(),
                    Retrievepdf = new Retrievepdf
                    {
                        SODOCUMENT = new SODOCUMENT
                        {
                            DOCID = docId
                        }
                    }
                });
            }

            // Serialize to XML
            var xmlSerializer = new XmlSerializer(typeof(Request));
            var xmlSettings = new XmlSerializerNamespaces();
            xmlSettings.Add("", "");
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
                            XmlSerializer serializer = new XmlSerializer(typeof(pdfResponse.Response));


                            // StringReader reader = new StringReader(test);
                            HomeController.Pdfres = (pdfResponse.Response)serializer.Deserialize(xmlStream);



                        }
                    }

                    //MessageBox.Show(test);


                }
            }




        }


    }

}
