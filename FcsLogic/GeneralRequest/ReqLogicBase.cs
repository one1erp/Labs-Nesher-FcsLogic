using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
//using MSXML;
using LSSERVICEPROVIDERLib;
using System.Reflection;
using System.IO;
using System.Configuration;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Runtime.CompilerServices;
using Oracle.DataAccess.Client;
using DAL;
using Common;
using System.Windows.Forms;
using FCS_OBJECTS;
using FcsLogic.SampleRequest;
using FcsLogic.RsultRequest;
using FcsLogic.GeneralRequest;

namespace FcsLogic.GeneralRequest
{         
    public class ReqLogicBase
    {
        protected INautilusDBConnection _ntlsCon;
        public OracleConnection oraCon;
        protected OracleCommand cmd;
        protected IDataLayer dal;
        protected ResponseServise _fcsResponse;

        //XmlConverter xml = new XmlConverter();

        protected event Action<string> SdgCreated;
        protected bool DEBUG;
        protected string _barcode;
        protected string U_FCS_MSG_ID;
        protected int _sdgId;
        protected string _sdgName;
        protected bool _requestFailed;
        protected string _statusMsg;
        protected List<PhraseEntry> phrases = null;
        protected U_FCS_MSG fcsmsg = null;



        protected Amil_Details Amil_Details = new Amil_Details();
        protected Expiry_Date Expiry_Date = new Expiry_Date();
        protected Importer_Details Importer_Details = new Importer_Details();
        protected Manufacture_Date Manufacture_Date = new Manufacture_Date();
        protected Sampling_Date Sampling_Date = new Sampling_Date();
        protected string[] classes = new string[]
        { "Attached_Document", "ATTACHED_DOCUMENT", "Report_Notes", "Test_Results", "Test_Result", "Report_Notes", "Lab_Notes" };


        //  readonly OutgoingReq OutgoingReq = new OutgoingReq();
        //  protected ResponseToDB ResponseDB = new ResponseToDB();

        //  responseServise resSendingResult;
        protected string urlApiServies, pfxFile, SOAPActionSample;


        public ReqLogicBase()
        {
            //Init fields
            _statusMsg = FCS_MSG_STATUS.New;
            _sdgId = 0;
            _sdgName = "";
            _requestFailed = false;

          
            string path = ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString;
            //string path = "";// ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString;
            //path = "Data Source=MICROB;user id=lims_sys;password=lims_sys;";
            //string assemblyPath = Assembly.GetExecutingAssembly().Location;
            //ExeConfigurationFileMap map = new ExeConfigurationFileMap();
            //map.ExeConfigFilename = assemblyPath + ".config";
            //Configuration cfg = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
            //var appSettings = cfg.AppSettings;
            //string path = Path.Combine(appSettings.Settings["connectionString"].Value, Environment.UserName.MakeSafeFilename('_'));

            //oraCon = new OracleConnection(ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString);
            oraCon = new OracleConnection(path);
            if (oraCon.State != ConnectionState.Open)
            {
                oraCon.Open();
            }
            Write("Before DB");
            dal = new MockDataLayer();
            //string ConStr = "metadata=res://*/NautilusModel1.csdl|res://*/NautilusModel1.ssdl|res://*/NautilusModel1.msl;provider=Oracle.DataAccess.Client;provider connection string="+'"'+"data source=MICROB;password=lims;persist security info=True;user id=LIMS"+'"';
            string ConStr = ConfigurationManager.ConnectionStrings["connectionStringEF"].ConnectionString;
            dal.Connect();
          //  dal.Connect();
            phrases = dal.GetPhraseByName("FCS Parameters").PhraseEntries.ToList();//;.OrderBy(o => o.ORDER_NUMBER)
            urlApiServies = GetPhraseEntry("urlApiServies");
            pfxFile = GetPhraseEntry("pfxFile");
            SOAPActionSample = GetPhraseEntry("SOAPActionSample");
            Write("After DB");
            Write(urlApiServies);
            Write(pfxFile);
            Write(SOAPActionSample);

        }
        protected string GetPhraseEntry(string phraseName)
        {
            return phrases.Find(x => x.PhraseName == phraseName).PhraseDescription;
        }


        protected void updateInsertData(string sql)
        {
            Write(sql);

            cmd = new OracleCommand(sql, oraCon);
            cmd.ExecuteNonQuery();
        }
        protected void Write(string s)
        {
            Console.WriteLine(s);
           // Logger.WriteLogFile(s);
        }


        protected XmlDocument ConvertObjectToXML<T>(T objectToConvert) where T : class
        {
            try
            {
                XmlDocument doc = new XmlDocument();

                Type sourceType = objectToConvert.GetType();

                XmlElement root = doc.CreateElement(sourceType.Name + "s");
                XmlElement rootChild = doc.CreateElement(sourceType.Name);

                PropertyInfo[] sourceProperties = sourceType.GetProperties();
                foreach (PropertyInfo pi in sourceProperties)
                {
                    if (pi.GetValue(objectToConvert, null) != null)
                    {
                        XmlElement child = doc.CreateElement(pi.Name);
                        child.InnerText = Convert.ToString(pi.GetValue(objectToConvert, null));
                        rootChild.AppendChild(child);
                    }
                }

                root.AppendChild(rootChild);
                doc.AppendChild(root);
                return doc;

            }
            catch (Exception EXP)
            {
                Write(EXP.Message);
                //    Logger.WriteLogFile(EXP.Message);
                string sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", EXP.Message, U_FCS_MSG_ID);
                updateInsertData(sql);
                return null;
            }
        }

        //     protected U_FCS_MSG fcsmsg = null;
        //     protected COA_Report SdgCoa_Report = null;
        // protected string guid = null;
        protected bool Is_Complete = false;


        protected XmlElement AddXmlNode2<T>(T objParent, PropertyInfo prop, XmlDocument xdoc) where T : class
        {
            try
            {
                XmlElement element = xdoc.CreateElement("fcs1", prop.Name, "http://schemas.datacontract.org/2004/07/FCS_LabsLib");
                string val = prop.GetValue(objParent, null) != null ? prop.GetValue(objParent, null).ToString() : "";
                if (prop.PropertyType == typeof(bool))
                {
                    val = val.ToLower();
                }
                element.InnerText = val;
                return element;
            }
            catch (Exception EXP)
            {
                Write(EXP.Message);
                //    Logger.WriteLogFile(EXP.Message);
                string sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", EXP.Message, U_FCS_MSG_ID);
                updateInsertData(sql);
                _requestFailed = true;
                return null;
            }
        }

        protected XmlElement GetAllChilds<T>(List<T> PropParent, string typeNode, XmlElement elementParent, XmlDocument xdoc) where T : class
        {
            try
            {
                foreach (var PropChilds in PropParent)
                {
                    XmlElement elementObj = xdoc.CreateElement("fcs1", typeNode, "http://schemas.datacontract.org/2004/07/FCS_LabsLib");

                    foreach (PropertyInfo prop in PropChilds.GetType().GetProperties())
                    {
                        if (prop.Name == "Notes")
                        {

                            XmlElement elementChild = xdoc.CreateElement("fcs1", prop.Name, "http://schemas.datacontract.org/2004/07/FCS_LabsLib");

                            elementChild = GetAllChildsLabNotes(PropChilds as Test_Result, "LabNotes", elementChild, xdoc);
                            elementObj.AppendChild(elementChild);
                        }
                        else
                        {
                            XmlElement elementChild = AddXmlNode2(PropChilds, prop, xdoc);
                            elementObj.AppendChild(elementChild);
                        }
                    }

                    elementParent.AppendChild(elementObj);
                }
                return elementParent;
            }
            catch (Exception EXP)
            {
                Write(EXP.Message);
                //    Logger.WriteLogFile(EXP.Message);
                string sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", EXP.Message, U_FCS_MSG_ID);
                updateInsertData(sql);
                _requestFailed = true;
                return null;
            }
        }
        protected XmlElement GetAllChildsReport_Date(Report_Date PropParent, string typeNode, XmlElement elementParent, XmlDocument xdoc)
        {
            try
            {
                foreach (PropertyInfo prop in PropParent.GetType().GetProperties())
                {
                    XmlElement element = AddXmlNode2(PropParent, prop, xdoc);
                    elementParent.AppendChild(element);
                }

                return elementParent;
            }
            catch (Exception EXP)
            {
                Write(EXP.Message);
                //    Logger.WriteLogFile(EXP.Message);
                string sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", EXP.Message, U_FCS_MSG_ID);
                updateInsertData(sql);
                _requestFailed = true;
                return null;
            }
        }
        protected XmlElement GetAllChildsLabNotes(Test_Result PropParent, string typeNode, XmlElement elementParent, XmlDocument xdoc)
        {
            try
            {
                foreach (var PropChilds in PropParent.Notes.Lab_Notes)
                {
                    XmlElement elementObj = xdoc.CreateElement("fcs1", typeNode, "http://schemas.datacontract.org/2004/07/FCS_LabsLib");

                    foreach (PropertyInfo prop in PropChilds.GetType().GetProperties())
                    {

                        XmlElement element = AddXmlNode2(PropChilds, prop, xdoc);
                        elementObj.AppendChild(element);

                    }
                    elementParent.AppendChild(elementObj);
                }
                return elementParent;
            }
            catch (Exception EXP)
            {
                Write(EXP.Message);
                //    Logger.WriteLogFile(EXP.Message);
                string sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", EXP.Message, U_FCS_MSG_ID);
                updateInsertData(sql);
                _requestFailed = true;
                return null;
            }
        }

    }

}
