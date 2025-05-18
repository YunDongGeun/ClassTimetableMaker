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
    public partial class QueryPage : Page
    {
        private readonly DBManager _dbManager;
        private readonly MainWindow _mainWindow;
        private TimeTableBlock _selectedBlock;

        public QueryPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            // App.config에서 DB 설정 불러오기
            string server = ConfigurationManager.AppSettings["DbServer"] ?? "localhost";
            int port = int.Parse(ConfigurationManager.AppSettings["DbPort"] ?? "3306");
            string database = ConfigurationManager.AppSettings["DbName"] ?? "class_time_table_maker";
            string username = ConfigurationManager.AppSettings["DbUser"] ?? "root";
            string password = ConfigurationManager.AppSettings["DbPassword"];

            // DB 매니저 초기화
            _dbManager = new DBManager(server, port, database, username, password);

            // 창이 열릴 때 모든 데이터 로드
            LoadAllTimeTableBlocks();
        }

        // 모든 시간표 블럭 로드
        private async void LoadAllTimeTableBlocks()
        {
            try
            {
                List<TimeTableBlock> blocks = await _dbManager.GetTimeTableBlocksAsync();
                dgTimeTableBlocks.ItemsSource = blocks;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}", "조회 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 교수명으로 검색
        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string professorName = txtSearchProfessor.Text.Trim();

                if (string.IsNullOrEmpty(professorName))
                {
                    // 검색어가 없으면 모든 데이터 로드
                    LoadAllTimeTableBlocks();
                    return;
                }

                List<TimeTableBlock> blocks = await _dbManager.GetTimeTableBlocksByProfessorAsync(professorName);
                dgTimeTableBlocks.ItemsSource = blocks;

                if (blocks.Count == 0)
                {
                    MessageBox.Show($"'{professorName}' 교수님의 시간표 정보가 없습니다.", "검색 결과", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"검색 중 오류가 발생했습니다: {ex.Message}", "검색 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 입력 화면으로 버튼 클릭 이벤트
        private void btnInput_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.NavigateToInputPage();
        }

        // 세부 정보 버튼 클릭 이벤트
        private void btnViewDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var block = button.DataContext as TimeTableBlock;
                if (block != null)
                {
                    ShowDetails(block);
                }
            }
        }

        // DataGrid 선택 변경 이벤트
        private void dgTimeTableBlocks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedBlock = dgTimeTableBlocks.SelectedItem as TimeTableBlock;
        }

        // 세부 정보 닫기 버튼 클릭 이벤트
        private void btnCloseDetails_Click(object sender, RoutedEventArgs e)
        {
            borderDetails.Visibility = Visibility.Collapsed;
            dgTimeTableBlocks.Visibility = Visibility.Visible;
        }

        // 세부 정보 표시
        private void ShowDetails(TimeTableBlock block)
        {
            if (block == null) return;

            // 기본 정보 설정
            txtDetailTitle.Text = $"{block.ClassName} 시간표 세부 정보";
            txtDetailProfessorName.Text = block.ProfessorName;
            txtDetailClassroom.Text = block.Classroom;
            txtDetailGrade.Text = block.Grade;
            txtDetailCourseType.Text = block.CourseType;

            // 강의 시간 정보
            txtDetailCourseHour1.Text = $"{block.CourseHour1}시간";
            txtDetailCourseHour2.Text = $"{block.CourseHour2}시간";

            // 시간 제약 정보 설정

            // 학교 지정 시간
            if (block.IsFixedTime && !string.IsNullOrEmpty(block.FixedTimeSlot))
            {
                spFixedTimeInfo.Visibility = Visibility.Visible;
                txtDetailFixedTimeSlot.Text = block.FixedTimeSlot;

                // 학교 지정 시간인 경우 불가능 시간은 표시하지 않음
                spUnavailableSlotsInfo.Visibility = Visibility.Collapsed;
            }
            else
            {
                spFixedTimeInfo.Visibility = Visibility.Collapsed;

                // 교수님 불가능한 시간
                if (!string.IsNullOrEmpty(block.UnavailableSlot1) || !string.IsNullOrEmpty(block.UnavailableSlot2))
                {
                    spUnavailableSlotsInfo.Visibility = Visibility.Visible;
                    txtDetailUnavailableSlot1.Text = block.UnavailableSlot1 ?? "없음";
                    txtDetailUnavailableSlot2.Text = block.UnavailableSlot2 ?? "없음";
                }
                else
                {
                    spUnavailableSlotsInfo.Visibility = Visibility.Collapsed;
                }
            }

            // 추가 불가능 시간
            if (block.HasAdditionalRestrictions &&
                (!string.IsNullOrEmpty(block.AdditionalUnavailableSlot1) || !string.IsNullOrEmpty(block.AdditionalUnavailableSlot2)))
            {
                spAdditionalSlotsInfo.Visibility = Visibility.Visible;
                txtDetailAdditionalSlot1.Text = block.AdditionalUnavailableSlot1 ?? "없음";
                txtDetailAdditionalSlot2.Text = block.AdditionalUnavailableSlot2 ?? "없음";
            }
            else
            {
                spAdditionalSlotsInfo.Visibility = Visibility.Collapsed;
            }

            // 세부 정보 영역 표시, 데이터 그리드 숨김
            borderDetails.Visibility = Visibility.Visible;
            dgTimeTableBlocks.Visibility = Visibility.Collapsed;
        }
    }
}
