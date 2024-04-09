using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Xml;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
//using System.Text;
//using System.Text.Json;
using RestSharp;
using System.Net;
using FcsLogic.GeneralRequest;

namespace FcsLogic.RsultRequest
{
    class FileRequest
    {
        public static ResponseReq PostReq(string url, string pfxFile, string coa_PdfFile)
        {
            ResponseReq res = new ResponseReq(false, null,0,"");

            try
            {
                var client = new RestClient(url);
                var request = new RestRequest(Method.POST);

                X509Certificate2Collection collection = new X509Certificate2Collection();//ל phrase
                collection.Import(pfxFile, "12345678", X509KeyStorageFlags.PersistKeySet);
                client.ClientCertificates = new X509CertificateCollection();
                client.ClientCertificates.AddRange(collection);
                System.Net.ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc0 | 0x300 | 0xc00); 

                request.AlwaysMultipartFormData = true;
                request.AddHeader("Content-Type", "multipart/form-data");
                request.AddFile("", coa_PdfFile);

                IRestResponse response = client.Execute(request);

                if (response != null && (int)response.StatusCode == 200)
                {
                    string guid = response.Content;
                    res.message = guid;
                    res.success = true;
                }
                else
                {
                    res.message = ((int)response.StatusCode).ToString() + ": " + response.StatusCode.ToString();
                }
                return res;
            }
            catch (Exception e)
            {
                MessageBox.Show("Err at File_Request: " + e.Message);
                res.message = "Err at File_Request: " + e.Message;
                return res;
            }
            

        }

    }    
}