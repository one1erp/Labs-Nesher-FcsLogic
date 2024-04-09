using System;
using System.Xml;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;
using RestSharp;
using System.Net;

namespace FcsLogic.GeneralRequest
{


    public  class OutgoingReq
    {

        public static ResponseReq PostReq(string url, string pfxFile, string xmlFile, string SOAPAction)
        {
            ResponseReq res = new ResponseReq(false, null, 0, null);


            try
            {
                var client = new RestClient(url);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("SOAPAction", SOAPAction);
                request.AddHeader("Content-Type", "text/xml");


                bool test = url.Contains("test");
                X509Certificate2Collection collection;

                if (test)
                {
                    collection = new X509Certificate2Collection();
                    collection.Import(pfxFile, "12345678", X509KeyStorageFlags.PersistKeySet);

                }
                else
                {
                    string subjectName = "Institute For Food Microbiology";

                    X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection certificates = store.Certificates;
                    X509Certificate2Collection foundCertificates = certificates.Find(X509FindType.FindBySubjectName, subjectName, false);
                    collection = certificates.Find(X509FindType.FindBySubjectName, subjectName, false);
                }


                client.ClientCertificates = new X509CertificateCollection();
                client.ClientCertificates.AddRange(collection);
                System.Net.ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc0 | 0x300 | 0xc00); //SecurityProtocolType.Tls;//| SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12
                XmlDocument xdoc = new XmlDocument();//xml doc used for xml parsing
                xdoc.Load(xmlFile);


                request.AddParameter("text/xml", xdoc.InnerXml, ParameterType.RequestBody);  //XmlText.bodyLab_Sample_Form
                IRestResponse response = client.Execute(request);
                //Console.WriteLine(response.Content);
                if (response != null && (int)response.StatusCode == 200)
                {
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(response.Content);
                    XmlNode Return_Code = xml.GetElementsByTagName("a:Return_Code")[0];
                    res.returnCode = int.Parse(Return_Code.InnerText);

                    if (Return_Code.InnerText == "0" || Return_Code.InnerText == "1")
                    {
                        res.success = true;
                        res.strXml = response.Content;
                    }
                    else
                    {
                        XmlNode Return_Code_Desc = xml.GetElementsByTagName("a:Return_Code_Desc")[0];
                        res.returnCodeDesc = System.Net.WebUtility.HtmlDecode(Return_Code_Desc.InnerText);
                        res.strXml = "api שגיאה בנתוני השליחה ל" + ": " + ((int)response.StatusCode).ToString();
                    }
                }
                else
                {
                    res.strXml = ((int)response.StatusCode).ToString() + ": " + response.StatusCode.ToString();
                }
                return res;
            }
            catch (Exception e)
            {
                //throw the exeption
                MessageBox.Show("Err at request: " + e.Message);
                return res;
            }


        }
    }
}



