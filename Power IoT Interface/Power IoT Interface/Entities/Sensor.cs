using System;
using System.Collections.Generic;
using System.Text;

namespace Power_IoT_Interface.Entities
{
    public class Sensor : BaseEntity
    {
        public String Sensor_Name { get; set; } = "";
        public String Tag { get; set; } = "";
        public String Unit { get; set; } = "";
        public String Related_Equipment_Type { get; set; } = "";

        public String Equipment_Type_Lookup { get; set; } = "";
        public String Sensor_Category { get; set; } = "";

        public String Equipment_Tag_Code { get; set; } = "";

        public static Dictionary<string, string> fieldIds { get; set; }

    }
}
