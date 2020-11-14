using System;
using System.Collections.Generic;
using System.Text;

namespace Power_IoT_Interface.Entities
{
    public class Predictions : BaseEntity
    {
        public String ML_Prediction { get; set; } = "";
        public String Date_Predicted { get; set; } = "";
        public String Status { get; set; } = "";
        public String Possibility_Per { get; set; } = "";
        public String Related_Sensor { get; set; } = "";
        public String Related_Equipment { get; set; } = "";
        public String ML_Prediction_Type { get; set; } = "";

        public static Dictionary<string, string> fieldIds { get; set; }

    }
}
