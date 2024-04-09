namespace FcsLogic.GeneralRequest
{
    public class ResponseReq
    {
        internal string message;

        public bool success { get; set; }
        public string strXml { get; set; }
        public int returnCode { get; set; }
        public string returnCodeDesc { get; set; }


        public ResponseReq(bool success, string strXml, int returnCode, string returnCodeDesc)
        {
            this.success = success;
            this.strXml = strXml;
            this.returnCode = returnCode;
            this.returnCodeDesc = returnCodeDesc;
        }
        public override string ToString()
        {
            return string.Format("success={0} \nstrXml={1}\nreturnCode={2}\nreturnCodeDesc{3}", success, strXml, returnCode, returnCodeDesc);
        }
    }
}



