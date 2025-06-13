using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ClassTimetableMaker.Model
{
    // 1. 교과목 뷰모델 클래스
    public class SubjectViewModel
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

        // UI 표시용 프로퍼티들
        public string SectionNames => string.Join(", ", GetSectionNamesList());
        public string ProfessorNames => string.Join(", ", SelectedProfessors?.Select(p => p.ProfessorName + (p.IsPrimary ? "(주담당)" : "")) ?? new string[0]);
        public Visibility ContinuousVisibility => IsContinuous ? Visibility.Visible : Visibility.Collapsed;

        // 선택된 교수 목록
        public List<ProfessorSelection> SelectedProfessors { get; set; } = new List<ProfessorSelection>();

        // 헬퍼 메서드들
        public List<string> GetSectionNamesList()
        {
            var sections = new List<string>();
            for (int i = 0; i < SectionCount; i++)
            {
                sections.Add(((char)('A' + i)).ToString());
            }
            return sections;
        }

        // Subject 모델로 변환
        public Subject ToSubject()
        {
            return new Subject
            {
                Id = this.Id,
                Name = this.Name,
                Grade = this.Grade,
                CourseType = this.CourseType,
                LectureHours1 = this.LectureHours1,
                LectureHours2 = this.LectureHours2,
                SectionCount = this.SectionCount,
                IsContinuous = this.IsContinuous,
                ContinuousHours = this.ContinuousHours
            };
        }

        // Subject에서 ViewModel로 변환
        public static SubjectViewModel FromSubject(Subject subject)
        {
            return new SubjectViewModel
            {
                Id = subject.Id,
                Name = subject.Name,
                Grade = subject.Grade,
                CourseType = subject.CourseType,
                LectureHours1 = subject.LectureHours1,
                LectureHours2 = subject.LectureHours2,
                SectionCount = subject.SectionCount,
                IsContinuous = subject.IsContinuous,
                ContinuousHours = subject.ContinuousHours,
                SelectedProfessors = subject.SubjectProfessors?.Select(sp => new ProfessorSelection
                {
                    ProfessorId = sp.ProfessorId,
                    ProfessorName = sp.Professor?.Name,
                    IsPrimary = sp.IsPrimary
                }).ToList() ?? new List<ProfessorSelection>()
            };
        }
    }

    // 2. 교수 체크박스 관리 클래스
    public class ProfessorCheckBox
    {
        public CheckBox CheckBox { get; set; }
        public RadioButton PrimaryRadioButton { get; set; }
        public Professor Professor { get; set; }

        public bool IsSelected => CheckBox?.IsChecked == true;
        public bool IsPrimary => PrimaryRadioButton?.IsChecked == true;
    }

    // 3. 교수 선택 정보 클래스
    public class ProfessorSelection
    {
        public int ProfessorId { get; set; }
        public string ProfessorName { get; set; }
        public bool IsPrimary { get; set; }
    }
}
