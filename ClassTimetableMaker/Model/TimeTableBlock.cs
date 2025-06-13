using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTimetableMaker.Model
{
    public class TimeTableBlock
    {
        // 기존 필드들...
        public int Id { get; set; }
        public string ClassName { get; set; }
        public string ProfessorName { get; set; }
        public string Grade { get; set; }
        public string Classroom { get; set; }
        public bool IsFixedTime { get; set; }
        public string FixedTimeSlot { get; set; }
        public string UnavailableSlot1 { get; set; }
        public string UnavailableSlot2 { get; set; }
        public string AdditionalUnavailableSlot1 { get; set; }
        public string AdditionalUnavailableSlot2 { get; set; }

        // 새로 추가된 필드들
        public int? LectureHours_1 { get; set; }        // 첫 번째 차시 시간
        public int? LectureHours_2 { get; set; }        // 두 번째 차시 시간
        public int? SectionCount { get; set; }          // 분반 개수 (기본값: 1)

        // 내부 처리용 필드들 (DB에 저장되지 않음)
        public string DisplayName { get; set; }         // 화면 표시용 이름 (차시 정보 포함)
        public int Duration { get; set; } = 1;          // 연강 시간 (기본값: 1시간)
        public int InstanceIndex { get; set; } = 0;     // 차시 인덱스 (0: 첫 번째, 1: 두 번째)
    }
}
