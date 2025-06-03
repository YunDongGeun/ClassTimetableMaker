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

        // 동적 시간표 관리를 위한 새로운 데이터 구조
        private Dictionary<string, List<LectureInstance>> timeSlotLectures = new(); // "period,day" -> 강의 인스턴스 리스트
        private Dictionary<string, int> dayColumnCounts = new(); // 각 요일별 열 개수 (최대 2개)
        private List<TimeTableBlock> availableLectures = new();

        // UI 요소들을 동적으로 관리
        private Dictionary<string, Border> cellBorders = new(); // "period,day,track" -> Border
        private Dictionary<string, Grid> cellContents = new(); // "period,day,track" -> Grid

        // 강의 인스턴스 위치 추적
        private Dictionary<string, string> placedLecturePositions = new(); // 강의인스턴스ID -> "period,day,track"

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
                    timeSlotLectures[timeSlotKey] = new List<LectureInstance>();
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
                    var lectureInstance = timeSlot.Value[track - 1];
                    if (!lectureInstance.IsContinuation) // 연강의 시작 부분만 표시
                    {
                        DisplayLectureInstance(lectureInstance, period, day, track);
                    }
                }
            }
        }

        // 강의 인스턴스 표시
        private void DisplayLectureInstance(LectureInstance lectureInstance, int period, int day, int track)
        {
            int duration = lectureInstance.Duration;

            // 연강의 경우 여러 셀에 걸쳐 표시
            for (int i = 0; i < duration; i++)
            {
                int currentPeriod = period + i;
                if (currentPeriod > 9) break; // 9교시 초과하면 중단

                string cellKey = $"{currentPeriod},{day},{track}";
                if (cellContents.ContainsKey(cellKey))
                {
                    var lectureBlock = CreateCellLectureItem(lectureInstance, i == 0, duration);
                    cellContents[cellKey].Children.Clear();
                    cellContents[cellKey].Children.Add(lectureBlock);

                    // 위치 정보 업데이트 (시작 셀만)
                    if (i == 0)
                    {
                        placedLecturePositions[lectureInstance.Id] = cellKey;
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
                ProcessLecturesForDisplay();
                PopulateLectureList();
                PlaceFixedTimeLectures(); // 고정 시간 강의 자동 배치
            }
            catch (Exception ex)
            {
                MessageBox.Show($"강의 목록 로드 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 강의 목록을 표시용으로 가공 (분반 및 차시 분할)
        private void ProcessLecturesForDisplay()
        {
            var processedLectures = new List<TimeTableBlock>();

            foreach (var lecture in availableLectures)
            {
                // 분반 처리
                var sections = CreateSections(lecture);

                foreach (var section in sections)
                {
                    // 차시 분할 처리
                    var instances = CreateLectureInstances(section);
                    processedLectures.AddRange(instances);
                }
            }

            availableLectures = processedLectures;
        }

        // 분반 생성
        private List<TimeTableBlock> CreateSections(TimeTableBlock originalLecture)
        {
            var sections = new List<TimeTableBlock>();

            int sectionCount = originalLecture.SectionCount ?? 1; // 분반 개수 (기본값 1)

            if (sectionCount <= 1)
            {
                sections.Add(originalLecture);
            }
            else
            {
                for (int i = 0; i < sectionCount; i++)
                {
                    char sectionLetter = (char)('A' + i);
                    var section = CloneLecture(originalLecture);
                    section.ClassName = $"{originalLecture.ClassName}{sectionLetter}";
                    section.Id = originalLecture.Id * 1000 + i; // 고유 ID 생성
                    sections.Add(section);
                }
            }

            return sections;
        }

        // 강의 인스턴스 생성 (차시 분할)
        private List<TimeTableBlock> CreateLectureInstances(TimeTableBlock lecture)
        {
            var instances = new List<TimeTableBlock>();

            // LectureHours_1, LectureHours_2에서 시간 정보 가져오기
            var lectureHours = new List<int>();
            if (lecture.LectureHours_1.HasValue && lecture.LectureHours_1.Value > 0)
                lectureHours.Add(lecture.LectureHours_1.Value);
            if (lecture.LectureHours_2.HasValue && lecture.LectureHours_2.Value > 0)
                lectureHours.Add(lecture.LectureHours_2.Value);

            if (lectureHours.Count == 0)
            {
                // 시간 정보가 없으면 기본 1시간으로 처리
                lectureHours.Add(1);
            }

            for (int i = 0; i < lectureHours.Count; i++)
            {
                var instance = CloneLecture(lecture);
                instance.Id = lecture.Id * 100 + i; // 고유 ID 생성
                instance.InstanceIndex = i;
                instance.Duration = lectureHours[i];

                if (lectureHours.Count > 1)
                {
                    instance.DisplayName = $"{lecture.ClassName} ({i + 1}차시)";
                }
                else
                {
                    instance.DisplayName = lecture.ClassName;
                }

                instances.Add(instance);
            }

            return instances;
        }

        // 강의 복제
        private TimeTableBlock CloneLecture(TimeTableBlock original)
        {
            return new TimeTableBlock
            {
                Id = original.Id,
                ClassName = original.ClassName,
                ProfessorName = original.ProfessorName,
                Grade = original.Grade,
                Classroom = original.Classroom,
                IsFixedTime = original.IsFixedTime,
                FixedTimeSlot = original.FixedTimeSlot,
                UnavailableSlot1 = original.UnavailableSlot1,
                UnavailableSlot2 = original.UnavailableSlot2,
                AdditionalUnavailableSlot1 = original.AdditionalUnavailableSlot1,
                AdditionalUnavailableSlot2 = original.AdditionalUnavailableSlot2,
                LectureHours_1 = original.LectureHours_1,
                LectureHours_2 = original.LectureHours_2,
                SectionCount = original.SectionCount,
                DisplayName = original.ClassName,
                Duration = 1,
                InstanceIndex = 0
            };
        }

        // 고정 시간 강의 자동 배치
        private void PlaceFixedTimeLectures()
        {
            var fixedLectures = availableLectures.Where(l => l.IsFixedTime && !string.IsNullOrEmpty(l.FixedTimeSlot)).ToList();

            foreach (var lecture in fixedLectures)
            {
                var timeSlot = ParseFixedTimeSlot(lecture.FixedTimeSlot);
                if (timeSlot.HasValue)
                {
                    var (day, startPeriod, endPeriod) = timeSlot.Value;
                    int duration = endPeriod - startPeriod + 1;

                    // 기존 Duration 덮어쓰기
                    lecture.Duration = duration;

                    var lectureInstance = new LectureInstance
                    {
                        Id = $"fixed_{lecture.Id}",
                        Lecture = lecture,
                        Duration = duration,
                        IsContinuation = false
                    };

                    if (CanPlaceFixedLecture(lectureInstance, day, startPeriod))
                    {
                        PlaceLectureInstance(lectureInstance, day, startPeriod);

                        // 사용 가능한 목록에서 제거
                        availableLectures.Remove(lecture);
                    }
                }
            }

            PopulateLectureList(); // 목록 업데이트
        }

        // 고정 시간 파싱 (예: "수요일 6~7교시" -> (3, 6, 7))
        private (int day, int startPeriod, int endPeriod)? ParseFixedTimeSlot(string fixedTimeSlot)
        {
            try
            {
                var dayNames = new Dictionary<string, int>
                {
                    {"월요일", 1}, {"화요일", 2}, {"수요일", 3}, {"목요일", 4}, {"금요일", 5}
                };

                // "수요일 6~7교시" 형태 파싱
                var parts = fixedTimeSlot.Split(' ');
                if (parts.Length >= 2)
                {
                    var dayName = parts[0];
                    var timeRange = parts[1].Replace("교시", "");

                    if (dayNames.ContainsKey(dayName))
                    {
                        int day = dayNames[dayName];

                        if (timeRange.Contains("~"))
                        {
                            var periodParts = timeRange.Split('~');
                            if (int.TryParse(periodParts[0], out int start) && int.TryParse(periodParts[1], out int end))
                            {
                                return (day, start, end);
                            }
                        }
                        else if (int.TryParse(timeRange, out int singlePeriod))
                        {
                            return (day, singlePeriod, singlePeriod);
                        }
                    }
                }
            }
            catch
            {
                // 파싱 오류 시 무시
            }

            return null;
        }

        // 고정 강의 배치 가능 여부 확인
        private bool CanPlaceFixedLecture(LectureInstance lectureInstance, int day, int startPeriod)
        {
            // 연강 범위 내의 모든 시간 슬롯 확인
            for (int i = 0; i < lectureInstance.Duration; i++)
            {
                int period = startPeriod + i;
                if (period > 9) return false; // 9교시 초과

                string timeSlotKey = $"{period},{day}";
                var existingLectures = timeSlotLectures[timeSlotKey];

                // 기본 제약조건 확인
                if (!CanDropLecture(lectureInstance.Lecture, day, period, false, existingLectures))
                {
                    return false;
                }
            }

            return true;
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
            string lectureId = $"lecture_{lecture.Id}";
            return placedLecturePositions.ContainsKey(lectureId);
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
                Text = lecture.DisplayName ?? lecture.ClassName,
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
                Text = $"{lecture.Grade} ({lecture.Duration}시간)",
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
                var lectureInstance = new LectureInstance
                {
                    Id = $"lecture_{lecture.Id}",
                    Lecture = lecture,
                    Duration = lecture.Duration,
                    IsContinuation = false
                };

                // 드래그 시작
                DragDrop.DoDragDrop(lectureItem, new LectureDragData(lectureInstance, lectureItem, false), DragDropEffects.Move);
            }
        }

        // 셀 내 강의 아이템 마우스 다운 이벤트 (시간표에서 드래그 시작)
        private void CellLectureItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border lectureBlock && lectureBlock.Tag is LectureInstance lectureInstance)
            {
                // 시간표에서 드래그 시작임을 표시
                DragDrop.DoDragDrop(lectureBlock, new LectureDragData(lectureInstance, lectureBlock, true), DragDropEffects.Move);
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
                if (CanDropLectureInstance(dragData.LectureInstance, day, period, dragData.IsFromTimetable))
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
                if (!CanDropLectureInstance(dragData.LectureInstance, day, period, dragData.IsFromTimetable))
                {
                    ShowConstraintViolationMessage(dragData.LectureInstance.Lecture, day, period);
                    return;
                }

                // 시간표에서 온 강의라면 원래 위치에서 제거
                if (dragData.IsFromTimetable)
                {
                    RemoveLectureInstanceFromCurrentPosition(dragData.LectureInstance);
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
                PlaceLectureInstance(dragData.LectureInstance, day, period);

                // 상태 업데이트
                UpdateStatusText();
            }
        }

        // 강의 인스턴스 배치 가능 여부 확인
        private bool CanDropLectureInstance(LectureInstance lectureInstance, int day, int period, bool isFromTimetable)
        {
            // 연강 범위 내의 모든 시간 슬롯 확인
            for (int i = 0; i < lectureInstance.Duration; i++)
            {
                int currentPeriod = period + i;
                if (currentPeriod > 9) return false; // 9교시 초과

                string timeSlotKey = $"{currentPeriod},{day}";
                var existingLectures = timeSlotLectures[timeSlotKey];

                // 시간표에서 온 강의라면 자기 자신은 제외하고 검사
                var lecturesForCheck = existingLectures;
                if (isFromTimetable)
                {
                    lecturesForCheck = existingLectures.Where(l => l.Id != lectureInstance.Id).ToList();
                }

                // 기본 제약조건 확인
                if (!CanDropLecture(lectureInstance.Lecture, day, currentPeriod, isFromTimetable, lecturesForCheck))
                {
                    return false;
                }
            }

            // 분반 동시간 배치 금지 확인
            if (HasConflictingSections(lectureInstance, day, period, isFromTimetable))
            {
                return false;
            }

            // 같은 교과목의 다른 차시가 같은 요일 연속 시간에 배치되는지 확인
            if (HasConflictingInstances(lectureInstance, day, period, isFromTimetable))
            {
                return false;
            }

            return true;
        }

        // 분반 충돌 확인
        private bool HasConflictingSections(LectureInstance lectureInstance, int day, int period, bool isFromTimetable)
        {
            var baseName = GetBaseClassName(lectureInstance.Lecture.ClassName);

            for (int i = 0; i < lectureInstance.Duration; i++)
            {
                int currentPeriod = period + i;
                if (currentPeriod > 9) continue;

                string timeSlotKey = $"{currentPeriod},{day}";
                var existingLectures = timeSlotLectures[timeSlotKey];

                foreach (var existing in existingLectures)
                {
                    if (isFromTimetable && existing.Id == lectureInstance.Id) continue;

                    var existingBaseName = GetBaseClassName(existing.Lecture.ClassName);

                    // 같은 기본 교과목명이고 같은 교수, 같은 강의실이면 분반으로 간주
                    if (existingBaseName == baseName &&
                        existing.Lecture.ProfessorName == lectureInstance.Lecture.ProfessorName &&
                        existing.Lecture.Classroom == lectureInstance.Lecture.Classroom)
                    {
                        return true; // 분반 충돌
                    }
                }
            }

            return false;
        }

        // 같은 교과목 차시 충돌 확인 (같은 요일 연속 배치 금지)
        private bool HasConflictingInstances(LectureInstance lectureInstance, int day, int period, bool isFromTimetable)
        {
            var originalId = GetOriginalLectureId(lectureInstance.Lecture.Id);

            // 같은 요일의 모든 시간 확인
            for (int checkPeriod = 1; checkPeriod <= 9; checkPeriod++)
            {
                // 현재 배치하려는 시간과 인접한 시간인지 확인
                bool isAdjacent = false;
                for (int i = 0; i < lectureInstance.Duration; i++)
                {
                    int targetPeriod = period + i;
                    if (Math.Abs(checkPeriod - targetPeriod) <= 1 && checkPeriod != targetPeriod)
                    {
                        isAdjacent = true;
                        break;
                    }
                }

                if (!isAdjacent) continue;

                string timeSlotKey = $"{checkPeriod},{day}";
                var existingLectures = timeSlotLectures[timeSlotKey];

                foreach (var existing in existingLectures)
                {
                    if (isFromTimetable && existing.Id == lectureInstance.Id) continue;

                    var existingOriginalId = GetOriginalLectureId(existing.Lecture.Id);

                    // 같은 원본 강의의 다른 차시인지 확인
                    if (existingOriginalId == originalId && existing.Lecture.InstanceIndex != lectureInstance.Lecture.InstanceIndex)
                    {
                        return true; // 차시 충돌
                    }
                }
            }

            return false;
        }

        // 기본 교과목명 추출 (분반 표시 제거)
        private string GetBaseClassName(string className)
        {
            if (string.IsNullOrEmpty(className)) return "";

            // 마지막 문자가 A, B, C... 형태의 분반 표시인지 확인
            char lastChar = className[className.Length - 1];
            if (lastChar >= 'A' && lastChar <= 'Z' && className.Length > 1)
            {
                return className.Substring(0, className.Length - 1);
            }

            return className;
        }

        // 원본 강의 ID 추출
        private int GetOriginalLectureId(int lectureId)
        {
            // ID 형태: 원본ID * 1000 + 분반번호, 원본ID * 100 + 차시번호
            if (lectureId >= 1000)
            {
                return lectureId / 1000; // 분반에서 원본 ID 추출
            }
            else if (lectureId >= 100)
            {
                return lectureId / 100; // 차시에서 원본 ID 추출
            }

            return lectureId; // 원본 ID 그대로
        }

        // 기본 제약조건 확인
        private bool CanDropLecture(TimeTableBlock lecture, int day, int period, bool isFromTimetable, List<LectureInstance> existingLectures)
        {
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
            var sameGradeLectures = existingLectures.Where(l => l.Lecture.Grade == lecture.Grade).ToList();
            if (sameGradeLectures.Count >= 2)
            {
                return false;
            }

            // 4. 동일한 강의실, 교수, 교과목 중복 확인
            foreach (var existingLecture in existingLectures)
            {
                // 강의실 중복 확인
                if (!string.IsNullOrEmpty(lecture.Classroom) &&
                    !string.IsNullOrEmpty(existingLecture.Lecture.Classroom) &&
                    lecture.Classroom.Equals(existingLecture.Lecture.Classroom, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // 교수님 중복 확인
                if (!string.IsNullOrEmpty(lecture.ProfessorName) &&
                    !string.IsNullOrEmpty(existingLecture.Lecture.ProfessorName) &&
                    lecture.ProfessorName.Equals(existingLecture.Lecture.ProfessorName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // 교과목 중복 확인 (정확히 동일한 교과목명)
                if (!string.IsNullOrEmpty(lecture.ClassName) &&
                    !string.IsNullOrEmpty(existingLecture.Lecture.ClassName) &&
                    lecture.ClassName.Equals(existingLecture.Lecture.ClassName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        // 강의 인스턴스 배치
        private void PlaceLectureInstance(LectureInstance lectureInstance, int day, int period)
        {
            // 연강의 모든 시간 슬롯에 배치
            for (int i = 0; i < lectureInstance.Duration; i++)
            {
                int currentPeriod = period + i;
                if (currentPeriod > 9) break; // 9교시 초과하면 중단

                string timeSlotKey = $"{currentPeriod},{day}";

                var instanceToAdd = new LectureInstance
                {
                    Id = lectureInstance.Id,
                    Lecture = lectureInstance.Lecture,
                    Duration = lectureInstance.Duration,
                    IsContinuation = i > 0 // 첫 번째가 아니면 연속 표시
                };

                timeSlotLectures[timeSlotKey].Add(instanceToAdd);
            }

            // 해당 요일의 최대 열 수 업데이트
            string firstTimeSlotKey = $"{period},{day}";
            int currentTrackCount = timeSlotLectures[firstTimeSlotKey].Count(l => !l.IsContinuation);

            if (currentTrackCount > dayColumnCounts[day.ToString()])
            {
                dayColumnCounts[day.ToString()] = currentTrackCount;
                // 그리드 재구성
                RebuildTimetableGrid();
            }
            else
            {
                // 트랙 번호 결정
                int track = GetAvailableTrack(day, period);

                // 강의 표시
                DisplayLectureInstance(lectureInstance, period, day, track);

                // 위치 정보 업데이트
                string cellKey = $"{period},{day},{track}";
                placedLecturePositions[lectureInstance.Id] = cellKey;
            }
        }

        // 사용 가능한 트랙 번호 찾기
        private int GetAvailableTrack(int day, int period)
        {
            for (int track = 1; track <= dayColumnCounts[day.ToString()]; track++)
            {
                string cellKey = $"{period},{day},{track}";
                if (cellContents.ContainsKey(cellKey) && cellContents[cellKey].Children.Count == 0)
                {
                    return track;
                }
            }

            // 모든 트랙이 차있으면 새 트랙 추가
            return dayColumnCounts[day.ToString()];
        }

        // 현재 위치에서 강의 인스턴스 제거
        private void RemoveLectureInstanceFromCurrentPosition(LectureInstance lectureInstance)
        {
            if (placedLecturePositions.ContainsKey(lectureInstance.Id))
            {
                string currentPosition = placedLecturePositions[lectureInstance.Id];
                var parts = currentPosition.Split(',');
                int startPeriod = int.Parse(parts[0]);
                int day = int.Parse(parts[1]);
                int track = int.Parse(parts[2]);

                // 연강의 모든 시간 슬롯에서 제거
                for (int i = 0; i < lectureInstance.Duration; i++)
                {
                    int period = startPeriod + i;
                    if (period > 9) break;

                    string timeSlotKey = $"{period},{day}";
                    timeSlotLectures[timeSlotKey].RemoveAll(l => l.Id == lectureInstance.Id);

                    // 해당 셀 비우기
                    string cellKey = $"{period},{day},{track}";
                    if (cellContents.ContainsKey(cellKey))
                    {
                        cellContents[cellKey].Children.Clear();
                    }
                }

                placedLecturePositions.Remove(lectureInstance.Id);

                // 빈 트랙들 정리
                RearrangeLecturesInTimeSlots(day);
            }
        }

        // 요일별 강의들 재정렬
        private void RearrangeLecturesInTimeSlots(int day)
        {
            // 해당 요일의 모든 시간 슬롯에서 강의들을 수집
            var allLectures = new Dictionary<string, List<LectureInstance>>();

            for (int period = 1; period <= 9; period++)
            {
                string timeSlotKey = $"{period},{day}";
                allLectures[timeSlotKey] = timeSlotLectures[timeSlotKey].Where(l => !l.IsContinuation).ToList();
                timeSlotLectures[timeSlotKey].Clear();

                // 해당 시간의 모든 셀 비우기
                for (int track = 1; track <= dayColumnCounts[day.ToString()]; track++)
                {
                    string cellKey = $"{period},{day},{track}";
                    if (cellContents.ContainsKey(cellKey))
                    {
                        cellContents[cellKey].Children.Clear();
                    }
                }
            }

            // 강의들을 다시 순서대로 배치
            foreach (var timeSlot in allLectures)
            {
                var parts = timeSlot.Key.Split(',');
                int period = int.Parse(parts[0]);

                for (int i = 0; i < timeSlot.Value.Count; i++)
                {
                    var lectureInstance = timeSlot.Value[i];
                    int track = i + 1;

                    // 연강의 모든 시간 슬롯에 다시 배치
                    for (int j = 0; j < lectureInstance.Duration; j++)
                    {
                        int currentPeriod = period + j;
                        if (currentPeriod > 9) break;

                        string currentTimeSlotKey = $"{currentPeriod},{day}";
                        var instanceToAdd = new LectureInstance
                        {
                            Id = lectureInstance.Id,
                            Lecture = lectureInstance.Lecture,
                            Duration = lectureInstance.Duration,
                            IsContinuation = j > 0
                        };

                        timeSlotLectures[currentTimeSlotKey].Add(instanceToAdd);

                        // 첫 번째 시간에만 표시
                        if (j == 0)
                        {
                            DisplayLectureInstance(lectureInstance, currentPeriod, day, track);
                            string cellKey = $"{currentPeriod},{day},{track}";
                            placedLecturePositions[lectureInstance.Id] = cellKey;
                        }
                    }
                }
            }

            // 열 수 재계산
            RecalculateColumnCountsForDay(day);
        }

        // 특정 요일의 열 수 재계산
        private void RecalculateColumnCountsForDay(int day)
        {
            int maxTracks = 1; // 최소 1개는 유지
            for (int period = 1; period <= 9; period++)
            {
                string timeSlotKey = $"{period},{day}";
                int trackCount = timeSlotLectures[timeSlotKey].Count(l => !l.IsContinuation);
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

        // 셀용 강의 아이템 생성 (연강 지원)
        private Border CreateCellLectureItem(LectureInstance lectureInstance, bool isFirstCell, int totalDuration)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(GetLectureColor(lectureInstance.Lecture.Grade)),
                Padding = new Thickness(2),
                Cursor = isFirstCell ? Cursors.Hand : Cursors.Arrow, // 첫 번째 셀만 드래그 가능
                Tag = lectureInstance
            };

            // 연강 표시를 위한 시각적 효과
            if (totalDuration > 1)
            {
                if (isFirstCell)
                {
                    // 첫 번째 셀: 상단과 좌우 테두리만
                    border.BorderBrush = Brushes.DarkSlateGray;
                    border.BorderThickness = new Thickness(2, 2, 2, 1);
                }
                else
                {
                    // 연속 셀: 좌우와 하단 테두리만
                    border.BorderBrush = Brushes.DarkSlateGray;
                    border.BorderThickness = new Thickness(2, 0, 2, 2);
                }
            }

            var stackPanel = new StackPanel();

            // 첫 번째 셀에만 텍스트 표시
            if (isFirstCell)
            {
                var classNameText = new TextBlock
                {
                    Text = lectureInstance.Lecture.DisplayName ?? lectureInstance.Lecture.ClassName,
                    FontWeight = FontWeights.Bold,
                    FontSize = 10,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center
                };

                var professorText = new TextBlock
                {
                    Text = lectureInstance.Lecture.ProfessorName,
                    FontSize = 8,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center
                };

                if (totalDuration > 1)
                {
                    var durationText = new TextBlock
                    {
                        Text = $"({totalDuration}시간 연강)",
                        FontSize = 7,
                        Foreground = Brushes.LightGray,
                        TextAlignment = TextAlignment.Center
                    };
                    stackPanel.Children.Add(durationText);
                }

                stackPanel.Children.Add(classNameText);
                stackPanel.Children.Add(professorText);

                // 드래그 이벤트 추가 (첫 번째 셀만)
                border.MouseLeftButtonDown += CellLectureItem_MouseLeftButtonDown;

                // 우클릭 메뉴 추가 (삭제 옵션)
                var contextMenu = new ContextMenu();
                var removeMenuItem = new MenuItem
                {
                    Header = "시간표에서 제거",
                    Icon = new TextBlock { Text = "✖", Foreground = Brushes.Red }
                };
                removeMenuItem.Click += (s, args) => RemoveLectureFromTimetable(lectureInstance);
                contextMenu.Items.Add(removeMenuItem);

                border.ContextMenu = contextMenu;
            }
            else
            {
                // 연속 셀에는 연결선 표시
                var connectionText = new TextBlock
                {
                    Text = "▲",
                    FontSize = 16,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 0.7
                };
                stackPanel.Children.Add(connectionText);
            }

            border.Child = stackPanel;

            return border;
        }

        // 시간표에서 강의 제거 (완전 삭제)
        private void RemoveLectureFromTimetable(LectureInstance lectureInstance)
        {
            // 현재 위치에서 제거
            RemoveLectureInstanceFromCurrentPosition(lectureInstance);

            // 왼쪽 목록에 다시 추가
            var lectureItem = CreateLectureItem(lectureInstance.Lecture);
            LectureListPanel.Children.Add(lectureItem);

            UpdateStatusText();
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

            // 지정된 시간 파싱 (예: "월요일 3" 또는 "수요일 6~7교시")
            var timeSlot = ParseFixedTimeSlot(lecture.FixedTimeSlot);
            if (timeSlot.HasValue)
            {
                var (fixedDay, startPeriod, endPeriod) = timeSlot.Value;
                return fixedDay == day && period >= startPeriod && period <= endPeriod;
            }

            return false;
        }

        // 제약조건 위반 메시지 표시
        private void ShowConstraintViolationMessage(TimeTableBlock lecture, int day, int period)
        {
            string message = "다음 제약조건으로 인해 배치할 수 없습니다:\n\n";

            string timeSlotKey = $"{period},{day}";
            var existingLectures = timeSlotLectures[timeSlotKey].Where(l => l.Lecture.Id != lecture.Id).ToList();

            if (IsUnavailableTime(lecture, day, period))
            {
                message += "• 교수님이 불가능한 시간입니다.\n";
            }

            if (lecture.IsFixedTime && !IsFixedTime(lecture, day, period))
            {
                message += "• 학교 지정 시간이 아닙니다.\n";
            }

            // 학년별 제한 확인
            var sameGradeLectures = existingLectures.Where(l => l.Lecture.Grade == lecture.Grade).ToList();
            if (sameGradeLectures.Count >= 2)
            {
                message += $"• 같은 시간에 {lecture.Grade} 수업이 이미 2개 배치되어 있습니다.\n";
            }

            // 분반 충돌 확인
            var baseName = GetBaseClassName(lecture.ClassName);
            foreach (var existingLecture in existingLectures)
            {
                var existingBaseName = GetBaseClassName(existingLecture.Lecture.ClassName);
                if (existingBaseName == baseName &&
                    existingLecture.Lecture.ProfessorName == lecture.ProfessorName &&
                    existingLecture.Lecture.Classroom == lecture.Classroom)
                {
                    message += $"• 분반 충돌: {existingLecture.Lecture.ClassName}과 같은 교과목의 분반입니다.\n";
                }
            }

            // 기타 중복 확인
            foreach (var existingLecture in existingLectures)
            {
                if (!string.IsNullOrEmpty(lecture.Classroom) &&
                    lecture.Classroom.Equals(existingLecture.Lecture.Classroom, StringComparison.OrdinalIgnoreCase))
                {
                    message += $"• 강의실 중복: {existingLecture.Lecture.ClassName}({existingLecture.Lecture.ProfessorName})과 같은 강의실을 사용합니다.\n";
                }

                if (!string.IsNullOrEmpty(lecture.ProfessorName) &&
                    lecture.ProfessorName.Equals(existingLecture.Lecture.ProfessorName, StringComparison.OrdinalIgnoreCase))
                {
                    message += $"• 교수님 중복: {existingLecture.Lecture.ClassName}과 같은 교수님입니다.\n";
                }

                if (!string.IsNullOrEmpty(lecture.ClassName) &&
                    lecture.ClassName.Equals(existingLecture.Lecture.ClassName, StringComparison.OrdinalIgnoreCase))
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
            int placedCount = placedLecturePositions.Count;
            int remainingCount = totalLectures - placedCount;

            // 학년별 배치 현황 추가
            var gradeStats = new Dictionary<string, int>();
            foreach (var position in placedLecturePositions)
            {
                var parts = position.Value.Split(',');
                int period = int.Parse(parts[0]);
                int day = int.Parse(parts[1]);
                int track = int.Parse(parts[2]);

                string timeSlotKey = $"{period},{day}";
                var lectureInstance = timeSlotLectures[timeSlotKey].FirstOrDefault(l => l.Id == position.Key && !l.IsContinuation);

                if (lectureInstance != null)
                {
                    string grade = lectureInstance.Lecture.Grade;
                    if (gradeStats.ContainsKey(grade))
                        gradeStats[grade]++;
                    else
                        gradeStats[grade] = 1;
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

    // 강의 인스턴스 클래스 (연강 지원)
    public class LectureInstance
    {
        public string Id { get; set; }
        public TimeTableBlock Lecture { get; set; }
        public int Duration { get; set; } // 연강 시간 수
        public bool IsContinuation { get; set; } // 연강의 연속 부분인지 여부
    }

    // 드래그 데이터 클래스 (개선된 버전)
    public class LectureDragData
    {
        public LectureInstance LectureInstance { get; set; }
        public Border OriginalItem { get; set; }
        public bool IsFromTimetable { get; set; } // 시간표에서 온 건지 여부

        public LectureDragData(LectureInstance lectureInstance, Border originalItem, bool isFromTimetable)
        {
            LectureInstance = lectureInstance;
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