namespace FcsLogic.RsultRequest
{


    public class Request2
    {
        public string Arrival_Temp { get; set; }

        public Attached_Document Attached_Document { get; set; }

        public int Barcode { get; set; }

        public bool Is_Complete { get; set; }

        public int Lab_Code { get; set; }

        public string Lab_Report_ID { get; set; }

        public int Lab_Report_Ver { get; set; }

        public bool Matching { get; set; }

        public string Organization { get; set; }

        public string Package_Condition { get; set; }

        public Report_Date Report_Date { get; set; }

        public Report_Notes Report_Notes { get; set; }

        public int Sample_Form_Num { get; set; }

        public Test_Results Test_Results { get; set; }

        public string Tester_Name { get; set; }

        public Request2(string arrival_Temp, Attached_Document attached_Document, int barcode, bool is_Complete, int lab_Code, string lab_Report_ID, int lab_Report_Ver, bool matching, string organization, string package_Condition, Report_Date report_Date, Report_Notes report_Notes, int sample_Form_Num, Test_Results test_Results, string tester_Name)
        {
            Arrival_Temp = arrival_Temp;
            Attached_Document = attached_Document;
            Barcode = barcode;
            Is_Complete = is_Complete;
            Lab_Code = lab_Code;
            Lab_Report_ID = lab_Report_ID;
            Lab_Report_Ver = lab_Report_Ver;
            Matching = matching;
            Organization = organization;
            Package_Condition = package_Condition;
            Report_Date = report_Date;
            Report_Notes = report_Notes;
            Sample_Form_Num = sample_Form_Num;
            Test_Results = test_Results;
            Tester_Name = tester_Name;
        }


    }
}
