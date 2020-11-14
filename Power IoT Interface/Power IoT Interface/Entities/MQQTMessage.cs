using System;
using System.Collections.Generic;
using System.Text;

namespace Power_IoT_Interface.Entities
{
    public class MQQTMessage
    {
        public String TimeStamp { get; set; }
        public String Tag { get; set; }
        public String EquipmentId { get; set; }
        public String Value { get; set; }

    }
}
