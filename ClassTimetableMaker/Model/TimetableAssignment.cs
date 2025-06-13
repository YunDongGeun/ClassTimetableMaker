using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTimetableMaker.Model
{

    // 5. 시간표 배치 모델
    public class TimetableAssignment
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public int ProfessorId { get; set; }
        public int? ClassroomId { get; set; }
        public int DayOfWeek { get; set; } // 1:월, 2:화, 3:수, 4:목, 5:금
        public int Period { get; set; } // 1-9교시
        public string SectionName { get; set; } = "A";
        public int InstanceIndex { get; set; } = 0; // 0: 1차시, 1: 2차시
        public int Duration { get; set; } = 1; // 연강 지속 시간
        public DateTime CreatedAt { get; set; }

        // 네비게이션 프로퍼티
        public Subject Subject { get; set; }
        public Professor Professor { get; set; }
        public Classroom Classroom { get; set; }

        // 헬퍼 메서드들
        public string GetDayName()
        {
            var dayNames = new[] { "", "월", "화", "수", "목", "금" };
            return dayNames[DayOfWeek];
        }

        public string GetTimeSlotDescription()
        {
            if (Duration == 1)
                return $"{GetDayName()}요일 {Period}교시";
            else
                return $"{GetDayName()}요일 {Period}~{Period + Duration - 1}교시";
        }

        public bool IsConflictWith(TimetableAssignment other)
        {
            if (DayOfWeek != other.DayOfWeek)
                return false;

            // 시간 겹침 확인
            int thisStart = Period;
            int thisEnd = Period + Duration - 1;
            int otherStart = other.Period;
            int otherEnd = other.Period + other.Duration - 1;

            return !(thisEnd < otherStart || otherEnd < thisStart);
        }
    }

}
