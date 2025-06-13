using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTimetableMaker.Model
{
    // 3. 교과목 모델
    public class Subject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Grade { get; set; }
        public string CourseType { get; set; }
        public int LectureHours1 { get; set; } = 1;
        public int LectureHours2 { get; set; } = 0;
        public int SectionCount { get; set; } = 1;
        public bool IsContinuous { get; set; } = false;
        public int ContinuousHours { get; set; } = 1;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 연관 데이터
        public List<Professor> Professors { get; set; } = new List<Professor>();
        public List<SubjectProfessor> SubjectProfessors { get; set; } = new List<SubjectProfessor>();

        // 헬퍼 메서드들
        public Professor GetPrimaryProfessor()
        {
            return SubjectProfessors?.Find(sp => sp.IsPrimary)?.Professor;
        }

        public List<string> GetSectionNames()
        {
            var sections = new List<string>();
            for (int i = 0; i < SectionCount; i++)
            {
                sections.Add(((char)('A' + i)).ToString());
            }
            return sections;
        }

        public int GetTotalLectureHours()
        {
            return LectureHours1 + LectureHours2;
        }
    }
}
