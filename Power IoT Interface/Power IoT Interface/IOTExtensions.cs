using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Power_IoT_Interface
{
    public static class IOTExtensions
    {
        public static List<T> ToQBArray<T>(this string result, Dictionary<string, string> fields)
        {
            List<T> array = new List<T>();

            dynamic jsonresult = JsonConvert.DeserializeObject(result);

            if (fields == null)
            {
                fields = new Dictionary<string, string>();
                if (jsonresult.fields != null)
                {
                    foreach (var f in jsonresult.fields)
                    {
                        fields.Add(f.label.ToString().Replace(" ", "_").Replace("#", "").Replace("%", "Per").Replace("_Lookup", ""), f.id.ToString());
                    }
                }
            }
            if (jsonresult.data != null)
            {
                foreach (var d in jsonresult.data)
                {
                    T e = (T)Activator.CreateInstance(typeof(T));

                    PropertyInfo[] properties = typeof(T).GetProperties();
                    foreach (PropertyInfo property in properties)
                    {
                        if (fields.ContainsKey(property.Name) || fields.ContainsKey(property.Name.Replace("_Lookup", "")))
                        {
                            String fieldId = fields[property.Name.Replace("_Lookup", "")];

                            String value = "";
                            if (d.fieldId != null)
                            {
                                value = d.fieldId.value;
                            }
                            foreach (var x in d)
                            {
                                if (x.Name.ToString().Equals(fieldId))
                                {
                                    var m = x.First.value;
                                    if (m != null)
                                    {
                                        value = m.ToString();
                                    }
                                    break;

                                }
                            }
                            if(property.PropertyType.Equals(typeof (String)) )
                                property.SetValue(e, value);
                            else if (property.PropertyType.Equals(typeof(Int32)))
                                property.SetValue(e, Int32.Parse(value));
                            else
                            { 
                                try
                                {
                                    Type tx = property.PropertyType;
                                    // property.SetValue(e, Convert.ChangeType(JsonConvert.DeserializeObject(value), tx));

                                    var props = tx.GetProperties(BindingFlags.Public | BindingFlags.Instance)
    .Where(x => x.GetSetMethod() != null);
                                    // create an instance of the type
                                    var obj = Activator.CreateInstance(tx);

                                    // set property values using reflection
                                    var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(value);
                                    foreach (var prop in props)
                                        prop.SetValue(obj, values[prop.Name]);

                                    property.SetValue(e, obj);
                                }
                                catch(Exception exx)
                                { }
                            }
                        }
                    }

                    array.Add(e);
                }
            }
            return array;
        }
        public static Dictionary<string, string> ToQBFields(this string result)
        {
            Dictionary<string, string> fields = new Dictionary<string, string>();

            dynamic jsonresult = JsonConvert.DeserializeObject(result);

            if (jsonresult != null)
            {
                foreach (var f in jsonresult)
                {
                    fields.Add(f.label.ToString().Replace(" ", "_").Replace("#", "").Replace("%", "Per").Replace("_Lookup", ""), f.id.ToString());
                }
            }

            return fields;
        }
    }
}
