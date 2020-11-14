using System;
using System.Collections.Generic;
using System.Text;

namespace Power_IoT_Interface.Entities
{
    public class SensorData : BaseEntity
    {
        public String Related_Equipment { get; set; } = "";
        public String Related_Sensor { get; set; } = "";
        public String Value { get; set; } = "";

        public double DecValue { get; set; } = 0;
        public String TimeStamp { get; set; } = "";
        public String Sensor_Name_Lookup { get; set; } = "";

        public String SiteId { get; set; } = "";
        public String Site { get; set; } = "";
        public String EquipmentName { get; set; } = "";
        public DateTime TimeStampDt { get; set; } = DateTime.UtcNow;
        public static Dictionary<string, string> fieldIds { get; set; }

    }
}
