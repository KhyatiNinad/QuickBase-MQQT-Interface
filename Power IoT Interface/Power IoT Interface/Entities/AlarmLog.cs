using System;
using System.Collections.Generic;
using System.Text;

namespace Power_IoT_Interface.Entities
{
    public class AlarmLog : BaseEntity
    {
        public String Alarm_Text { get; set; } = "";
        public String Related_Sensor_Datum { get; set; } = "";
        public String Related_Equipment { get; set; } = "";
        public String Related_Alarm { get; set; } = "";
        public static Dictionary<string, string> fieldIds { get; set; }


    }
}
