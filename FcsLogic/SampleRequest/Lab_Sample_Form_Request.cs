using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FcsLogic.SampleRequest
{
    class Lab_Sample_Form_Request
    {
        public string Barcode { get; set; }

        public string Lab_Code { get; set; }

        public Lab_Sample_Form_Request(string barcode, string labCode)
        {
            Barcode = barcode;
            Lab_Code = labCode;
        }
    }
}
