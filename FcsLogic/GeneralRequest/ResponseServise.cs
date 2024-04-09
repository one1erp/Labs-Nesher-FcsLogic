namespace FcsLogic
{
    public class ResponseServise
    {
        public bool success { get; set; }
        public string str { get; set; }
        public string err { get; set; }
        public string errDesc { get; set; }



        public ResponseServise(bool success, string str, string err,string errDesc)
        {
            this.success = success;
            this.str = str;
            this.err = err;
            this.errDesc = errDesc;
        }
    }
}
