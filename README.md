# QuickBase MQQT Interface

This is a .NET Core 3.0 based Micro Service to listen to MQQT Clients - RabbitMQ and read the incoming Sensor Data for Equipments.
The information in Sensor Data is matched with Equipment and Sensor Master Data hosted in Quick Base and data in inserted to Influx Database and then, daily summary data is stored back to QuickBase for Reporting. Alarms are also generated based on Sensor Data.

Expected MQQT Payload is:

```
{
"TimeStamp":"2020-11-14T13:38:58Z",
"Tag":"gasturbine/power",
"EquipmentId":"1",
"Value":"10.22"
}
```

Below Credentials/URLs are required, which can be configured in \Power IoT Interface\Power IoT InterfaceappSettings.json

```
* Influx DB - URL, Org, Bucket and Security Token
* Rabbit MQ - URL, Username, password
* Quickbase - Realm, Security Token and Table IDs
```