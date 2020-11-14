using System;
using System.Collections.Generic;
using System.Text;

namespace Power_IoT_Interface.Entities
{
    public class WorkOrder : BaseEntity
    {
        public BaseUser Assigned_To { get; set; }
        public String Description { get; set; } = "";
        public String Due_Date { get; set; } = "";
        public String Priority { get; set; } = "";
        public String Status { get; set; } = "";
        public String Related_Equipment { get; set; } = "";
        public String Related_Alarms_Log { get; set; } = "";

        public static Dictionary<string, string> fieldIds { get; set; }

    }
}
