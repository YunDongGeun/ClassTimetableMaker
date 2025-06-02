using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClassTimetableMaker.Views
{
    /// <summary>
    /// Page1.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Page1 : Page
    {
        private readonly MainWindow _mainWindow;

        private readonly DBManager _dbManager;
        private Dictionary<string, TimeTableBlock> placedLectures = new(); // 배치된 강의들
        private Dictionary<string, OriginalPosition> originalPositions = new(); // 드래그 시작 위치 저장
        private List<TimeTableBlock> availableLectures = new(); // 사용 가능한 강의 목록

        public Page1(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            // DB 매니저 초기화
            string server = ConfigurationManager.AppSettings["DbServer"] ?? "localhost";
            int port = int.Parse(ConfigurationManager.AppSettings["DbPort"] ?? "3306");
            string database = ConfigurationManager.AppSettings["DbName"] ?? "class_time_table_maker";
            string username = ConfigurationManager.AppSettings["DbUser"];
            string password = ConfigurationManager.AppSettings["DbPassword"];

            _dbManager = new DBManager(server, port, database, username, password);

            // 강의 목록 로드
            LoadLectures();
        }

        // DB에서 강의 목록 로드
        private async void LoadLectures()
        {
            try
            {
                availableLectures = await _dbManager.GetTimeTableBlocksAsync();
                PopulateLectureList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"강의 목록 로드 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 강의 목록을 UI에 표시
        private void PopulateLectureList()
        {
            LectureListPanel.Children.Clear();

            foreach (var lecture in availableLectures)
            {
                var lectureItem = CreateLectureItem(lecture);
                LectureListPanel.Children.Add(lectureItem);
            }
        }

        // 드래그 가능한 강의 아이템 생성
        private Border CreateLectureItem(TimeTableBlock lecture)
        {
            var border = new Border
            {
                Margin = new Thickness(5),
                Padding = new Thickness(8),
                Background = new SolidColorBrush(GetLectureColor(lecture.Grade)),
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Cursor = Cursors.Hand,
                Tag = lecture
            };

            var stackPanel = new StackPanel();

            var classNameText = new TextBlock
            {
                Text = lecture.ClassName,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };

            var professorText = new TextBlock
            {
                Text = lecture.ProfessorName,
                FontSize = 10,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };

            var gradeText = new TextBlock
            {
                Text = lecture.Grade,
                FontSize = 9,
                Foreground = Brushes.LightGray,
                TextWrapping = TextWrapping.Wrap
            };

            stackPanel.Children.Add(classNameText);
            stackPanel.Children.Add(professorText);
            stackPanel.Children.Add(gradeText);

            border.Child = stackPanel;

            // 드래그 이벤트 추가
            border.MouseLeftButtonDown += LectureItem_MouseLeftButtonDown;

            return border;
        }

        // 학년별 색상 지정
        private Color GetLectureColor(string grade)
        {
            return grade switch
            {
                "1학년" => Colors.CornflowerBlue,
                "2학년" => Colors.ForestGreen,
                "3학년" => Colors.Orange,
                "4학년" => Colors.Crimson,
                "대학원" => Colors.Purple,
                _ => Colors.Gray
            };
        }

        // 강의 아이템 마우스 다운 이벤트 (드래그 시작)
        private void LectureItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border lectureItem && lectureItem.Tag is TimeTableBlock lecture)
            {
                // 원래 위치 저장
                originalPositions[lecture.Id.ToString()] = new OriginalPosition
                {
                    Parent = lectureItem.Parent as Panel,
                    Item = lectureItem
                };

                // 드래그 시작
                DragDrop.DoDragDrop(lectureItem, new LectureDragData(lecture, lectureItem), DragDropEffects.Move);
            }
        }

        // 셀 드래그 엔터 이벤트
        private void Cell_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is Border cell && e.Data.GetData(typeof(LectureDragData)) is LectureDragData dragData)
            {
                // 드롭 가능한지 검사
                if (CanDropLecture(cell, dragData.Lecture))
                {
                    cell.Background = new SolidColorBrush(Color.FromArgb(100, 144, 238, 144)); // 연한 초록색
                    e.Effects = DragDropEffects.Move;
                }
                else
                {
                    cell.Background = new SolidColorBrush(Color.FromArgb(100, 255, 182, 193)); // 연한 빨간색
                    e.Effects = DragDropEffects.None;
                }
            }
            e.Handled = true;
        }

        // 셀 드래그 리브 이벤트
        private void Cell_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is Border cell)
            {
                cell.Background = Brushes.White; // 원래 배경색으로 복원
            }
        }

        // 셀 드롭 이벤트
        private void Cell_Drop(object sender, DragEventArgs e)
        {
            if (sender is Border targetCell && e.Data.GetData(typeof(LectureDragData)) is LectureDragData dragData)
            {
                targetCell.Background = Brushes.White; // 배경색 복원

                // 드롭 가능한지 다시 한번 검사
                if (!CanDropLecture(targetCell, dragData.Lecture))
                {
                    ShowConstraintViolationMessage(targetCell, dragData.Lecture);
                    return;
                }

                // 기존 위치에서 제거
                RemoveLectureFromOriginalPosition(dragData);

                // 새 위치에 배치
                PlaceLectureInCell(targetCell, dragData.Lecture);

                // 상태 업데이트
                UpdateStatusText();
            }
        }

        // 강의를 셀에 배치
        private void PlaceLectureInCell(Border targetCell, TimeTableBlock lecture)
        {
            var cellPosition = targetCell.Tag.ToString();
            var cellContent = FindCellContent(targetCell);

            if (cellContent != null)
            {
                // 기존 내용 제거
                cellContent.Children.Clear();

                // 새 강의 아이템 생성
                var lectureBlock = CreateCellLectureItem(lecture);
                cellContent.Children.Add(lectureBlock);

                // 배치된 강의 기록
                placedLectures[cellPosition] = lecture;
            }
        }

        // 셀용 강의 아이템 생성 (시간표에 표시되는 버전)
        private Border CreateCellLectureItem(TimeTableBlock lecture)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(GetLectureColor(lecture.Grade)),
                Padding = new Thickness(2)
            };

            var stackPanel = new StackPanel();

            var classNameText = new TextBlock
            {
                Text = lecture.ClassName,
                FontWeight = FontWeights.Bold,
                FontSize = 10,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };

            var professorText = new TextBlock
            {
                Text = lecture.ProfessorName,
                FontSize = 8,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };

            stackPanel.Children.Add(classNameText);
            stackPanel.Children.Add(professorText);

            border.Child = stackPanel;

            // 우클릭 메뉴 추가
            var contextMenu = new ContextMenu();
            var removeMenuItem = new MenuItem
            {
                Header = "시간표에서 제거",
                Icon = new TextBlock { Text = "✖", Foreground = Brushes.Red }
            };
            removeMenuItem.Click += (s, args) => RemoveLectureFromTimetable(border, lecture);
            contextMenu.Items.Add(removeMenuItem);

            border.ContextMenu = contextMenu;
            border.Tag = lecture;

            return border;
        }

        // 시간표에서 강의 제거
        private void RemoveLectureFromTimetable(Border lectureBlock, TimeTableBlock lecture)
        {
            // 셀에서 제거
            if (lectureBlock.Parent is Grid cellContent)
            {
                cellContent.Children.Remove(lectureBlock);
            }

            // 배치된 강의 목록에서 제거
            var cellToRemove = placedLectures.FirstOrDefault(x => x.Value.Id == lecture.Id);
            if (!cellToRemove.Equals(default(KeyValuePair<string, TimeTableBlock>)))
            {
                placedLectures.Remove(cellToRemove.Key);
            }

            // 왼쪽 목록에 다시 추가
            var lectureItem = CreateLectureItem(lecture);
            LectureListPanel.Children.Add(lectureItem);

            UpdateStatusText();
        }

        // 셀의 콘텐츠 그리드 찾기
        private Grid FindCellContent(Border cell)
        {
            return cell.Child as Grid;
        }

        // 원래 위치에서 강의 제거
        private void RemoveLectureFromOriginalPosition(LectureDragData dragData)
        {
            if (dragData.OriginalItem.Parent is Panel parent)
            {
                parent.Children.Remove(dragData.OriginalItem);
            }
        }

        // 강의 배치 가능 여부 검사
        private bool CanDropLecture(Border targetCell, TimeTableBlock lecture)
        {
            var cellPosition = targetCell.Tag.ToString();
            var parts = cellPosition.Split(',');
            int period = int.Parse(parts[0]);
            int day = int.Parse(parts[1]);

            // 1. 이미 다른 강의가 배치되어 있는지 확인
            if (placedLectures.ContainsKey(cellPosition))
            {
                return false;
            }

            // 2. 교수님 불가능한 시간 확인
            if (IsUnavailableTime(lecture, day, period))
            {
                return false;
            }

            // 3. 학교 지정 시간인 경우 해당 시간에만 배치 가능
            if (lecture.IsFixedTime && !IsFixedTime(lecture, day, period))
            {
                return false;
            }

            // 4. 같은 시간대에 같은 학년 수업이 3개 이상인지 확인
            if (CountSameGradeLecturesAtTime(lecture.Grade, period) >= 3)
            {
                return false;
            }

            return true;
        }

        // 교수님 불가능한 시간인지 확인
        private bool IsUnavailableTime(TimeTableBlock lecture, int day, int period)
        {
            var dayNames = new[] { "", "월요일", "화요일", "수요일", "목요일", "금요일" };
            var dayName = dayNames[day];

            // 오전(1-4교시), 오후(5-9교시) 구분
            var timeSlot = period <= 4 ? "오전" : "오후";

            // 불가능한 시간 슬롯들 확인
            var unavailableSlots = new[]
            {
                lecture.UnavailableSlot1,
                lecture.UnavailableSlot2,
                lecture.AdditionalUnavailableSlot1,
                lecture.AdditionalUnavailableSlot2
            };

            foreach (var slot in unavailableSlots)
            {
                if (!string.IsNullOrEmpty(slot) && slot.Contains(dayName) && slot.Contains(timeSlot))
                {
                    return true;
                }
            }

            return false;
        }

        // 학교 지정 시간인지 확인
        private bool IsFixedTime(TimeTableBlock lecture, int day, int period)
        {
            if (string.IsNullOrEmpty(lecture.FixedTimeSlot))
                return false;

            var dayNames = new[] { "", "월요일", "화요일", "수요일", "목요일", "금요일" };
            var dayName = dayNames[day];

            // 지정된 시간 파싱 (예: "월요일 3")
            var parts = lecture.FixedTimeSlot.Split(' ');
            if (parts.Length >= 2)
            {
                var fixedDay = parts[0];
                if (int.TryParse(parts[1], out int fixedPeriod))
                {
                    return fixedDay == dayName && fixedPeriod == period;
                }
            }

            return false;
        }

        // 같은 시간대에 같은 학년 강의 개수 확인
        private int CountSameGradeLecturesAtTime(string grade, int period)
        {
            int count = 0;
            foreach (var placed in placedLectures)
            {
                var cellPosition = placed.Key;
                var lecture = placed.Value;
                var parts = cellPosition.Split(',');
                int placedPeriod = int.Parse(parts[0]);

                if (placedPeriod == period && lecture.Grade == grade)
                {
                    count++;
                }
            }
            return count;
        }

        // 제약조건 위반 메시지 표시
        private void ShowConstraintViolationMessage(Border targetCell, TimeTableBlock lecture)
        {
            var cellPosition = targetCell.Tag.ToString();
            var parts = cellPosition.Split(',');
            int period = int.Parse(parts[0]);
            int day = int.Parse(parts[1]);

            string message = "다음 제약조건으로 인해 배치할 수 없습니다:\n\n";

            if (placedLectures.ContainsKey(cellPosition))
            {
                message += "• 이미 다른 강의가 배치되어 있습니다.\n";
            }

            if (IsUnavailableTime(lecture, day, period))
            {
                message += "• 교수님이 불가능한 시간입니다.\n";
            }

            if (lecture.IsFixedTime && !IsFixedTime(lecture, day, period))
            {
                message += "• 학교 지정 시간이 아닙니다.\n";
            }

            if (CountSameGradeLecturesAtTime(lecture.Grade, period) >= 3)
            {
                message += "• 같은 시간대에 같은 학년 수업이 이미 3개 배치되어 있습니다.\n";
            }

            MessageBox.Show(message, "배치 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // 상태 텍스트 업데이트
        private void UpdateStatusText()
        {
            int totalLectures = availableLectures.Count;
            int placedCount = placedLectures.Count;
            int remainingCount = totalLectures - placedCount;

            StatusText.Text = $"전체 강의: {totalLectures}개 | 배치된 강의: {placedCount}개 | 남은 강의: {remainingCount}개";
        }

        // 과목 추가 버튼 클릭 이벤트
        private void AddLectureButton_Click(object sender, RoutedEventArgs e)
        {
            // InputPage로 이동하거나 새 강의 추가 다이얼로그 표시
            _mainWindow.NavigateToInputPage();
            //MessageBox.Show("새 강의를 추가하려면 입력 화면을 이용해주세요.", "정보", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // 드래그 데이터 클래스
    public class LectureDragData
    {
        public TimeTableBlock Lecture { get; set; }
        public Border OriginalItem { get; set; }

        public LectureDragData(TimeTableBlock lecture, Border originalItem)
        {
            Lecture = lecture;
            OriginalItem = originalItem;
        }
    }

    // 원래 위치 정보 클래스
    public class OriginalPosition
    {
        public Panel Parent { get; set; }
        public Border Item { get; set; }
    }
}
