using System;
using System.Collections.Generic;
using System.Text;

namespace Power_IoT_Interface.Entities
{
    public class BaseEntity
    {
        public int Record_ID { get; set; } = 0;
        public BaseUser Record_Owner { get; set; }
        public String Date_Created { get; set; } = "";
        public String Date_Modified { get; set; } = "";
        public BaseUser Last_Modified_By { get; set; }

    }

    public class BaseUser
    {
        public String id { get; set; } = "";
        public String email { get; set; } = "";
    }
}
