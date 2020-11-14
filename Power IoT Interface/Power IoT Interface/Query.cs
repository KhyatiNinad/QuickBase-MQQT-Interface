using System;
using System.Collections.Generic;
using System.Text;

namespace Power_IoT_Interface
{
    public class Query
    {
        public String from { get; set; } = "";
        public String where { get; set; } = "";
        public Options options { get; set; }
        public List<GroupBy> groupBy { get; set; }
        public List<int> select { get; set; }
        public List<SortBy> sortBy { get; set; }


    }
    public class SortBy
    {
        public int fieldId { get; set; } = 0;
        public String order { get; set; } = "";

    }
    public class GroupBy
    {
        public int fieldId { get; set; } = 0;
        public String grouping { get; set; } = "";

    }
    public class Options
    {
        public int skip { get; set; } = 0;
        public int top { get; set; } = 0;
        public bool compareWithAppLocalTime  { get; set; } = false;

    }


    public class InsertUpdateQuery
    {
        public String to { get; set; } = "";
        public List<Dictionary<string, object>> data { get; set; } = new List<Dictionary<string, object>>();
        public List<int> fieldsToReturn { get; set; }


    }
}
