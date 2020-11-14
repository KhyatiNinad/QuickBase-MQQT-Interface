using Power_IoT_Interface.Entities;
using System;

namespace Power_IoT_Interface
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Program...");
            //InfluxConnector inx = new InfluxConnector();
            //inx.WriteData();
            MQQTInterface mqqtInterface = new MQQTInterface();
            //mqqtInterface.GetData<Equipment>("Equipment", "", Equipment.fieldIds);
        }
    }
}
