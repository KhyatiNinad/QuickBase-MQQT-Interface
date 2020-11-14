using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Power_IoT_Interface.Entities
{
    public class Equipment : BaseEntity
    {
        public String Equipment_Name { get; set; } = "";
        public String Equipment_Description { get; set; } = "";
        public String Site_Name_Lookup { get; set; } = "";
        public String Related_Site { get; set; } = "";

        public String Related_Equipment_Type { get; set; } = "";
        public String Equipment_Type_Lookup { get; set; } = "";
        public String Make { get; set; } = "";
        public String Model { get; set; } = "";
        public String Address_Lookup { get; set; } = "";
        public String Status { get; set; } = "";

        public static Dictionary<string, string> fieldIds { get; set; }


    }
}
