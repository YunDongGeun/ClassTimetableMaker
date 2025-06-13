using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTimetableMaker.Model
{
    // 6. 레거시 호환용 - 기존 TimeTableBlock과 유사한 뷰 모델 (UI 바인딩용)
    public class TimetableBlockViewModel
    {
        public int Id { get; set; }
        public string ClassName { get; set; }
        public string ProfessorName { get; set; }
        public string Grade { get; set; }
        public string Classroom { get; set; }
        public string CourseType { get; set; }
        public int LectureHours1 { get; set; }
        public int LectureHours2 { get; set; }
        public int SectionCount { get; set; }
        public bool IsContinuous { get; set; }
        public int Duration { get; set; } = 1;
        public string DisplayName { get; set; }
        public int InstanceIndex { get; set; } = 0;

        // Subject에서 ViewModel로 변환
        public static TimetableBlockViewModel FromSubject(Subject subject, Professor professor = null)
        {
            return new TimetableBlockViewModel
            {
                Id = subject.Id,
                ClassName = subject.Name,
                ProfessorName = professor?.Name ?? subject.GetPrimaryProfessor()?.Name,
                Grade = subject.Grade,
                CourseType = subject.CourseType,
                LectureHours1 = subject.LectureHours1,
                LectureHours2 = subject.LectureHours2,
                SectionCount = subject.SectionCount,
                IsContinuous = subject.IsContinuous,
                Duration = subject.ContinuousHours,
                DisplayName = subject.Name
            };
        }

        // 분반별 ViewModel 생성
        public static List<TimetableBlockViewModel> CreateSectionViewModels(Subject subject)
        {
            var viewModels = new List<TimetableBlockViewModel>();
            var sections = subject.GetSectionNames();

            foreach (var section in sections)
            {
                var viewModel = FromSubject(subject);
                viewModel.ClassName = $"{subject.Name}{section}";
                viewModel.DisplayName = $"{subject.Name}{section}";
                viewModel.Id = subject.Id * 1000 + sections.IndexOf(section); // 분반용 고유 ID
                viewModels.Add(viewModel);
            }

            return viewModels;
        }

        // 차시별 ViewModel 생성
        public static List<TimetableBlockViewModel> CreateInstanceViewModels(TimetableBlockViewModel sectionViewModel)
        {
            var instances = new List<TimetableBlockViewModel>();

            if (sectionViewModel.LectureHours1 > 0)
            {
                var instance1 = new TimetableBlockViewModel
                {
                    Id = sectionViewModel.Id * 100 + 0,
                    ClassName = sectionViewModel.ClassName,
                    ProfessorName = sectionViewModel.ProfessorName,
                    Grade = sectionViewModel.Grade,
                    Classroom = sectionViewModel.Classroom,
                    CourseType = sectionViewModel.CourseType,
                    Duration = sectionViewModel.LectureHours1,
                    InstanceIndex = 0,
                    DisplayName = sectionViewModel.LectureHours2 > 0 ?
                        $"{sectionViewModel.ClassName} (1차시)" : sectionViewModel.ClassName
                };
                instances.Add(instance1);
            }

            if (sectionViewModel.LectureHours2 > 0)
            {
                var instance2 = new TimetableBlockViewModel
                {
                    Id = sectionViewModel.Id * 100 + 1,
                    ClassName = sectionViewModel.ClassName,
                    ProfessorName = sectionViewModel.ProfessorName,
                    Grade = sectionViewModel.Grade,
                    Classroom = sectionViewModel.Classroom,
                    CourseType = sectionViewModel.CourseType,
                    Duration = sectionViewModel.LectureHours2,
                    InstanceIndex = 1,
                    DisplayName = $"{sectionViewModel.ClassName} (2차시)"
                };
                instances.Add(instance2);
            }

            return instances;
        }
    }
}
