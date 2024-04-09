using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCS_OBJECTS;

namespace FcsLogic.SampleRequest
{
    public class Lab_Sample_Form_Response
    {


        public Amil_Details Amil_Details = new Amil_Details();

        public int barcode { get; set; }
        public string Barcode
        {
            get { return ""; }  //get { return barcode.HasValue ? barcode.ToString() : string.Empty; }
            set { if (!string.IsNullOrEmpty(value)) barcode = Int32.Parse(value); }
        }

        public string Batch_Num { get; set; }

        public string Container_Num { get; set; }

        public string Country_Name { get; set; }

        public int del_File_Num { get; set; }
        public string Del_File_Num
        {
            get { return ""; }
            set { if (!string.IsNullOrEmpty(value)) del_File_Num = Int32.Parse(value); }
        }

        public string Delivery_To_Lab { get; set; }

        public Expiry_Date Expiry_Date = new Expiry_Date();

        public Importer_Details Importer_Details = new Importer_Details();

        public string Importer_Store { get; set; }

        public string Inspector_Title { get; set; }

        public string Is_Vet { get; set; }

        public Manufacture_Date Manufacture_Date = new Manufacture_Date();

        public string Num_Of_Samples { get; set; }

        public int num_Of_Samples_Vet { get; set; }
        public string Num_Of_Samples_Vet
        {
            get { return ""; }
            set { if (!string.IsNullOrEmpty(value)) num_Of_Samples_Vet = Int32.Parse(value); }
        }

        public string Organization { get; set; }

        public string Packing_Type { get; set; }

        public int payer_ID { get; set; }
        public string Payer_ID
        {
            get { return ""; }
            set { if (!string.IsNullOrEmpty(value)) payer_ID = Int32.Parse(value); }
        }

        public int producer_Country { get; set; }
        public string Producer_Country
        {
            get { return ""; }
            set { if (!string.IsNullOrEmpty(value)) producer_Country = Int32.Parse(value); }
        }

        public string Producer_Name { get; set; }

        public string Product_Brand_Name { get; set; }

        public int product_Group_Code { get; set; }
        public string Product_Group_Code
        {
            get { return ""; }
            set { if (!string.IsNullOrEmpty(value)) product_Group_Code = Int32.Parse(value); }
        }

        public string Product_Group_Description { get; set; }

        public string Product_name_eng { get; set; }

        public string Product_name_heb { get; set; }

        public string Property_Plus { get; set; }

        public string Remark { get; set; }

        public int return_Code { get; set; }
        public string Return_Code
        {
            get { return ""; }
            set { if (!string.IsNullOrEmpty(value)) return_Code = Int32.Parse(value); }
        }

        public string Return_Code_Desc { get; set; }

        public int sample_Form_Num { get; set; }
        public string Sample_Form_Num
        {
            get { return ""; }
            set { if (!string.IsNullOrEmpty(value)) sample_Form_Num = Int32.Parse(value); }
        }

        public Sampling_Date Sampling_Date = new Sampling_Date();

        public string Sampling_Inspector { get; set; }

        public string Sampling_Place { get; set; }

        public string Sampling_Reason { get; set; }

        public double sampling_Temp { get; set; }
        public string Sampling_Temp
        {
            get { return ""; }
            set { if (!string.IsNullOrEmpty(value)) sampling_Temp = double.Parse(value); }
        }

        public string Sampling_Time { get; set; }

        public string Test_Description { get; set; }

        public int test_Sub_Code { get; set; }
        public string Test_Sub_Code
        {
            get { return ""; }
            set { if (!string.IsNullOrEmpty(value)) test_Sub_Code = Int32.Parse(value); }
        }

        internal ResponseToDB GetDbObj()
        {
            
       
          return new ResponseToDB(
               this.return_Code,
               this.Return_Code_Desc,
               this.barcode,
               this.sample_Form_Num,
               this.Is_Vet,
               this.Product_Group_Description,
               this.product_Group_Code,
               this.Product_name_heb,
               this.Product_name_eng,
               this.Product_Brand_Name,
               this.Organization,
               this.payer_ID,
               this.Producer_Name,
               this.producer_Country,
               this.Country_Name,
               this.Manufacture_Date._date,
               this.Sampling_Time,
               this.sampling_Temp,
               this.Expiry_Date._date,
               this.Batch_Num,
               this.Property_Plus,
               this.Sampling_Place,
               this.Sampling_Reason,
               this.Packing_Type,
               this.Delivery_To_Lab,
               this.Sampling_Inspector,
               this.Inspector_Title,
               this.Container_Num,
               this.Num_Of_Samples,
               this.num_Of_Samples_Vet,
               this.del_File_Num,
               this.Sampling_Date._date,
               this.Importer_Store,
               this.Remark,
               this.test_Sub_Code,
               this.Test_Description);

        }
    }
}