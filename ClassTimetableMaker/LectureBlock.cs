using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTimetableMaker
{
    public class LectureBlock
    {
        public string SubjectName { get; set; }
        public string ProfessorName { get; set; }
        public int SectionCount { get; set; } // 분반 개수
        public TimeType TimeStructure { get; set; } // 2+1, 3연강 등
        public string Room { get; set; }
        public bool IsFixedTime { get; set; } // 지정 시간 여부
        public List<TimeSlot> FixedTimes { get; set; } = new();
    }
}
