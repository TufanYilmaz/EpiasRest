using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpiasRest
{
    public class EpiasRawIndex
    {
        public byte DEPARTMENT_ID;
        public int ID;
        public string EIC;
        public DateTime DATETIME_;
        public DateTime CALCDATETIME_;
        public int PERIOD;
        public int PROFILE_PERIOD;
        public double CONSUMPTION_VALUE;
        public double GENERATION_VALUE;

    }
}
