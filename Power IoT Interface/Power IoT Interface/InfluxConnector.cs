using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Writes;
using Power_IoT_Interface.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Power_IoT_Interface
{
    public class InfluxConnector
    {
         string token = "";
         string bucket = "";
         string org = "";
        InfluxDBClient client = null;
        public InfluxConnector(String server, String _token, String _bucket, String _org)
        {
            token = _token;
            bucket = _bucket;
            org = _org;

             client = InfluxDBClientFactory.Create(server, token.ToCharArray());
        }

        public void WriteData()
        {
            var point = PointData
  .Measurement("influxData")
  .Tag("assetId", "1")
  .Field("used_percent", 23.43234543)
  .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            using (var writeApi = client.GetWriteApi())
            {
                writeApi.WritePoint(bucket, org, point);
            }
        }


        public void WriteData(List<SensorData> sensorData)
        {
            var point = sensorData.Select(u => PointData
  .Measurement("influxData")
  .Tag("equipmentId", u.Related_Equipment)
  .Tag("equipment", u.EquipmentName)
.Tag("siteId", u.SiteId)
.Tag("site", u.Site)

  .Field(u.Sensor_Name_Lookup, u.DecValue)
  .Timestamp(u.TimeStampDt, WritePrecision.S)).ToList();

            using (var writeApi = client.GetWriteApi())
            {
                foreach(PointData pd in point)
                writeApi.WritePoint(bucket, org, pd);
            }
        }

        public async Task<List<FluxTable>> ReadData(String eqpId, DateTime dx, String measure)
        {
            DateTime start = new DateTime(dx.Year, dx.Month, dx.Day);
            String st = start.ToString("s") + "Z";
            String en = start.AddDays(1).AddMinutes(-1).ToString("s") + "Z";

            var query = $"from(bucket: \"{ bucket}\") " +
                " |> range(start: " + st + " , stop: " + en + ")" + 
                "|> filter(fn: (r) => r._measurement == \"influxData\" and r.equipmentId == \"" + eqpId + "\"  ) " +
                "   |> filter(fn: (r) => r[\"_field\"] == \"" + measure + "\")";

                ;
            var tables = client.GetQueryApi().QueryAsync(query, org).Result;
            return tables;

        }
    }
}
