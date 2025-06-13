using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTimetableMaker.Model
{
    // 7. 시간 슬롯 파싱 헬퍼 클래스
    public static class TimeSlotParser
    {
        private static readonly Dictionary<string, int> DayNameToNumber = new()
        {
            {"월요일", 1}, {"화요일", 2}, {"수요일", 3}, {"목요일", 4}, {"금요일", 5}
        };

        public static bool IsTimeSlotAvailable(string unavailableSlots, int dayOfWeek, int period)
        {
            if (string.IsNullOrEmpty(unavailableSlots))
                return true;

            var dayNames = new[] { "", "월요일", "화요일", "수요일", "목요일", "금요일" };
            var dayName = dayNames[dayOfWeek];
            var timeSlot = period <= 4 ? "오전" : "오후";

            var slots = unavailableSlots.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var slot in slots)
            {
                var trimmedSlot = slot.Trim();

                // 전체 요일 체크
                if (trimmedSlot.Contains(dayName) && trimmedSlot.Contains("전체"))
                    return false;

                // 오전/오후 체크
                if (trimmedSlot.Contains(dayName) && trimmedSlot.Contains(timeSlot))
                    return false;

                // 특정 교시 체크 (예: "월요일1-3교시" 또는 "모든요일1-2교시")
                if (IsSpecificPeriodUnavailable(trimmedSlot, dayOfWeek, period))
                    return false;
            }

            return true;
        }

        private static bool IsSpecificPeriodUnavailable(string slot, int dayOfWeek, int period)
        {
            // "모든요일1-2교시" 형태 체크
            if (slot.Contains("모든요일") && slot.Contains($"{period}교시"))
                return true;

            // 범위 체크 (예: "1-3교시")
            if (slot.Contains("-") && slot.Contains("교시"))
            {
                var dayNames = new[] { "", "월요일", "화요일", "수요일", "목요일", "금요일" };
                var dayName = dayNames[dayOfWeek];

                if (slot.Contains(dayName) || slot.Contains("모든요일"))
                {
                    var parts = slot.Split(new char[] { '-', '교', '시' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        if (int.TryParse(parts[^2], out int start) && int.TryParse(parts[^1], out int end))
                        {
                            return period >= start && period <= end;
                        }
                    }
                }
            }

            return false;
        }

        public static List<int> GetPreferredDays(string preferredTimeSlots)
        {
            var dayNumbers = new List<int>();

            if (string.IsNullOrEmpty(preferredTimeSlots))
                return dayNumbers;

            var days = preferredTimeSlots.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var day in days)
            {
                var trimmedDay = day.Trim();
                if (DayNameToNumber.ContainsKey(trimmedDay))
                {
                    dayNumbers.Add(DayNameToNumber[trimmedDay]);
                }
            }

            return dayNumbers;
        }
    }
}
