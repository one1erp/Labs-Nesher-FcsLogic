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

namespace FcsLogic
{
    public class ResultReqLogicCls : ReqLogicBase
    {

        public ResultReqLogicCls() : base()
        {
        }

        public ResponseServise FcsResultRequest(string coaId)
        {
         var resSendingResult = new ResponseServise(false, null, null, null);
            try
            {
                _requestFailed = false;


                //phrases שליפת רשימת
                COA_Report coaReport = dal.GetCoaReportById(long.Parse(coaId));
                //DB שליפת הנתונים לשליחה מה
                if (coaReport.Status != "A")
                {
                    resSendingResult.Error += "התעודה לא מאושרת,לא ניתן לשלוח למשרד הבריאות" + ";";
                    _requestFailed = true;
                    Write("התעודה לא מאושרת,לא ניתן לשלוח למשרד הבריאות");
                }
                else
                {
                    fcsmsg = coaReport.Sdg.U_FCS_MSG;
                    string guid = "";
                    if (coaReport != null)
                    {
                        guid = LoadPdf2Api(coaReport, resSendingResult);
                    }

                    if (string.IsNullOrEmpty(guid) == false)
                    {
                        //לשליחה XML עריכת
                        string xmlFile = ParseXMLReuest2(coaReport, guid);
                        if (xmlFile != null && _requestFailed == false)
                        {
                            //erquest שליחת
                            ApiRequest(fcsmsg, xmlFile, resSendingResult);
                        }
                    }

                }

                dal.SaveChanges();
                resSendingResult.Success = _requestFailed;
                return resSendingResult;
            }
            catch (Exception en)
            {
                resSendingResult.Error += en.Message + ";";
                resSendingResult.Success = false;
                return resSendingResult;

            }
        }
        protected string ParseXMLReuest2(COA_Report SdgCoa_Report, string guid)
        {
            try
            {
                var sdg = SdgCoa_Report.Sdg;
                string fileName = Path.GetFileName(SdgCoa_Report.PdfPath);
                List<ATTACHED_DOCUMENT> AD = new List<ATTACHED_DOCUMENT>()
                {
                    new ATTACHED_DOCUMENT(fileName,1,".pdf","?",guid,SdgCoa_Report.PdfPath)
                };

                Attached_Document at = new Attached_Document(AD);
                List<_LabNotes> LN = new List<_LabNotes>()
                {
                    new _LabNotes(sdg.Comments,"C"),
                    new _LabNotes(sdg.U_COA_REMARKS,"A")
                };

                Report_Notes Report_Notes = new Report_Notes(LN);
                List<Test_Result> TR = new List<Test_Result>() { };
                Test_Results tr = new Test_Results(TR);
                Report_Date Report_Date = new Report_Date(SdgCoa_Report.CreatedOn.Value.ToShortDateString());

                string labReportVer = SdgCoa_Report.Name.Substring(SdgCoa_Report.Name.IndexOf("(") + 1,
                    SdgCoa_Report.Name.Length - SdgCoa_Report.Name.IndexOf(")") - 1);



                Request2 req = new Request2(
                    sdg.U_FOOD_TEMPERATURE,
                    at,
                    (int)fcsmsg.U_BARCODE,
                    Is_Complete,
                    13,
                    SdgCoa_Report.Name,
                    int.Parse(labReportVer),
                    false,
                    fcsmsg.U_ORGANIZATION,
                    "?",
                    Report_Date,
                    Report_Notes,
                    fcsmsg.U_SAMPLE_FORM_NUM.HasValue ? Convert.ToInt32(fcsmsg.U_SAMPLE_FORM_NUM) : 0,
                    tr,
                    sdg.COMPLETED_BY != null ? dal.GetOperators().Where(x => x.OperatorId == sdg.COMPLETED_BY.Value).FirstOrDefault().Name : "");




                foreach (var sample in sdg.Samples.Where(x => x.Status != "X"))
                {
                    foreach (var aliq in sample.Aliqouts.Where(x => x.Status != "X"))
                    {
                        List<Result> result4Array = new List<Result>();
                        var t = aliq.Tests;

                        //get one correct resultf from results of aliq

                        foreach (var test in aliq.Tests.Where(x => x.STATUS != "X"))
                        {
                            Result result = test.Results.Where(x => x.FormattedResult != null & x.REPORTED == "T").FirstOrDefault();

                            if (result != null)
                            {
                                result4Array.Add(result);
                                break;
                            }

                        }
                        //

                        if (result4Array.Count > 0)
                        {
                            //get All Remarks Of aliq
                            var results = from item in aliq.Tests
                                          from res in item.Results
                                          where res.Name == "הערה" && res.FormattedResult != null
                                          select res;

                            List<_LabNotes> LNR = new List<_LabNotes>() { };
                            foreach (var resultRmrk in results)
                            {
                                LNR.Add(new _LabNotes(resultRmrk.FormattedResult, ""));
                            }
                            Notes notes = new Notes(LNR);
                            //

                            foreach (Result result in result4Array)
                            {
                                Test_Result newTest = new Test_Result("", aliq.Name, "", aliq.TestTemplateEx.U_LOQ, 0,
                                    "", "", aliq.TestTemplateEx.U_DEFAULT_UNIT.ToString(), 0, aliq.TestTemplateEx.Standard, notes,
                                    result.FormattedResult, 0, sample.Description, "", aliq.TestTemplateEx.Name, 0, "");
                                req.Test_Results.Test_Result.Add(newTest);
                            }

                        }
                    }
                }

                XmlDocument xdoc = new XmlDocument();//xml doc used for xml parsing
                xdoc.Load(GetPhraseEntry("xmlParseResultRequestFile"));

                var nsmgr1 = new XmlNamespaceManager(xdoc.NameTable);
                nsmgr1.AddNamespace("fcs1", "http://schemas.datacontract.org/2004/07/FCS_LabsLib");
                XmlNode requestNode = xdoc.GetElementsByTagName("fcs:Request")[0];


                foreach (PropertyInfo prop in req.GetType().GetProperties())//.GetType()
                {
                    var x = prop.Name;

                    if (classes.Contains(prop.Name))
                    {
                        XmlElement element = xdoc.CreateElement("fcs1", prop.Name, "http://schemas.datacontract.org/2004/07/FCS_LabsLib");

                        switch (prop.Name)
                        {
                            case "Attached_Document":
                                element = GetAllChilds(req.Attached_Document.ATTACHED_DOCUMENT, "ATTACHED_DOCUMENT", element, xdoc);
                                break;
                            case "Report_Notes":
                                element = GetAllChilds(req.Report_Notes.LabNotes, "LabNotes", element, xdoc);
                                break;
                            case "Test_Results":
                                element = GetAllChilds(req.Test_Results.Test_Result, "Test_Result", element, xdoc);
                                break;
                            case "Report_Date":
                                element = GetAllChildsReport_Date(req.Report_Date, "Report_Date", element, xdoc);
                                break;
                        }
                        requestNode.AppendChild(element);
                    }
                    else
                    {
                        XmlElement element = AddXmlNode2(req, prop, xdoc);
                        requestNode.AppendChild(element);
                    }
                }
                string dt = DateTime.Now.ToString("dd-M-yyyy--HH-mm-ss");
                string XmlFileName = GetPhraseEntry("xmlResultRequestFile") + dt + "_" + fcsmsg.U_BARCODE.ToString() + ".xml";
                xdoc.Save(XmlFileName);
                return XmlFileName;
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

        private void ApiRequest(U_FCS_MSG fcsmsg, string xmlFile, ResponseServise resSendingResult)
        {
            try
            {
                if (_requestFailed == false)
                {
                    GeneralRequest.ResponseReq resRequest = OutgoingReq.PostReq(GetPhraseEntry("urlApiServies"), GetPhraseEntry("pfxFile"), xmlFile, GetPhraseEntry("SOAPActionRequest"));

                    string msg = "";

                    if (resRequest.success == true)
                    {
                        fcsmsg.U_STATUS = "SR";
                        msg = "התוצאות נשלחו למשרד הבריאות בהצלחה";
                        fcsmsg.U_RETURN_CODE = resRequest.returnCode;

                    }
                    else
                    {
                        msg = "שליחת התוצאות למשרד הבריאות נכשלה";
                        fcsmsg.U_ERROR += resRequest.strXml;
                        fcsmsg.U_STATUS = "FR";
                        fcsmsg.U_RETURN_CODE = resRequest.returnCode;
                        fcsmsg.U_RETURN_CODE_DESC = resRequest.returnCodeDesc;

                    }


                    Write(msg);
                    resSendingResult.Error = msg;
                }
            }
            catch (Exception EXP)
            {
                Write(EXP.Message);

                string sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", EXP.Message, U_FCS_MSG_ID);
                updateInsertData(sql);
                Write(EXP.Message);
                _requestFailed = true;
            }
        }

        private string LoadPdf2Api(COA_Report SdgCoa_Report, ResponseServise resSendingResult)
        {
            if (SdgCoa_Report.PdfPath != null)
            {
                //מעלה PDF
                // FileRequest.responseReq
                //
                //aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
                ResponseReq resRequest = FileRequest.PostReq(GetPhraseEntry("urlApiDocs"), pfxFile, SdgCoa_Report.PdfPath);
                if (resRequest.success == true)
                {
                    string guid = resRequest.message;

                    if (SdgCoa_Report.Partial == "T")
                        Is_Complete = true;
                    fcsmsg.U_STATUS = "SC";
                    return guid;

                }
                else
                {
                    resSendingResult.Error += "שליחת התעודה נכשלה" + resRequest.message;
                    fcsmsg.U_STATUS = "FC";
                    _requestFailed = true;
                    return null;
                }
            }
            else
            {
                fcsmsg.U_STATUS = "M";
                resSendingResult.Error += "לתעודה של דרישה זו pdf לא נמצא מסמך " + ";";
                _requestFailed = true;
                return null;
            }
        }







    }

}
