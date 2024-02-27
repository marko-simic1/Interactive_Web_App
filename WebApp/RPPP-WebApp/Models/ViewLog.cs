using System;

namespace RPPP_WebApp.Models
{
    public class ViewLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }


        public static ViewLog FromString(string logString)
        {
            return new ViewLog();
        }
    }
}
