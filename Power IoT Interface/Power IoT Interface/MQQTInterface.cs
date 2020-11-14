using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Power_IoT_Interface.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Linq;
using System.Text;
using RabbitMQ.Client;
using System.Threading;
using RabbitMQ.Client.Events;
using InfluxDB.Client.Core.Flux.Domain;

namespace Power_IoT_Interface
{
    public class MQQTInterface
    {
        private IConfigurationRoot envconfiguration;
        private List<Equipment> allEquipments = new List<Equipment>();
        private List<Sensor> allSensors = new List<Sensor>();
        private List<Site> allSites = new List<Site>();
        private List<EquipmentType> allEquipmentType = new List<EquipmentType>();
        private List<Alarm> allAlarms = new List<Alarm>();

        private Exception ex = null;
        private ConnectionFactory factory = null;

        public MQQTInterface()
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true);

            envconfiguration = builder.Build();




            // init the fields
            initFields();
            //init sites
            allSites = GetData<Site>("Site", Site.fieldIds);

            // init sensors
            allSensors = GetData<Sensor>("Sensor", Sensor.fieldIds);

            //init equipment Types
            allEquipmentType = GetData<EquipmentType>("EquipmentType", EquipmentType.fieldIds);

            //init equipments
            allEquipments = GetData<Equipment>("Equipment", Equipment.fieldIds);

            // init Alarms
            allAlarms = GetData<Alarm>("Alarm", Alarm.fieldIds);



            // init queue

            var factory = new ConnectionFactory()
            {
                HostName = envconfiguration.GetSection("rabbitMQServer").Value,
                //   UserName = envconfiguration.GetSection("rabbitMQUserName").Value,
                //  Password = envconfiguration.GetSection("rabbitMQPassword").Value,
                //   VirtualHost = envconfiguration.GetSection("rabbitMQHost").Value
            };


            IConnection connection = null;
            IModel channel = null;

            try
            {
                var connected = false;
                var connRetryCount = 0;
                while (!connected && connRetryCount < 20)
                {
                    try
                    {
                        connRetryCount++;
                        connection = factory.CreateConnection();
                        connected = true;
                        channel = connection.CreateModel();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + ex.StackTrace);
                        Console.WriteLine("Sleeping for 5 sec before retry");
                        Thread.Sleep(5000);
                    }
                }
                if (connRetryCount >= 20)
                {
                    Console.WriteLine("Exhausted retry count while trying to connect to rmq");
                    return;
                }

                Dictionary<string, object> qargs = new Dictionary<string, object>();

                channel.ExchangeDeclare(envconfiguration.GetSection("DataExchange").Value, ExchangeType.Fanout, true, false, null);


                channel.QueueDeclare(queue: envconfiguration.GetSection("DBStoreQueue").Value, //MQ name where trend data will be posted
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: qargs);





                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (model, ea) =>
                {
                    if (ea.Body.Length > 0)
                    {
                        var body = ea.Body;
                        String message = "";
                        MQQTMessage rawData = null; ;
                        try
                        {
                            try
                            {
                                message = Encoding.UTF8.GetString(body.ToArray());
                            }
                            catch
                            {
                                throw new Exception("Unable to decode message");
                            }

                            try
                            {
                                rawData = JsonConvert.DeserializeObject<MQQTMessage>(message);

                                Equipment exp = allEquipments.Where(u => u.Record_ID.ToString().Equals(rawData.EquipmentId)).FirstOrDefault();

                                if (exp != null)
                                {
                                    Sensor sx = allSensors.Where(u => u.Tag.Equals(rawData.Tag)).FirstOrDefault();
                                    if (sx != null)
                                    {
                                        DateTime dt = DateTime.Parse(rawData.TimeStamp);
                                        DateTime myDt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                                        Alarm al = allAlarms.Where(u => u.Related_Sensor.Equals(sx.Record_ID.ToString())).FirstOrDefault();

                                        Console.WriteLine("Writing Data for " + exp.Equipment_Name + " - " + myDt.ToString("s"));

                                        InsertMQQTSensorData(exp, sx, myDt, al, rawData.Value);
                                    }
                                }

                            }
                            catch
                            {
                                throw new Exception("Unable to parse message");
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                    }

                };

                String consumerTab =
                channel.BasicConsume(queue: envconfiguration.GetSection("DBStoreQueue").Value,
                                   autoAck: true,
                                   consumer: consumer);

                channel.QueueBind(queue: envconfiguration.GetSection("DBStoreQueue").Value,
  exchange: envconfiguration.GetSection("DataExchange").Value,
  routingKey: "QB");
                Console.WriteLine("Listening press ENTER to quit");
                Console.ReadLine();

                channel.QueueUnbind(queue: envconfiguration.GetSection("DBStoreQueue").Value,
  exchange: envconfiguration.GetSection("DataExchange").Value, "QB", null);
            }
            catch (Exception conException)
            {
                Console.WriteLine("Error connecting to required service.", conException);
                Console.WriteLine(conException.Message);
            }



        }

        private void InsertMQQTSensorData(Equipment ex, Sensor sx, DateTime dt, Alarm al, String value)
        {

            try
            {

                List<decimal> allVal = new List<decimal>();

                List<SensorData> dr = new List<SensorData>();


                double v = 0;
                Double.TryParse(value, out v);

                SensorData sxx = new SensorData();
                sxx.Related_Equipment = ex.Record_ID.ToString();
                sxx.Related_Sensor = sx.Record_ID.ToString();
                sxx.TimeStamp = dt.ToString("s") + "Z";
                sxx.TimeStampDt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                sxx.Sensor_Name_Lookup = sx.Tag.ToLower().Replace("/", "_");
                sxx.Value = Math.Round(v, 4).ToString();
                sxx.DecValue = Math.Round(v, 4);

                sxx.Site = ex.Site_Name_Lookup;
                sxx.SiteId = ex.Related_Site;

                sxx.EquipmentName = ex.Equipment_Name;

                allVal.Add((decimal)sxx.DecValue);
                dr.Add(sxx);

                //Write to Influx
                InfluxConnector conn = new InfluxConnector(envconfiguration.GetSection("InfluxServer").Value,
                    envconfiguration.GetSection("InfluxToken").Value,
                    envconfiguration.GetSection("InfluxBucket").Value,
                    envconfiguration.GetSection("InfluxOrg").Value);

                conn.WriteData(dr);

                String measure = sx.Tag.Replace("/", "_");

                // Get data for current day from influx
                List<FluxTable> tab = conn.ReadData(ex.Record_ID.ToString(), dt, measure).Result;

                //Get data for current day from QB
                if (tab != null)
                {
                    if (tab.Count > 0)
                    {
                        FluxTable ft = tab[0];
                        if (ft.Records.Count > 0)
                        {
                            foreach (var rec in ft.Records)
                            {
                                String cx = rec.GetValue().ToString();
                                decimal dxm = 0;
                                if (Decimal.TryParse(cx, out dxm))
                                {
                                    allVal.Add(dxm);
                                }
                            }
                        }
                    }
                }
                DateTime start = new DateTime(dt.Year, dt.Month, dt.Day);
                SensorData s = new SensorData();
                s.Related_Equipment = ex.Record_ID.ToString();
                s.Related_Sensor = sx.Record_ID.ToString();
                s.TimeStamp = start.ToString("s") + "Z";
                s.TimeStampDt = DateTime.SpecifyKind(start, DateTimeKind.Utc);
                s.Sensor_Name_Lookup = sx.Tag.ToLower().Replace("/", "_");

                decimal sensorValue = 0;

                if (sx.Sensor_Category.Equals("Efficiency"))
                {
                    sensorValue = allVal.Average();
                }
                else
                {
                    sensorValue = allVal.Sum();
                }

                s.Value = sensorValue.ToString();

                List<SensorData> sensorData = GetData<SensorData>("SensorData", SensorData.fieldIds, "{15.EX.'" + ex.Record_ID + "'}AND{8.EX." +
                    sx.Record_ID + "}AND{11.OBF.'" + dt.ToString("yyyy-MM-dd") + "'}AND{11.AF.'" + dt.AddDays(-2).ToString("yyyy-MM-dd") + "'}");
                var ext = sensorData.Where(u => u.TimeStamp.Equals(dt.ToString("MM/dd/yyyy") + " 12:00:00 AM")).FirstOrDefault();

                if (ext != null)
                    s.Record_ID = ext.Record_ID;

                List<SensorData> dx = UpsertData<SensorData>("SensorData", s, SensorData.fieldIds);
                if (dx.Count > 0)
                {
                    // Alarm
                    if (al != null)
                    {
                        bool isAlarm = false;
                        decimal alVal = Decimal.Parse(al.Formula_Value);
                        if (al.Formula_Operator.Equals(">"))
                        {
                            if (sensorValue > alVal)
                                isAlarm = true;
                        }
                        else if (al.Formula_Operator.Equals("<"))
                        {
                            if (sensorValue < alVal)
                                isAlarm = true;
                        }
                        else if (al.Formula_Operator.Equals(">="))
                        {
                            if (sensorValue >= alVal)
                                isAlarm = true;
                        }
                        else if (al.Formula_Operator.Equals("<="))
                        {
                            if (sensorValue <= alVal)
                                isAlarm = true;
                        }
                        else if (al.Formula_Operator.Equals("=="))
                        {
                            if (sensorValue == alVal)
                                isAlarm = true;
                        }

                        if (isAlarm)
                        {
                            AlarmLog a = new AlarmLog();
                            a.Alarm_Text = al.Alarm_Name + " Raised - " + al.Priority + " for " + ex.Site_Name_Lookup + "-" + ex.Equipment_Name;
                            a.Related_Equipment = ex.Record_ID.ToString();
                            a.Related_Sensor_Datum = dx[0].Record_ID.ToString();
                            a.Related_Alarm = al.Record_ID.ToString();

                            List<AlarmLog> da = UpsertData<AlarmLog>("AlarmLog", a, AlarmLog.fieldIds);

                        }

                    }
                }
            }
            catch (Exception exx)
            { }


        }


     
        public void initFields()
        {
            Site.fieldIds = SetFieldIds(envconfiguration.GetSection("Site").Value);
            Equipment.fieldIds = SetFieldIds(envconfiguration.GetSection("Equipment").Value);
            EquipmentType.fieldIds = SetFieldIds(envconfiguration.GetSection("EquipmentType").Value);
            Sensor.fieldIds = SetFieldIds(envconfiguration.GetSection("Sensor").Value);
            SensorData.fieldIds = SetFieldIds(envconfiguration.GetSection("SensorData").Value);
            Alarm.fieldIds = SetFieldIds(envconfiguration.GetSection("Alarm").Value);

            AlarmLog.fieldIds = SetFieldIds(envconfiguration.GetSection("AlarmLog").Value);
            WorkOrder.fieldIds = SetFieldIds(envconfiguration.GetSection("WorkOrder").Value);


            Predictions.fieldIds = SetFieldIds(envconfiguration.GetSection("Prediction").Value);



        }


        public List<T> UpsertData<T>(String Name, T input, Dictionary<string, string> fieldIds)
        {

            try
            {

                String token = envconfiguration.GetSection("token").Value;
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(envconfiguration.GetSection("endpoint").Value + "records");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.Timeout = 200000;
                httpWebRequest.Headers.Add("Authorization", token);
                httpWebRequest.Headers.Add("QB-Realm-Hostname", envconfiguration.GetSection("realm").Value);


                InsertUpdateQuery query = new InsertUpdateQuery();



                query.to = envconfiguration.GetSection(Name).Value;

                Dictionary<string, object> dx = new Dictionary<string, object>();

                if (fieldIds != null)
                {
                    query.fieldsToReturn = new List<int>();

                    PropertyInfo[] properties = typeof(T).GetProperties();
                    foreach (PropertyInfo property in properties)
                    {
                        if (fieldIds.ContainsKey(property.Name))
                        {
                            String fieldId = fieldIds[property.Name];
                            if (property.Name.Equals("Record_ID"))
                            {
                                if (Int32.Parse(property.GetValue(input, null).ToString()) != 0)
                                {
                                    dx.Add(fieldId, new { value = property.GetValue(input, null) });
                                }
                            }
                            else if (!property.Name.Equals("Record_Owner") && !property.Name.Equals("Date_Created")
                                && !property.Name.Equals("Date_Modified") && !property.Name.Equals("Last_Modified_By")
                                && !property.Name.EndsWith("_Lookup"))
                            {
                                dx.Add(fieldId, new { value = property.GetValue(input, null) });
                            }
                            query.fieldsToReturn.Add(Int32.Parse(fieldId));
                        }

                    }
                    query.data.Add(dx);
                }



                String data = JsonConvert.SerializeObject(query);

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(data);
                }
                Console.WriteLine("Getting Response from QB Service ");

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();



                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    String result = streamReader.ReadToEnd();

                    var list = result.ToQBArray<T>(fieldIds);
                    return list;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }


        public List<T> GetData<T>(String Name, Dictionary<string, string> fieldIds, String whereClause = "")
        {

            try
            {

                String token = envconfiguration.GetSection("token").Value;
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(envconfiguration.GetSection("endpoint").Value + "records/query");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.Timeout = 200000;
                httpWebRequest.Headers.Add("Authorization", token);
                httpWebRequest.Headers.Add("QB-Realm-Hostname", envconfiguration.GetSection("realm").Value);


                Query query = new Query();
                query.where = whereClause;


                query.from = envconfiguration.GetSection(Name).Value;

                if (fieldIds != null)
                {
                    query.select = new List<int>();

                    PropertyInfo[] properties = typeof(T).GetProperties();
                    foreach (PropertyInfo property in properties)
                    {
                        if (fieldIds.ContainsKey(property.Name) || fieldIds.ContainsKey(property.Name.Replace("_Lookup", "")))
                        {
                            String fieldId = fieldIds[property.Name.Replace("_Lookup", "")];
                            query.select.Add(Int32.Parse(fieldId));
                        }

                    }
                }

                String data = JsonConvert.SerializeObject(query);

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(data);
                }
                Console.WriteLine("Getting Response from QB Service ");

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();



                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    String result = streamReader.ReadToEnd();
                    /* T eq = new Equipment();
                     if(Equipment.fieldIds == null)
                     {
                         Equipment.fieldIds = SetFieldIds(envconfiguration.GetSection("Equipment").Value);
                     }*/
                    var equipmentList = result.ToQBArray<T>(fieldIds);
                    return equipmentList;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public Dictionary<string, string> SetFieldIds(String tableId)
        {
            Console.WriteLine("Getting fields for " + tableId);

            try
            {
                String token = envconfiguration.GetSection("token").Value;
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(envconfiguration.GetSection("endpoint").Value + "/fields?tableId=" + tableId);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "GET";
                httpWebRequest.Timeout = 200000;
                httpWebRequest.Headers.Add("Authorization", token);
                httpWebRequest.Headers.Add("QB-Realm-Hostname", envconfiguration.GetSection("realm").Value);

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();



                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    String result = streamReader.ReadToEnd();

                    var a = result.ToQBFields();
                    return a;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }


        private void GenerateRandomData()
        {
            Random r = new Random();

            //DateTime dt = new DateTime(2020, 11, 1);
            //   DateTime myDt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);

            //Sensor sx = allSensors.Where(u => u.Related_Equipment_Type.Equals(allEquipments[0].Related_Equipment_Type)).FirstOrDefault();

            int eqp = 0;
            foreach (Equipment ex in allEquipments)
            {
                eqp++;
                for (int i = 1; i < 16; i++)
                {
                    DateTime dt = new DateTime(2020, 11, 1 + i);
                    DateTime myDt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    Console.WriteLine("Writing Data for " + ex.Equipment_Name + " - " + myDt.ToString("s"));

                    int rn = r.Next(0, 5);

                    if (rn > 3)
                    {
                        // 20% change
                        int num = 5 - rn;

                        for (int j = 0; j < num; j++)
                        {
                            int type = r.Next(0, 3);
                            String tp = "Wear";
                            String text = "Wear expected to occur";
                            switch (type)
                            {
                                case 1: tp = "Leakage"; text = "Leakage may occur in the equipment"; break;
                                case 2:
                                    tp = "Low Efficiency";
                                    int prob = r.Next(10, 40);
                                    text = "Equipment might reach " + prob + "% efficiency"; break;
                                case 3: tp = "Repair"; text = "Repair expected for the equipment"; break;

                            }
                            int sens = r.Next(0, 5);
                            int pos = r.Next(40, 100);
                            Predictions s = new Predictions();
                            s.Related_Equipment = ex.Record_ID.ToString();
                            s.Related_Sensor = allSensors.Where(u => u.Related_Equipment_Type.Equals(ex.Related_Equipment_Type)).ToList()[sens].Record_ID.ToString();
                            s.Date_Predicted = myDt.ToString("s") + "Z";
                            s.Possibility_Per = pos.ToString();
                            s.ML_Prediction = text;
                            s.Status = "Open";
                            s.ML_Prediction_Type = tp;
                            List<Predictions> dtp = UpsertData<Predictions>("Prediction", s, Predictions.fieldIds);

                        }
                    }

                    /*foreach (Sensor sx in allSensors.Where(u => u.Related_Equipment_Type.Equals(ex.Related_Equipment_Type)))
                    {
                        InsertSensorData(ex, sx,
                 myDt,
                 allAlarms.Where(u => u.Related_Sensor.Equals(sx.Record_ID.ToString())).FirstOrDefault());

                    }
                    */
                }
                // System.Threading.Thread.Sleep(3000);

            }
        }

        private void InsertSensorData(Equipment ex, Sensor sx, DateTime dt, Alarm al)
        {

            try
            {

                decimal value = 0;
                List<decimal> allVal = new List<decimal>();

                List<SensorData> dr = new List<SensorData>();

                DateTime dstart = new DateTime(dt.Year, dt.Month, dt.Day);

                DateTime dend = dt.AddHours(24);
                Random rx = new Random();
                while (dstart < dend)
                {
                    double v = 0;
                    if (sx.Sensor_Category.Equals("Efficiency"))
                    {
                        v = rx.NextDouble() * 100;
                        if (v < 50)
                            v += 40;
                        if (v > 90)
                            v = 90;
                    }
                    else if (sx.Sensor_Category.Equals("Up Time"))
                    {
                        v = rx.NextDouble() * 3;
                        if (v < 1)
                            v += 1;
                        if (v > 3)
                            v = 3;

                    }
                    else if (sx.Sensor_Category.Equals("Power Consumed"))
                    {
                        v = (0.3 + rx.NextDouble()) * 15;
                    }
                    else if (sx.Sensor_Category.Equals("Cost"))
                    {
                        v = rx.NextDouble() * 10;
                    }
                    else
                    {
                        v = (0.3 + rx.NextDouble()) * 20;
                    }
                    SensorData sxx = new SensorData();
                    sxx.Related_Equipment = ex.Record_ID.ToString();
                    sxx.Related_Sensor = sx.Record_ID.ToString();
                    sxx.TimeStamp = dstart.ToString("s") + "Z";
                    sxx.TimeStampDt = DateTime.SpecifyKind(dstart, DateTimeKind.Utc);
                    sxx.Sensor_Name_Lookup = sx.Tag.ToLower().Replace("/", "_");
                    sxx.Value = Math.Round(v, 4).ToString();
                    sxx.DecValue = Math.Round(v, 4);

                    sxx.Site = ex.Site_Name_Lookup;
                    sxx.SiteId = ex.Related_Site;

                    sxx.EquipmentName = ex.Equipment_Name;

                    allVal.Add((decimal)sxx.DecValue);
                    dr.Add(sxx);
                    dstart = dstart.AddHours(3);
                }

                InfluxConnector conn = new InfluxConnector(envconfiguration.GetSection("InfluxServer").Value,
                    envconfiguration.GetSection("InfluxToken").Value,
                    envconfiguration.GetSection("InfluxBucket").Value,
                    envconfiguration.GetSection("InfluxOrg").Value);
                conn.WriteData(dr);

                SensorData s = new SensorData();
                s.Related_Equipment = ex.Record_ID.ToString();
                s.Related_Sensor = sx.Record_ID.ToString();
                s.TimeStamp = dt.ToString("s") + "Z";
                s.TimeStampDt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                s.Sensor_Name_Lookup = sx.Tag.ToLower().Replace("/", "_");


                if (sx.Sensor_Category.Equals("Efficiency"))
                {
                    value = allVal.Average();
                }
                else
                {
                    value = allVal.Sum();
                }

                s.Value = value.ToString();


                List<SensorData> dx = UpsertData<SensorData>("SensorData", s, SensorData.fieldIds);
                if (dx.Count > 0)
                {
                    // Alarm
                    if (al != null)
                    {
                        bool isAlarm = false;
                        decimal alVal = Decimal.Parse(al.Formula_Value);
                        if (al.Formula_Operator.Equals(">"))
                        {
                            if (value > alVal)
                                isAlarm = true;
                        }
                        else if (al.Formula_Operator.Equals("<"))
                        {
                            if (value < alVal)
                                isAlarm = true;
                        }
                        else if (al.Formula_Operator.Equals(">="))
                        {
                            if (value >= alVal)
                                isAlarm = true;
                        }
                        else if (al.Formula_Operator.Equals("<="))
                        {
                            if (value <= alVal)
                                isAlarm = true;
                        }
                        else if (al.Formula_Operator.Equals("=="))
                        {
                            if (value == alVal)
                                isAlarm = true;
                        }

                        if (isAlarm)
                        {
                            AlarmLog a = new AlarmLog();
                            a.Alarm_Text = al.Alarm_Name + " Raised - " + al.Priority + " for " + ex.Site_Name_Lookup + "-" + ex.Equipment_Name;
                            a.Related_Equipment = ex.Record_ID.ToString();
                            a.Related_Sensor_Datum = dx[0].Record_ID.ToString();
                            a.Related_Alarm = al.Record_ID.ToString();

                            List<AlarmLog> da = UpsertData<AlarmLog>("AlarmLog", a, AlarmLog.fieldIds);

                        }

                    }
                }
            }
            catch (Exception exx)
            { }


        }


    }


}
