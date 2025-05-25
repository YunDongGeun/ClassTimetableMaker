using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTimetableMaker
{
    public class Professor
    {
        public string Name { get; set; }
        public List<TimeSlot> UnavailableSlots { get; set; } = new();
        public SpecialConstraintType ConstraintType { get; set; } // 지정 시간 or 추가 제약
        public List<TimeSlot> ExtraConstraintSlots { get; set; } // 0~2개
    }

    public enum SpecialConstraintType
    {
        None,           // 기본값 (선택하지 않음)
        FixedTime,      // 지정 시간
        AdditionalSlots // 추가 시간 제약
    }
}
