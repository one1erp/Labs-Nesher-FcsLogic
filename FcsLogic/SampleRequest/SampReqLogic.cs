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
    public class SampReqLogic : ReqLogicBase
    {



        public SampReqLogic() : base()
        {

        }

        public ResponseServise FcsSampleRequest(string _barcode)
        {


            _fcsResponse = new ResponseServise(true, null, null, null);
     

            try
            {
                this._barcode = _barcode;

                //עוד לפני הגישה למשה"ב - יצירת רשומה חדשה עם מזהה הברקוד
                CreateOrUpdateFcsMsg();

                //גישה למשה"ב
                //כאן הוא מקבל שגיאה, לא מצליח להתחבר למשה"ב
                Lab_Sample_Form();

                //SDG בדיקת נתוני תשובה האם ניתן ליצור 
                ValidationData();

                dal.SaveChanges();

        
                _fcsResponse.FCS_MSG_ID = U_FCS_MSG_ID;


                Write("_fcsResponse.success - " + _fcsResponse.Success);

                return _fcsResponse;
            }
            catch (Exception en)
            {
                _fcsResponse.Error += "FcsSampleRequest:" + en.Message + ";";
                return _fcsResponse;
            }

        }
        private void CreateOrUpdateFcsMsg()
        {
            string sql;
            OracleTransaction transaction = null;

            try
            {


                sql = string.Format("SELECT MU.U_FCS_MSG_ID,MU.U_STATUS FROM lims_sys.U_FCS_MSG M INNER JOIN lims_sys.U_FCS_MSG_USER MU ON M.U_FCS_MSG_ID = MU.U_FCS_MSG_ID WHERE M.name = '{0}'", _barcode);


                cmd = new OracleCommand(sql, oraCon);
                OracleDataReader reader1 = cmd.ExecuteReader();


                if (!reader1.HasRows)
                {
                    transaction = oraCon.BeginTransaction();
                    cmd.Connection = oraCon;
                    cmd.Transaction = transaction;

                    sql = "select lims.sq_U_FCS_MSG.nextval from dual";
                    Write(sql);

                    cmd = new OracleCommand(sql, oraCon);
                    var U_FCS_MSG_USER = cmd.ExecuteScalar();
                    U_FCS_MSG_ID = U_FCS_MSG_USER.ToString();
  //aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
                  
                    sql = string.Format("INSERT INTO lims_sys.U_FCS_MSG (U_FCS_MSG_ID, NAME, VERSION, VERSION_STATUS) VALUES ('" + U_FCS_MSG_ID + "','" + _barcode + "','{0}','{1}')", '1', 'A');

                    updateInsertData(sql);


                    sql = string.Format("INSERT INTO lims_sys.U_FCS_MSG_USER (U_FCS_MSG_ID) VALUES ('" + U_FCS_MSG_ID + "')");


                    sql = string.Format("INSERT INTO lims_sys.U_FCS_MSG_USER (U_FCS_MSG_ID,U_BARCODE, U_STATUS) VALUES ('" + U_FCS_MSG_ID + "','" + _barcode + "','{0}')", FCS_MSG_STATUS.New);
                    updateInsertData(sql);

                    _fcsResponse.FCS_MSG_ID = U_FCS_MSG_ID;

                    transaction.Commit();
                }
                else //Fcs Exists
                {
                    if (reader1.Read())
                    {
                        U_FCS_MSG_ID = reader1["U_FCS_MSG_ID"].ToString();
                        _statusMsg = reader1["U_STATUS"].ToString();
                    }
                    if (_statusMsg == "C")
                    {

                        _fcsResponse.Error += "קיימת דרישה למשרד הבריאות" + ";";
                        Write(_fcsResponse.Error);
                        _fcsResponse.Success = false; 
                    }
                    else
                    {
                        sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = '{0}' WHERE U_FCS_MSG_ID = '{1}'", "", U_FCS_MSG_ID);//איפוס שגיאות
                        updateInsertData(sql);
                    }
                }

            }
            catch (Exception EXP)
            {
                Write(EXP.Message);
                sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", EXP.Message, U_FCS_MSG_ID);
                updateInsertData(sql);
                _fcsResponse.Success = false;
                try
                {
                    transaction.Rollback();
                }
                catch (Exception en)
                {
                    _fcsResponse.Error += en.Message + ";";
                    Write(en.Message);
                    Write(en.Message);
                }
            }
        }

        private void Lab_Sample_Form()
        {
            //api request
            string sql;

            if (_fcsResponse.Success  && (_statusMsg == FCS_MSG_STATUS.New || _statusMsg == FCS_MSG_STATUS.Request_Failed || _statusMsg == FCS_MSG_STATUS.Create_SDG_Failed))
            {
               
                //עריכת xml לשליחה

                //לא מצליח לבנות XML
                string XmlFile = BuildXmlRequest();
                //נשלח XML null
                ResponseReq resRequest = OutgoingReq.PostReq(urlApiServies, pfxFile, XmlFile, SOAPActionSample);
                Write(resRequest.ToString());

                if (resRequest.success == true)
                {
                    //עריכת xml שהתקבל
                    var res = ParseXMLResponse(resRequest.strXml);
                    if (res != null)
                        Update_FCS_MSG(res);
                }
                else
                {
                    //עדכון סטטוס - נכשל בבקשת api

                    _fcsResponse.Error += "Api request fail!";
                    _fcsResponse.ErrDesc = resRequest.returnCodeDesc;
                    Write(_fcsResponse.Error);
                    Write(resRequest.strXml);
                    Write(resRequest.returnCodeDesc);
                    Write(resRequest.ToString());
                    _fcsResponse.Success = false;
                    sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_STATUS = '{0}',U_ERROR = U_ERROR || '{1}',U_RETURN_CODE = {2}, U_RETURN_CODE_DESC = '{3}' WHERE U_FCS_MSG_ID = '{4}'", FCS_MSG_STATUS.Request_Failed, resRequest.strXml, resRequest.returnCode, resRequest.returnCodeDesc, U_FCS_MSG_ID);
                    updateInsertData(sql);
                }
            }
        }

        protected void ValidationData()
        {
            string sql;

            try
            {
                if (_fcsResponse.Success  && (_statusMsg == FCS_MSG_STATUS.New || _statusMsg == FCS_MSG_STATUS.Request_Failed || _statusMsg == FCS_MSG_STATUS.Missing_Data || _statusMsg == FCS_MSG_STATUS.Create_SDG_Failed))
                {
                    //ולידציות נתונים
                    sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER M " +
                    "SET M.U_STATUS = CASE WHEN M.U_ORGANIZATION in(select PE.PHRASE_NAME from lims_sys.PHRASE_ENTRY pe inner join lims_sys.PHRASE_HEADER ph on PH.PHRASE_ID = PE.PHRASE_ID where PH.name = 'Closure Station') then '{0}' else '{1}' END, " +
                    "M.U_ERROR = M.U_ERROR || CASE WHEN M.U_ORGANIZATION not in(select PE.PHRASE_NAME from lims_sys.PHRASE_ENTRY pe inner join lims_sys.PHRASE_HEADER ph on PH.PHRASE_ID = PE.PHRASE_ID where PH.name = 'Closure Station') then ';תחנת הסגר לא קיימת במערכת' END " +
                    "WHERE M.U_FCS_MSG_ID = '{2}'", FCS_MSG_STATUS.New, FCS_MSG_STATUS.Missing_Data, U_FCS_MSG_ID);
                    updateInsertData(sql);

                    //Check Payer
                    sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER M " +
                    "SET M.U_STATUS = CASE WHEN TO_CHAR(M.U_PAYER_ID) in(select CLIENT_CODE from lims_sys.CLIENT where CLIENT_CODE is not null) then '{0}' else '{1}' END, " +
                    "M.U_ERROR = M.U_ERROR || CASE WHEN TO_CHAR(M.U_PAYER_ID) not in(select CLIENT_CODE from lims_sys.CLIENT where CLIENT_CODE is not null) then ';ח.פ לקוח משלם לא קיים במערכת' END " +
                    "WHERE M.U_FCS_MSG_ID = '{2}'", FCS_MSG_STATUS.New, FCS_MSG_STATUS.Missing_Data, U_FCS_MSG_ID);
                    updateInsertData(sql);

                    //sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER M " +
                    //"SET M.U_STATUS = CASE WHEN TO_CHAR(M.U_PRODUCT_GROUP_CODE) " +
                    //"in(SELECT fp.NAME FROM u_fcs_product FP ,u_fcs_product_user FPU ,PRODUCT p " +
                    //"WHERE fp.u_fcs_product_id=fpu.u_fcs_product_id AND fpu.u_lab_product = P.PRODUCT_ID and fp.NAME = TO_CHAR(M.U_PRODUCT_GROUP_CODE)) " +
                    //"then '{0}' else '{1}' END, " +
                    //"M.U_ERROR = M.U_ERROR || CASE WHEN TO_CHAR(M.U_PRODUCT_GROUP_CODE) not " +
                    //"in(SELECT fp.NAME FROM u_fcs_product FP ,u_fcs_product_user FPU ,PRODUCT p " +
                    //"WHERE fp.u_fcs_product_id=fpu.u_fcs_product_id AND fpu.u_lab_product = P.PRODUCT_ID and fp.NAME = TO_CHAR(M.U_PRODUCT_GROUP_CODE)) " +
                    //"then ';מוצר לא קיים במערכת' END " +
                    //"WHERE M.U_FCS_MSG_ID = '{2}'", Constants.New, Constants.Missing_Data, U_FCS_MSG_ID);
                    //updateInsertData(sql);

                    //Check Test Template
                    sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER M " +
                    "SET M.U_STATUS = CASE WHEN TO_CHAR(M.U_TEST_SUB_CODE) " +
                    "in(select FT.NAME FROM lims_sys.U_FCS_TEST FT INNER JOIN lims_sys.U_FCS_TEST_USER FTU ON FTU.U_FCS_TEST_ID=FT.U_FCS_TEST_ID " +
                    "WHERE FTU.u_lab_ttex is not NULL  and FT.NAME = TO_CHAR(M.U_TEST_SUB_CODE)) " +
                    "then '{0}' else '{1}' END, " +
                    "M.U_ERROR = M.U_ERROR || CASE WHEN TO_CHAR(M.U_TEST_SUB_CODE) not " +
                    "in(select FT.NAME FROM lims_sys.U_FCS_TEST FT INNER JOIN lims_sys.U_FCS_TEST_USER FTU ON FTU.U_FCS_TEST_ID=FT.U_FCS_TEST_ID " +
                    "WHERE FTU.u_lab_ttex is not NULL  and FT.NAME = TO_CHAR(M.U_TEST_SUB_CODE)) " +
                    "then ';מזהה בדיקה לא קיים במערכת' END " +
                    "WHERE M.U_FCS_MSG_ID = '{2}'", FCS_MSG_STATUS.New, FCS_MSG_STATUS.Missing_Data, U_FCS_MSG_ID);
                    updateInsertData(sql);

                    sql = string.Format("SELECT U_STATUS,U_ERROR FROM lims_sys.U_FCS_MSG_USER WHERE U_FCS_MSG_ID = '{0}'", U_FCS_MSG_ID);
                    cmd = new OracleCommand(sql, oraCon);
                    OracleDataReader reader2 = cmd.ExecuteReader();

                    if (!reader2.HasRows)
                    {
                        Write("The status does not exist!");
                        sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", "The status does not exist!", U_FCS_MSG_ID);
                        updateInsertData(sql);
                        _fcsResponse.Success = false;
                      
                    }
                    else
                    {
                        string err = "";
                        while (reader2.Read())
                        {
                            _statusMsg = reader2["U_STATUS"].ToString();
                            err = reader2["U_ERROR"].ToString();
                        }
                        if (_statusMsg == "M")
                        {
                            Write(err);
                            _fcsResponse.Success = false;
                        }
                        _fcsResponse.ErrDesc = err;

                    }
                }


            }
            catch (Exception EXP)
            {
                Write(EXP.Message);
                sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", EXP.Message, U_FCS_MSG_ID);
                updateInsertData(sql);
                _fcsResponse.Success = false;

            }
        }


        private void Update_FCS_MSG(Lab_Sample_Form_Response response)
        {
            string sql;

            try
            {

                
                //יצירת אובייקת ל db

    

                ResponseToDB ResponseDB  = response.GetDbObj();

            
                string strSql1 = "UPDATE lims_sys.U_FCS_MSG_USER  set ";
                string strSql2 = " WHERE U_FCS_MSG_ID = '{0}'";
                bool first = true;
                foreach (PropertyInfo prop in ResponseDB.GetType().GetProperties())
                {
                    if ((prop.GetValue(ResponseDB, null)) != null && (prop.GetValue(ResponseDB, null)) != "")
                    {
                        if (!first)
                        {
                            strSql1 += " , ";                        
                        }
                        first = false;
                        if (prop.Name == "U_Expiry_Date" || prop.Name == "U_Manufacture_Date" || prop.Name == "U_Sampling_Date")
                        {
                            //U_Expiry_Date = TO_DATE('26/01/2022', 'DD/MM/YYYY'),
                            strSql1  += prop.Name + " = TO_DATE('" + prop.GetValue(ResponseDB, null) + "', 'dd/mm/YYYY')";
                        }
                        else
                        {
                            strSql1 +=  prop.Name + " = '" + prop.GetValue(ResponseDB, null) + "'";

                        }
                    }
                }
                strSql1 += strSql2;
                sql = string.Format(strSql1, U_FCS_MSG_ID);
                updateInsertData(sql);
            }
            catch (Exception EXP)
            {
                Write(EXP.Message);
                sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", EXP.Message, U_FCS_MSG_ID);
                updateInsertData(sql);
                _fcsResponse.Success = false;
            }
        }



        private string BuildXmlRequest()
        {
            try
            {
                SampleRequest.Lab_Sample_Form_Request lsbqObj = new SampleRequest.Lab_Sample_Form_Request(_barcode, GetPhraseEntry("lab"));

                XmlDocument xmlTemplate = new XmlDocument();//xml doc used for xml parsing

                //כאן הוא נופל
                xmlTemplate.Load(GetPhraseEntry("xmlParseSampleRequestFile"));

                var nsmgr1 = new XmlNamespaceManager(xmlTemplate.NameTable);
                nsmgr1.AddNamespace("fcs1", "http://schemas.datacontract.org/2004/07/FCS_LabsLib");
                XmlNode requestNode = xmlTemplate.GetElementsByTagName("fcs:Request")[0];

                foreach (PropertyInfo prop in lsbqObj.GetType().GetProperties())//.GetType()
                {
                    XmlElement element = AddXmlNode1(lsbqObj, prop, xmlTemplate);
                    requestNode.AppendChild(element);
                }
                string dt = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
                string fileName = GetPhraseEntry("xmlSampleRequestFile") + dt + "_" + _barcode + ".xml";
                xmlTemplate.Save(fileName);
                return fileName;

            }
            catch (Exception EXP)
            {
                Write(EXP.Message);

                string sql;
                //         Logger.WriteLogFile(EXP.Message);
                sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", EXP.Message, U_FCS_MSG_ID);
                updateInsertData(sql);
                _fcsResponse.Success = false;
                return null;
            }
        }


        private Lab_Sample_Form_Response ParseXMLResponse(string xmlResponse)
        {
            try
            {
                Lab_Sample_Form_Response response = new Lab_Sample_Form_Response();
                XmlDocument xdoc = new XmlDocument();//xml doc used for xml parsing
                xdoc.LoadXml(xmlResponse);
                string dt = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
                xdoc.Save(GetPhraseEntry("xmlResponseFile") + dt + "_" + _barcode + ".xml");
                XmlNodeList responseNode = xdoc.GetElementsByTagName("Lab_Sample_FormResult")[0].ChildNodes;
                object testRes = RuntimeHelpers.GetObjectValue(response);

                foreach (XmlNode element in responseNode)
                {
                    if (element.ChildNodes.Count > 1)
                    {
                        object typeElem = null;
                        object testRes2 = null;
                        switch (element.LocalName)
                        {
                            case "Amil_Details":
                                testRes2 = RuntimeHelpers.GetObjectValue(Amil_Details);
                                typeElem = Amil_Details;
                                foreach (XmlNode childElement in element.ChildNodes)
                                {
                                    typeElem.GetType().GetProperty(childElement.LocalName).SetValue(testRes2, childElement.InnerText.Replace("'", ""), null);
                                }
                                Amil_Details = (Amil_Details)typeElem;
                                break;

                            case "Expiry_Date":
                                testRes2 = RuntimeHelpers.GetObjectValue(Expiry_Date);
                                typeElem = Expiry_Date;
                                foreach (XmlNode childElement in element.ChildNodes)
                                {
                                    typeElem.GetType().GetProperty(childElement.LocalName).SetValue(testRes2, childElement.InnerText.Replace("'", ""), null);
                                }
                                Expiry_Date = (Expiry_Date)typeElem;
                                if (Expiry_Date.Day != "0" && Expiry_Date.Month != "0" && Expiry_Date.Year != "0")
                                {
                                    string strExpiry_Date = Expiry_Date.Day + "/" + Expiry_Date.Month + "/" + Expiry_Date.Year;
                                    response.Expiry_Date._date = strExpiry_Date;
                                }
                                break;

                            case "Importer_Details":
                                testRes2 = RuntimeHelpers.GetObjectValue(Importer_Details);
                                typeElem = Importer_Details;
                                foreach (XmlNode childElement in element.ChildNodes)
                                {
                                    typeElem.GetType().GetProperty(childElement.LocalName).SetValue(testRes2, childElement.InnerText.Replace("'", ""), null);
                                }
                                Importer_Details = (Importer_Details)typeElem;
                                break;

                            case "Manufacture_Date":
                                testRes2 = RuntimeHelpers.GetObjectValue(Manufacture_Date);
                                typeElem = Manufacture_Date;
                                foreach (XmlNode childElement in element.ChildNodes)
                                {
                                    typeElem.GetType().GetProperty(childElement.LocalName).SetValue(testRes2, childElement.InnerText.Replace("'", ""), null);
                                }
                                Manufacture_Date = (Manufacture_Date)typeElem;
                                if (Manufacture_Date.Day != "0" && Manufacture_Date.Month != "0" && Manufacture_Date.Year != "0")
                                {
                                    string strManufacture_Date = Manufacture_Date.Day + "/" + Manufacture_Date.Month + "/" + Manufacture_Date.Year;
                                    response.Manufacture_Date._date = strManufacture_Date;
                                }
                                break;

                            case "Sampling_Date":
                                testRes2 = RuntimeHelpers.GetObjectValue(Sampling_Date);
                                typeElem = Sampling_Date;
                                foreach (XmlNode childElement in element.ChildNodes)
                                {
                                    typeElem.GetType().GetProperty(childElement.LocalName).SetValue(testRes2, childElement.InnerText.Replace("'", ""), null);
                                }
                                Sampling_Date = (Sampling_Date)typeElem;
                                if (Sampling_Date.Day != "0" && Sampling_Date.Month != "0" && Sampling_Date.Year != "0")
                                {
                                    string strSampling_Date = Sampling_Date.Day + "/" + Sampling_Date.Month + "/" + Sampling_Date.Year;
                                    response.Manufacture_Date._date = strSampling_Date;
                                }
                                break;
                        }
                    }
                    else
                        response.GetType().GetProperty(element.LocalName).SetValue(testRes, element.InnerText.Replace("'", ""), null);
                }
                return response;
            }
            catch (Exception EXP)
            {
                Write(EXP.Message);
                string sql;
                //         Logger.WriteLogFile(EXP.Message);
                sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", EXP.Message, U_FCS_MSG_ID);
                updateInsertData(sql);
                _fcsResponse.Success = false;
                return null;
            }
        }

        protected XmlElement AddXmlNode1<T>(T objParent, PropertyInfo prop, XmlDocument xdoc) where T : class
        {
            XmlElement element = xdoc.CreateElement("fcs1", prop.Name, "http://schemas.datacontract.org/2004/07/FCS_LabsLib");
            string val = prop.GetValue(objParent, null).ToString();
            element.InnerText = val;
            return element;
        }



        //private void CreateSdg()
        //{
        //    string sql;
        //    try
        //    {
        //        if (_return == false && (statusMsg == FCS_MSG_STATUS.New || statusMsg == FCS_MSG_STATUS.Create_SDG_Failed))
        //        {
        //            if (statusMsg == FCS_MSG_STATUS.Create_SDG_Failed)
        //                SelectDataFcsMsg();

        //            var cw = new CreateNewSdg();
        //            ResultCreateSdj resultCreateSdj = cw.RunEvent(ResponseDB, _processXml, oraCon, cmd, barcode, U_FCS_MSG_ID, GetPhraseEntry("resEntity"), GetPhraseEntry("docEntity"));//יצירת דרישה (sdg)
        //            sdgId = resultCreateSdj.sdgId;
        //            if (sdgId != 0)
        //            {
        //                sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_STATUS = '{0}' WHERE U_FCS_MSG_ID = '{1}'", FCS_MSG_STATUS.Sdg_Created, U_FCS_MSG_ID);//עדכון סטטוס - נוצר sdg
        //                RunSql(sql);

        //                sql = string.Format("UPDATE lims_sys.SDG_USER SET U_FCS_MSG_ID = '{0}' WHERE SDG_ID = '{1}'", U_FCS_MSG_ID, sdgId);
        //                RunSql(sql);


        //                sql = string.Format("SELECT NAME FROM lims_sys.SDG WHERE SDG_ID = '{0}'", sdgId);
        //                cmd = new OracleCommand(sql, oraCon);
        //                OracleDataReader reader1 = cmd.ExecuteReader();

        //                if (!reader1.HasRows)
        //                {
        //                    MessageBox.Show("The sdg created does not exist!");
        //                    Logger.WriteLogFile("The sdg created does not exist!");
        //                    sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || 'The sdg created does not exist!' WHERE U_FCS_MSG_ID = '{0}'", U_FCS_MSG_ID);
        //                    RunSql(sql);
        //                    _return = true;
        //                }
        //                else
        //                {
        //                    if (reader1.Read())
        //                    {
        //                        sdgName = reader1["NAME"].ToString();
        //                        MessageBox.Show("הדרישה נוצרה בהצלחה");
        //                        SdgCreated(sdgName);
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                MessageBox.Show("לא נוצרה דרישה");
        //                Logger.WriteLogFile("The sdg created does not exist!");
        //                _return = true;
        //                sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_STATUS = '{0}',U_ERROR = U_ERROR || '{1}' WHERE U_FCS_MSG_ID = '{2}'", FCS_MSG_STATUS.Create_SDG_Failed, resultCreateSdj.message, U_FCS_MSG_ID);//עדכון סטטוס - נכשל ביצירת sdg
        //                RunSql(sql);
        //            }
        //        }
        //    }
        //    catch (Exception EXP)
        //    {
        //        MessageBox.Show(EXP.Message);
        //        Logger.WriteLogFile(EXP.Message);
        //        sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", EXP.Message, U_FCS_MSG_ID);
        //        RunSql(sql);
        //        _return = true;
        //    }
        //}

        //private void RunSql(string sql)
        //{
        //    cmd = new OracleCommand(sql, oraCon);
        //    cmd.ExecuteNonQuery();
        //    Logger.WriteLogFile(sql);

        //}


        //private void SelectDataFcsMsg()
        //{
        //    string sql;
        //    try
        //    {
        //        sql = string.Format("SELECT * FROM lims_sys.U_FCS_MSG_USER WHERE U_FCS_MSG_ID = '{0}'", U_FCS_MSG_ID);
        //        cmd = new OracleCommand(sql, oraCon);
        //        OracleDataReader reader3 = cmd.ExecuteReader();

        //        if (!reader3.HasRows)
        //        {
        //            Logger.WriteLogFile("The fcsMsg does not exist!");
        //            MessageBox.Show("The fcsMsg does not exist!", "Nautilus", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        //            sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", "The fcsMsg does not exist!", U_FCS_MSG_ID);
        //            RunSql(sql);
        //            _return = true;
        //        }
        //        else
        //            while (reader3.Read())
        //            {
        //                ResponseDB = new ResponseToDB(
        //                int.Parse(reader3["U_Return_Code"].ToString()),
        //                reader3["U_Return_Code_Desc"].ToString(),
        //                int.Parse(reader3["U_Barcode"].ToString()),
        //                int.Parse(reader3["U_Sample_Form_Num"].ToString()),
        //                reader3["U_Is_Vet"].ToString(),
        //                reader3["U_Product_Group_Desc"].ToString(),
        //                int.Parse(reader3["U_Product_Group_Code"].ToString()),
        //                reader3["U_Product_name_heb"].ToString(),
        //                reader3["U_Product_name_eng"].ToString(),
        //                reader3["U_Product_Brand_Name"].ToString(),
        //                reader3["U_Organization"].ToString(),
        //                int.Parse(reader3["U_Payer_ID"].ToString()),
        //                reader3["U_Producer_Name"].ToString(),
        //                int.Parse(reader3["U_Producer_Country"].ToString()),
        //                reader3["U_Country_Name"].ToString(),
        //                reader3["U_Manufacture_Date"].ToString(),
        //                reader3["U_Sampling_Time"].ToString(),
        //                double.Parse(reader3["U_Sampling_Temp"].ToString()),
        //                reader3["U_Expiry_Date"].ToString(),
        //                reader3["U_Batch_Num"].ToString(),
        //                reader3["U_Property_Plus"].ToString(),
        //                reader3["U_Sampling_Place"].ToString(),
        //                reader3["U_Sampling_Reason"].ToString(),
        //                reader3["U_Packing_Type"].ToString(),
        //                reader3["U_Delivery_To_Lab"].ToString(),
        //                reader3["U_Sampling_Inspector"].ToString(),
        //                reader3["U_Inspector_Title"].ToString(),
        //                reader3["U_Container_Num"].ToString(),
        //                reader3["U_Num_Of_Samples"].ToString(),
        //                int.Parse(reader3["U_Num_Of_Samples_Vet"].ToString()),
        //                int.Parse(reader3["U_Del_File_Num"].ToString()),
        //                reader3["U_Sampling_Date"].ToString(),
        //                reader3["U_Importer_Store"].ToString(),
        //                reader3["U_Remark"].ToString(),
        //                int.Parse(reader3["U_Test_Sub_Code"].ToString()),
        //                reader3["U_Test_Description"].ToString()
        //                );
        //            }
        //    }
        //    catch (Exception EXP)
        //    {
        //        MessageBox.Show(EXP.Message);
        //        Logger.WriteLogFile(EXP.Message);
        //        sql = string.Format("UPDATE lims_sys.U_FCS_MSG_USER SET U_ERROR = U_ERROR || '{0}' WHERE U_FCS_MSG_ID = '{1}'", EXP.Message, U_FCS_MSG_ID);
        //        RunSql(sql);
        //        _return = true;
        //    }
        //}



    }

}
