using System;
using System.Collections.Generic;
using System.Text;

namespace Power_IoT_Interface.Entities
{
    public class Alarm : BaseEntity
    {
        public String Alarm_Name { get; set; } = "";
        public String Related_Sensor{ get; set; } = "";
        public String Formula_Operator { get; set; } = "";
        public String Formula_Value { get; set; } = "";

        public String Priority { get; set; } = "";

        public static Dictionary<string, string> fieldIds { get; set; }


    }
}
