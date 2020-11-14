using System;
using System.Collections.Generic;
using System.Text;

namespace Power_IoT_Interface.Entities
{
    public class EquipmentType : BaseEntity
    {
        public String Equipment_Type { get; set; } = "";
        public static Dictionary<string, string> fieldIds { get; set; }


    }
}
