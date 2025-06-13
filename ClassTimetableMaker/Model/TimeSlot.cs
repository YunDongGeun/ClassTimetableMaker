using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTimetableMaker.Model
{
    public class TimeSlot
    {
        public DayOfWeek Day { get; set; }
        public int Period { get; set; } // 교시 번호
    }
}
