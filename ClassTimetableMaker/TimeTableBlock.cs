using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTimetableMaker
{
    public class TimeTableBlock
    {
        public string ProfessorName { get; set; }
        public string Classroom { get; set; }
        public string Grade { get; set; }
        public string CourseType { get; set; }
        public bool IsFixedTime { get; set; }
        public bool HasAdditionalRestrictions { get; set; }
        public string UnavailableSlot1 { get; set; }
        public string UnavailableSlot2 { get; set; }
        public string AdditionalUnavailableSlot1 { get; set; }
        public string AdditionalUnavailableSlot2 { get; set; }
        public int CourseHour1 { get; set; }
        public int CourseHour2 { get; set; }
    }
}
