using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTimetableMaker.Model
{
    public class ClassroomViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsSaved { get; set; } = false;
        public bool IsTemp { get; set; } = true;
        public bool IsInUse { get; set; } = false;

        public static ClassroomViewModel FromClassroom(Classroom classroom)
        {
            return new ClassroomViewModel
            {
                Id = classroom.Id,
                Name = classroom.Name,
                CreatedAt = classroom.CreatedAt,
                UpdatedAt = classroom.UpdatedAt,
                IsSaved = true,
                IsTemp = false
            };
        }
    }

}
