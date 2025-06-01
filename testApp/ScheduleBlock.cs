using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testApp
{
    public class ScheduleBlock
    {
        public Course Course { get; set; }
        public int Column { get; set; } // 시간표에서 위치를 지정하기 위한 값
        public int Row { get; set; }    // 행 값 (시간 기준)
    }
}
