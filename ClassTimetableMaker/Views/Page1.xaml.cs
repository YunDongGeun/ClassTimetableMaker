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
    public partial class Page1 : Page
    {
        private readonly MainWindow _mainWindow;
        private readonly DBManager _dbManager;

        // 동적 시간표 관리를 위한 새로운 데이터 구조
        private Dictionary<string, List<TimeTableBlock>> timeSlotLectures = new(); // "period,day" -> 강의 리스트
        private Dictionary<string, int> dayColumnCounts = new(); // 각 요일별 열 개수 (최대 2개)
        private List<TimeTableBlock> availableLectures = new();

        // UI 요소들을 동적으로 관리
        private Dictionary<string, Border> cellBorders = new(); // "period,day,track" -> Border
        private Dictionary<string, Grid> cellContents = new(); // "period,day,track" -> Grid

        // 드래그 중인 강의가 시간표에서 온 것인지 추적
        private Dictionary<int, string> placedLecturePositions = new(); // 강의ID -> "period,day,track"

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

            // 초기 시간표 구조 설정
            InitializeDynamicTimetable();

            // 강의 목록 로드
            LoadLectures();
        }

        // 동적 시간표 초기화
        private void InitializeDynamicTimetable()
        {
            // 각 요일별 초기 열 개수를 1로 설정
            for (int day = 1; day <= 5; day++)
            {
                dayColumnCounts[day.ToString()] = 1;
            }

            // 모든 시간슬롯 초기화
            for (int period = 1; period <= 9; period++)
            {
                for (int day = 1; day <= 5; day++)
                {
                    string timeSlotKey = $"{period},{day}";
                    timeSlotLectures[timeSlotKey] = new List<TimeTableBlock>();
                }
            }

            // 초기 그리드 구성
            RebuildTimetableGrid();
        }

        // 시간표 그리드 재구성
        private void RebuildTimetableGrid()
        {
            TimetableGrid.Children.Clear();
            TimetableGrid.ColumnDefinitions.Clear();
            cellBorders.Clear();
            cellContents.Clear();

            // 열 정의: 교시 번호 + 각 요일별 동적 열들
            TimetableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) }); // 교시 번호

            int currentColumn = 1;
            for (int day = 1; day <= 5; day++)
            {
                int columnCount = dayColumnCounts[day.ToString()];
                for (int track = 1; track <= columnCount; track++)
                {
                    TimetableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    currentColumn++;
                }
            }

            // 헤더 생성
            CreateHeaders();

            // 셀 생성
            CreateCells();
        }

        // 헤더 생성
        private void CreateHeaders()
        {
            string[] dayNames = { "", "월", "화", "수", "목", "금" };

            int currentColumn = 1;
            for (int day = 1; day <= 5; day++)
            {
                int columnCount = dayColumnCounts[day.ToString()];

                if (columnCount == 1)
                {
                    // 단일 열인 경우
                    var headerBorder = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(5),
                        Background = new SolidColorBrush(Color.FromRgb(52, 58, 64))
                    };

                    var headerText = new TextBlock
                    {
                        Text = dayNames[day],
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    };

                    headerBorder.Child = headerText;
                    Grid.SetRow(headerBorder, 0);
                    Grid.SetColumn(headerBorder, currentColumn);
                    TimetableGrid.Children.Add(headerBorder);
                    currentColumn++;
                }
                else
                {
                    // 다중 열인 경우 - 요일명만 표시하고 열을 병합하는 효과
                    for (int track = 1; track <= columnCount; track++)
                    {
                        var headerBorder = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(track == 1 ? 1 : 0, 1, 1, 1), // 좌측 경계선 조정
                            Padding = new Thickness(5),
                            Background = new SolidColorBrush(Color.FromRgb(52, 58, 64))
                        };

                        var headerText = new TextBlock
                        {
                            Text = track == 1 ? dayNames[day] : "", // 첫 번째 열에만 요일명 표시
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White
                        };

                        headerBorder.Child = headerText;
                        Grid.SetRow(headerBorder, 0);
                        Grid.SetColumn(headerBorder, currentColumn);
                        TimetableGrid.Children.Add(headerBorder);
                        currentColumn++;
                    }
                }
            }

            // 교시 번호 헤더 (빈 셀)
            var cornerBorder = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromRgb(52, 58, 64))
            };
            Grid.SetRow(cornerBorder, 0);
            Grid.SetColumn(cornerBorder, 0);
            TimetableGrid.Children.Add(cornerBorder);
        }

        // 셀 생성
        private void CreateCells()
        {
            // 교시 번호 생성
            for (int period = 1; period <= 9; period++)
            {
                var periodBorder = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush(Color.FromRgb(248, 249, 250))
                };

                var periodText = new TextBlock
                {
                    Text = period.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold
                };

                periodBorder.Child = periodText;
                Grid.SetRow(periodBorder, period);
                Grid.SetColumn(periodBorder, 0);
                TimetableGrid.Children.Add(periodBorder);
            }

            // 수업 셀 생성
            for (int period = 1; period <= 9; period++)
            {
                int currentColumn = 1;
                for (int day = 1; day <= 5; day++)
                {
                    int columnCount = dayColumnCounts[day.ToString()];
                    for (int track = 1; track <= columnCount; track++)
                    {
                        string cellKey = $"{period},{day},{track}";

                        var cellBorder = new Border
                        {
                            BorderBrush = Brushes.Gray,
                            BorderThickness = new Thickness(track == 1 ? 1 : 0, 1, 1, 1), // 좌측 경계선 조정
                            Background = Brushes.White,
                            AllowDrop = true,
                            Tag = cellKey,
                            MinHeight = 60
                        };

                        var cellContent = new Grid();
                        cellBorder.Child = cellContent;

                        // 드래그 이벤트 연결
                        cellBorder.Drop += Cell_Drop;
                        cellBorder.DragEnter += Cell_DragEnter;
                        cellBorder.DragLeave += Cell_DragLeave;

                        Grid.SetRow(cellBorder, period);
                        Grid.SetColumn(cellBorder, currentColumn);
                        TimetableGrid.Children.Add(cellBorder);

                        // 관리용 딕셔너리에 저장
                        cellBorders[cellKey] = cellBorder;
                        cellContents[cellKey] = cellContent;

                        currentColumn++;
                    }
                }
            }

            // 기존 배치된 강의들 다시 표시
            RedisplayPlacedLectures();
        }

        // 배치된 강의들 다시 표시
        private void RedisplayPlacedLectures()
        {
            foreach (var timeSlot in timeSlotLectures)
            {
                var parts = timeSlot.Key.Split(',');
                int period = int.Parse(parts[0]);
                int day = int.Parse(parts[1]);

                for (int track = 1; track <= timeSlot.Value.Count; track++)
                {
                    string cellKey = $"{period},{day},{track}";
                    if (cellContents.ContainsKey(cellKey))
                    {
                        var lecture = timeSlot.Value[track - 1];
                        var lectureBlock = CreateCellLectureItem(lecture);
                        cellContents[cellKey].Children.Clear();
                        cellContents[cellKey].Children.Add(lectureBlock);

                        // 위치 정보 업데이트
                        placedLecturePositions[lecture.Id] = cellKey;
                    }
                }
            }
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
                // 이미 배치된 강의는 제외
                if (!IsLectureAlreadyPlaced(lecture))
                {
                    var lectureItem = CreateLectureItem(lecture);
                    LectureListPanel.Children.Add(lectureItem);
                }
            }
        }

        // 강의가 이미 배치되었는지 확인
        private bool IsLectureAlreadyPlaced(TimeTableBlock lecture)
        {
            return placedLecturePositions.ContainsKey(lecture.Id);
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
                // 드래그 시작
                DragDrop.DoDragDrop(lectureItem, new LectureDragData(lecture, lectureItem, false), DragDropEffects.Move);
            }
        }

        // 셀 내 강의 아이템 마우스 다운 이벤트 (시간표에서 드래그 시작)
        private void CellLectureItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border lectureBlock && lectureBlock.Tag is TimeTableBlock lecture)
            {
                // 시간표에서 드래그 시작임을 표시
                DragDrop.DoDragDrop(lectureBlock, new LectureDragData(lecture, lectureBlock, true), DragDropEffects.Move);
            }
        }

        // 셀 드래그 엔터 이벤트
        private void Cell_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is Border cell && e.Data.GetData(typeof(LectureDragData)) is LectureDragData dragData)
            {
                var cellKey = cell.Tag.ToString();
                var parts = cellKey.Split(',');
                int period = int.Parse(parts[0]);
                int day = int.Parse(parts[1]);

                // 드롭 가능한지 검사
                if (CanDropLecture(dragData.Lecture, day, period, dragData.IsFromTimetable))
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

                var cellKey = targetCell.Tag.ToString();
                var parts = cellKey.Split(',');
                int period = int.Parse(parts[0]);
                int day = int.Parse(parts[1]);

                // 드롭 가능한지 다시 한번 검사
                if (!CanDropLecture(dragData.Lecture, day, period, dragData.IsFromTimetable))
                {
                    ShowConstraintViolationMessage(dragData.Lecture, day, period);
                    return;
                }

                // 시간표에서 온 강의라면 원래 위치에서 제거
                if (dragData.IsFromTimetable)
                {
                    RemoveLectureFromCurrentPosition(dragData.Lecture);
                }
                else
                {
                    // 왼쪽 목록에서 온 강의라면 목록에서 제거
                    if (dragData.OriginalItem.Parent is Panel parent)
                    {
                        parent.Children.Remove(dragData.OriginalItem);
                    }
                }

                // 새 위치에 강의 배치
                PlaceLectureInTimeSlot(dragData.Lecture, day, period);

                // 상태 업데이트
                UpdateStatusText();
            }
        }

        // 현재 위치에서 강의 제거
        private void RemoveLectureFromCurrentPosition(TimeTableBlock lecture)
        {
            if (placedLecturePositions.ContainsKey(lecture.Id))
            {
                string currentPosition = placedLecturePositions[lecture.Id];
                var parts = currentPosition.Split(',');
                int period = int.Parse(parts[0]);
                int day = int.Parse(parts[1]);
                int track = int.Parse(parts[2]);

                string timeSlotKey = $"{period},{day}";
                timeSlotLectures[timeSlotKey].RemoveAll(l => l.Id == lecture.Id);
                placedLecturePositions.Remove(lecture.Id);

                // 해당 셀 비우기
                if (cellContents.ContainsKey(currentPosition))
                {
                    cellContents[currentPosition].Children.Clear();
                }

                // 빈 트랙들 정리 (뒤의 강의들을 앞으로 이동)
                RearrangeLecturesInTimeSlot(timeSlotKey);
            }
        }

        // 시간슬롯 내 강의들 재정렬
        private void RearrangeLecturesInTimeSlot(string timeSlotKey)
        {
            var parts = timeSlotKey.Split(',');
            int period = int.Parse(parts[0]);
            int day = int.Parse(parts[1]);

            var lectures = timeSlotLectures[timeSlotKey];

            // 모든 관련 셀 비우기
            for (int track = 1; track <= dayColumnCounts[day.ToString()]; track++)
            {
                string cellKey = $"{period},{day},{track}";
                if (cellContents.ContainsKey(cellKey))
                {
                    cellContents[cellKey].Children.Clear();
                }
            }

            // 강의들을 다시 순서대로 배치
            for (int i = 0; i < lectures.Count; i++)
            {
                int track = i + 1;
                string cellKey = $"{period},{day},{track}";
                if (cellContents.ContainsKey(cellKey))
                {
                    var lectureBlock = CreateCellLectureItem(lectures[i]);
                    cellContents[cellKey].Children.Add(lectureBlock);
                    placedLecturePositions[lectures[i].Id] = cellKey;
                }
            }

            // 열 수 재계산 (해당 요일의 최대 필요 열 수)
            RecalculateColumnCountsForDay(day);
        }

        // 특정 요일의 열 수 재계산
        private void RecalculateColumnCountsForDay(int day)
        {
            int maxTracks = 1; // 최소 1개는 유지
            for (int period = 1; period <= 9; period++)
            {
                string timeSlotKey = $"{period},{day}";
                int trackCount = timeSlotLectures[timeSlotKey].Count;
                if (trackCount > maxTracks)
                {
                    maxTracks = trackCount;
                }
            }

            // 열 수가 변경되었다면 그리드 재구성
            if (dayColumnCounts[day.ToString()] != maxTracks)
            {
                dayColumnCounts[day.ToString()] = maxTracks;
                RebuildTimetableGrid();
            }
        }

        // 강의 배치
        private void PlaceLectureInTimeSlot(TimeTableBlock lecture, int day, int period)
        {
            string timeSlotKey = $"{period},{day}";

            // 해당 강의 추가
            timeSlotLectures[timeSlotKey].Add(lecture);

            // 트랙 번호 결정
            int track = timeSlotLectures[timeSlotKey].Count;

            // 해당 요일의 최대 열 수 업데이트
            if (track > dayColumnCounts[day.ToString()])
            {
                dayColumnCounts[day.ToString()] = track;
                // 그리드 재구성
                RebuildTimetableGrid();
            }
            else
            {
                // 단순히 해당 셀에만 추가
                string cellKey = $"{period},{day},{track}";
                if (cellContents.ContainsKey(cellKey))
                {
                    var lectureBlock = CreateCellLectureItem(lecture);
                    cellContents[cellKey].Children.Clear();
                    cellContents[cellKey].Children.Add(lectureBlock);
                    placedLecturePositions[lecture.Id] = cellKey;
                }
            }
        }

        // 셀용 강의 아이템 생성 (시간표에 표시되는 버전)
        private Border CreateCellLectureItem(TimeTableBlock lecture)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(GetLectureColor(lecture.Grade)),
                Padding = new Thickness(2),
                Cursor = Cursors.Hand, // 드래그 가능하다는 표시
                Tag = lecture
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

            // 드래그 이벤트 추가 (시간표에서 다른 곳으로 이동 가능)
            border.MouseLeftButtonDown += CellLectureItem_MouseLeftButtonDown;

            // 우클릭 메뉴 추가 (삭제 옵션)
            var contextMenu = new ContextMenu();
            var removeMenuItem = new MenuItem
            {
                Header = "시간표에서 제거",
                Icon = new TextBlock { Text = "✖", Foreground = Brushes.Red }
            };
            removeMenuItem.Click += (s, args) => RemoveLectureFromTimetable(lecture);
            contextMenu.Items.Add(removeMenuItem);

            border.ContextMenu = contextMenu;

            return border;
        }

        // 시간표에서 강의 제거 (완전 삭제)
        private void RemoveLectureFromTimetable(TimeTableBlock lecture)
        {
            // 현재 위치에서 제거
            RemoveLectureFromCurrentPosition(lecture);

            // 왼쪽 목록에 다시 추가
            var lectureItem = CreateLectureItem(lecture);
            LectureListPanel.Children.Add(lectureItem);

            UpdateStatusText();
        }

        // 강의 배치 가능 여부 검사 (개선된 버전)
        private bool CanDropLecture(TimeTableBlock lecture, int day, int period, bool isFromTimetable)
        {
            string timeSlotKey = $"{period},{day}";
            var existingLectures = timeSlotLectures[timeSlotKey];

            // 시간표에서 온 강의라면 자기 자신은 제외하고 검사
            var lecturesForCheck = existingLectures.Where(l => l.Id != lecture.Id).ToList();

            // 1. 교수님 불가능한 시간 확인
            if (IsUnavailableTime(lecture, day, period))
            {
                return false;
            }

            // 2. 학교 지정 시간인 경우 해당 시간에만 배치 가능
            if (lecture.IsFixedTime && !IsFixedTime(lecture, day, period))
            {
                return false;
            }

            // 3. 학년별 최대 2개 제한 확인
            var sameGradeLectures = lecturesForCheck.Where(l => l.Grade == lecture.Grade).ToList();
            if (sameGradeLectures.Count >= 2)
            {
                return false;
            }

            // 4. 동일한 강의실, 교수, 교과목 중복 확인
            foreach (var existingLecture in lecturesForCheck)
            {
                // 강의실 중복 확인
                if (!string.IsNullOrEmpty(lecture.Classroom) &&
                    !string.IsNullOrEmpty(existingLecture.Classroom) &&
                    lecture.Classroom.Equals(existingLecture.Classroom, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // 교수님 중복 확인
                if (!string.IsNullOrEmpty(lecture.ProfessorName) &&
                    !string.IsNullOrEmpty(existingLecture.ProfessorName) &&
                    lecture.ProfessorName.Equals(existingLecture.ProfessorName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // 교과목 중복 확인 (같은 교과목이 동시에 여러 개 열리면 안됨)
                if (!string.IsNullOrEmpty(lecture.ClassName) &&
                    !string.IsNullOrEmpty(existingLecture.ClassName) &&
                    lecture.ClassName.Equals(existingLecture.ClassName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
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

        // 제약조건 위반 메시지 표시
        private void ShowConstraintViolationMessage(TimeTableBlock lecture, int day, int period)
        {
            string message = "다음 제약조건으로 인해 배치할 수 없습니다:\n\n";

            string timeSlotKey = $"{period},{day}";
            var existingLectures = timeSlotLectures[timeSlotKey].Where(l => l.Id != lecture.Id).ToList();

            if (IsUnavailableTime(lecture, day, period))
            {
                message += "• 교수님이 불가능한 시간입니다.\n";
            }

            if (lecture.IsFixedTime && !IsFixedTime(lecture, day, period))
            {
                message += "• 학교 지정 시간이 아닙니다.\n";
            }

            // 학년별 제한 확인
            var sameGradeLectures = existingLectures.Where(l => l.Grade == lecture.Grade).ToList();
            if (sameGradeLectures.Count >= 2)
            {
                message += $"• 같은 시간에 {lecture.Grade} 수업이 이미 2개 배치되어 있습니다.\n";
            }

            // 중복 확인
            foreach (var existingLecture in existingLectures)
            {
                if (!string.IsNullOrEmpty(lecture.Classroom) &&
                    lecture.Classroom.Equals(existingLecture.Classroom, StringComparison.OrdinalIgnoreCase))
                {
                    message += $"• 강의실 중복: {existingLecture.ClassName}({existingLecture.ProfessorName})과 같은 강의실을 사용합니다.\n";
                }

                if (!string.IsNullOrEmpty(lecture.ProfessorName) &&
                    lecture.ProfessorName.Equals(existingLecture.ProfessorName, StringComparison.OrdinalIgnoreCase))
                {
                    message += $"• 교수님 중복: {existingLecture.ClassName}과 같은 교수님입니다.\n";
                }

                if (!string.IsNullOrEmpty(lecture.ClassName) &&
                    lecture.ClassName.Equals(existingLecture.ClassName, StringComparison.OrdinalIgnoreCase))
                {
                    message += $"• 교과목 중복: 같은 교과목이 이미 배치되어 있습니다.\n";
                }
            }

            MessageBox.Show(message, "배치 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // 상태 텍스트 업데이트
        private void UpdateStatusText()
        {
            int totalLectures = availableLectures.Count;
            int placedCount = 0;

            foreach (var timeSlot in timeSlotLectures.Values)
            {
                placedCount += timeSlot.Count;
            }

            int remainingCount = totalLectures - placedCount;

            // 학년별 배치 현황 추가
            var gradeStats = new Dictionary<string, int>();
            foreach (var timeSlot in timeSlotLectures.Values)
            {
                foreach (var lecture in timeSlot)
                {
                    if (gradeStats.ContainsKey(lecture.Grade))
                        gradeStats[lecture.Grade]++;
                    else
                        gradeStats[lecture.Grade] = 1;
                }
            }

            string gradeStatsText = string.Join(" | ", gradeStats.Select(g => $"{g.Key}: {g.Value}개"));

            StatusText.Text = $"전체 강의: {totalLectures}개 | 배치된 강의: {placedCount}개 | 남은 강의: {remainingCount}개";
            if (!string.IsNullOrEmpty(gradeStatsText))
            {
                StatusText.Text += $" | {gradeStatsText}";
            }
        }

        // 과목 추가 버튼 클릭 이벤트
        private void AddLectureButton_Click(object sender, RoutedEventArgs e)
        {
            // InputPage로 이동하거나 새 강의 추가 다이얼로그 표시
            _mainWindow.NavigateToInputPage();
        }
    }

    // 드래그 데이터 클래스 (개선된 버전)
    public class LectureDragData
    {
        public TimeTableBlock Lecture { get; set; }
        public Border OriginalItem { get; set; }
        public bool IsFromTimetable { get; set; } // 시간표에서 온 건지 여부

        public LectureDragData(TimeTableBlock lecture, Border originalItem, bool isFromTimetable)
        {
            Lecture = lecture;
            OriginalItem = originalItem;
            IsFromTimetable = isFromTimetable;
        }
    }

    // 원래 위치 정보 클래스 (더 이상 사용하지 않지만 호환성을 위해 유지)
    public class OriginalPosition
    {
        public Panel Parent { get; set; }
        public Border Item { get; set; }
    }
}