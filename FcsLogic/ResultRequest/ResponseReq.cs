//using System.Text;
//using System.Text.Json;

namespace FcsLogic.RsultRequest
{
    public class ResponseReq
        {
            public bool success { get; set; }
            public string message { get; set; }

            public ResponseReq(bool success, string message)
            {
                this.success = success;
                this.message = message;
            }
        }
}