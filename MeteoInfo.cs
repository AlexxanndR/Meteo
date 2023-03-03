using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeteoTask
{
    internal class MeteoInfo
    {
        public string? Time { get; set; }

        public string? SensorName { get; set; }

        public float WindSpeed { get; set; }

        public float WindDirection { get; set; }
    }
}
