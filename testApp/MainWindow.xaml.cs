using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace testApp;

public partial class MainWindow : Window
{
    public ScheduleViewModel ViewModel { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new ScheduleViewModel();
        DataContext = ViewModel;
    }

    private void ListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is Course selectedCourse)
        {
            ScheduleBlock block = new ScheduleBlock
            {
                Course = selectedCourse,
                Column = 0,
                Row = 0
            };

            DragDrop.DoDragDrop(listView, new DataObject(typeof(ScheduleBlock), block), DragDropEffects.Move);
        }
    }

    private void ScheduleGrid_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void ScheduleGrid_PreviewDrop(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement fe &&
            fe.Tag is string tag &&
            tag.Split(',') is [var rowStr, var colStr] &&
            int.TryParse(rowStr, out int row) &&
            int.TryParse(colStr, out int column))
        {
            if (e.Data.GetData(typeof(ScheduleBlock)) is ScheduleBlock droppedBlock)
            {
                // 다른 블럭이 해당 위치에 있는지 확인 (자기 자신은 예외)
                if (ViewModel.ScheduleBlocks.Any(b =>
                        b != droppedBlock &&
                        b.Row == row &&
                        b.Column == column))
                {
                    MessageBox.Show("해당 위치는 이미 사용 중입니다!", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 위치 갱신
                droppedBlock.Row = row;
                droppedBlock.Column = column;

                // UI 업데이트 유도: Remove 후 Add (ObservableCollection은 같은 객체일 경우 UI 리프레시 안함)
                ViewModel.ScheduleBlocks.Remove(droppedBlock);
                ViewModel.ScheduleBlocks.Add(droppedBlock);
            }
        }
    }

    private void AddTestData_Click(object sender, RoutedEventArgs e)
    {
        // 테스트 Course 객체 생성
        var testCourse = new Course
        {
            Name = "자료구조",
            Professor = "홍길동",
            StartTime = DateTime.Today.AddHours(14),   // 9시 시작
            EndTime = DateTime.Today.AddHours(18),    // 10시 종료
            Room = "101호"
        };

        // ViewModel에 Course 추가
        ViewModel.Courses.Add(testCourse);

        // ScheduleBlock 추가
        var block = new ScheduleBlock
        {
            Course = testCourse,
            Row = 1,     // 2번째 행
            Column = 2   // 3번째 열
        };

        // 중복 여부 확인 후 추가
        if (!ViewModel.ScheduleBlocks.Any(b => b.Row == block.Row && b.Column == block.Column))
        {
            ViewModel.ScheduleBlocks.Add(block);
            MessageBox.Show("테스트 데이터가 추가되었습니다.", "확인", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("이미 동일한 위치에 블럭이 있습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Block_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ScheduleBlock block)
        {
            DragDrop.DoDragDrop(fe, new DataObject(typeof(ScheduleBlock), block), DragDropEffects.Move);
        }
    }

}
