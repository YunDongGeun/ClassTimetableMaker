using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using GalaSoft.MvvmLight.CommandWpf;

namespace testApp
{
    public class ScheduleViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Course> Courses { get; set; } = new ObservableCollection<Course>();
        public ObservableCollection<ScheduleBlock> ScheduleBlocks { get; set; } = new ObservableCollection<ScheduleBlock>();

        private Course _selectedCourse;
        public Course SelectedCourse
        {
            get => _selectedCourse;
            set
            {
                _selectedCourse = value;
                OnPropertyChanged(nameof(SelectedCourse));
            }
        }

        public ICommand AddCourseCommand { get; }
        public ICommand DragCommand { get; }
        public ICommand DropCommand { get; }

        public ScheduleViewModel()
        {
            AddCourseCommand = new RelayCommand(AddCourse);
            DragCommand = new RelayCommand<ScheduleBlock>(StartDrag);
            DropCommand = new RelayCommand<object>(HandleDrop);
        }

        private void AddCourse()
        {
            if (Courses.Any(c => (c.StartTime < SelectedCourse.EndTime && c.EndTime > SelectedCourse.StartTime) &&
                                 (c.Professor == SelectedCourse.Professor || c.Room == SelectedCourse.Room)))
            {
                MessageBox.Show("시간 충돌이 발생했습니다!", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Courses.Add(new Course
            {
                Name = SelectedCourse.Name,
                Professor = SelectedCourse.Professor,
                StartTime = SelectedCourse.StartTime,
                EndTime = SelectedCourse.EndTime,
                Room = SelectedCourse.Room
            });
        }

        private void StartDrag(ScheduleBlock block)
        {
            DataObject data = new DataObject(typeof(ScheduleBlock), block);
            DragDrop.DoDragDrop(Application.Current.MainWindow, data, DragDropEffects.Move);
        }

        private void HandleDrop(object dropData)
        {
            if (dropData is ScheduleBlock droppedBlock)
            {
                if (ScheduleBlocks.Any(b => b.Row == droppedBlock.Row && b.Column == droppedBlock.Column))
                {
                    MessageBox.Show("해당 위치는 이미 사용 중입니다!", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ScheduleBlocks.Add(droppedBlock);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
