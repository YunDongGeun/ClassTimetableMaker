using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTimetableMaker.Model
{
    public class Professor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PreferredTimeSlots { get; set; } // 예: "월요일,화요일,수요일"
        public string UnavailableTimeSlots { get; set; } // 예: "월요일오전,금요일오후"
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 헬퍼 메서드들
        public List<string> GetPreferredDays()
        {
            if (string.IsNullOrEmpty(PreferredTimeSlots))
                return new List<string>();
            return new List<string>(PreferredTimeSlots.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }

        public List<string> GetUnavailableSlots()
        {
            if (string.IsNullOrEmpty(UnavailableTimeSlots))
                return new List<string>();
            return new List<string>(UnavailableTimeSlots.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }

        public bool IsAvailable(int dayOfWeek, int period)
        {
            var dayNames = new[] { "", "월요일", "화요일", "수요일", "목요일", "금요일" };
            var dayName = dayNames[dayOfWeek];
            var timeSlot = period <= 4 ? "오전" : "오후";

            var unavailableSlots = GetUnavailableSlots();

            foreach (var slot in unavailableSlots)
            {
                if (slot.Contains(dayName) && (slot.Contains("전체") || slot.Contains(timeSlot)))
                    return false;

                // 특정 교시 체크 (예: "월요일1-3교시")
                if (slot.Contains(dayName) && slot.Contains($"{period}교시"))
                    return false;
            }

            return true;
        }
    }
}
