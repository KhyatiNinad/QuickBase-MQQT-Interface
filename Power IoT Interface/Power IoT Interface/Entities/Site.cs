using System;
using System.Collections.Generic;
using System.Text;

namespace Power_IoT_Interface.Entities
{
    public class Site : BaseEntity
    {
        public String Site_Name { get; set; } = "";
        public String Type { get; set; } = "";
        public String Related_Area { get; set; } = "";
        public String Target { get; set; } = "";
        public static Dictionary<string, string> fieldIds { get; set; }


    }
}
