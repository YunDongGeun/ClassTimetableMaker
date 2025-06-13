using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTimetableMaker.Model
{
    // 4. 교과목-교수 매핑 모델
    public class SubjectProfessor
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public int ProfessorId { get; set; }
        public bool IsPrimary { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        // 네비게이션 프로퍼티
        public Subject Subject { get; set; }
        public Professor Professor { get; set; }
    }
}
