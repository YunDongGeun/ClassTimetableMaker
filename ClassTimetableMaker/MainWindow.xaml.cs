using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Configuration;
using ClassTimetableMaker;
using ClassTimetableMaker.Views;

namespace ClassTimetableMaker
{
    public partial class MainWindow : Window
    {
        private TimeTablePage _timeTablePage;
        private InputPage _inputPage;
        private QueryPage _queryPage;
        private Page1 _page1;            
        
        // 새로운 페이지들
        private ProfessorInputPage _professorInputPage;
        private ClassroomInputPage _classroomInputPage;
        private SubjectInputPage _subjectInputPage;

        private readonly SQLiteDBManager _dbManager;

        public MainWindow()
        {
            InitializeComponent();

            // DB SQLite 설정
            string databasePath = ConfigurationManager.AppSettings["DatabasePath"];
            _dbManager = new SQLiteDBManager(databasePath);

            // 애플리케이션 로드 시 DB 연결 테스트
            TestDatabaseConnection();

            // 페이지 초기화
            _timeTablePage = new TimeTablePage(this);
            _inputPage = new InputPage(this);
            _queryPage = new QueryPage(this);
            _page1 = new Page1(this);

            // 기본 페이지 설정 (입력 페이지)
            MainFrame.Navigate(_page1);
        }

        // 데이터베이스 연결 테스트
        private async void TestDatabaseConnection()
        {
            try
            {
                bool isConnected = await _dbManager.TestConnectionAsync();
                if (!isConnected)
                {
                    MessageBox.Show(
                        "데이터베이스 연결에 실패했습니다.\n설정을 확인하세요.",
                        "연결 오류",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"데이터베이스 연결 중 오류가 발생했습니다: {ex.Message}",
                    "연결 오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        // 교수 입력 페이지로 이동 (새 교수 추가)
        public void NavigateToProfessorInputPage()
        {
            _professorInputPage = new ProfessorInputPage(this);
            MainFrame.Navigate(_professorInputPage);
        }

        // 교수 입력 페이지로 이동 (교수 정보 수정)
        public void NavigateToProfessorInputPage(Professor professorToEdit)
        {
            _professorInputPage = new ProfessorInputPage(this, professorToEdit);
            MainFrame.Navigate(_professorInputPage);
        }

        // 강의실 입력 페이지로 이동
        public void NavigateToClassroomInputPage()
        {
            _classroomInputPage = new ClassroomInputPage(this);
            MainFrame.Navigate(_classroomInputPage);
        }

        // 교과목 입력 페이지로 이동
        public void NavigateToSubjectInputPage()
        {
            _subjectInputPage = new SubjectInputPage(this);
            MainFrame.Navigate(_subjectInputPage);
        }

        // 교과목 입력 페이지로 이동 (교과목 수정)
        public void NavigateToSubjectInputPage(Subject subjectToEdit)
        {
            _subjectInputPage = new SubjectInputPage(this, subjectToEdit);
            MainFrame.Navigate(_subjectInputPage);
        }

        // 기존 메서드들
        public void NavigateToInputPage()
        {
            MainFrame.Navigate(_inputPage);
        }

        public void NavigateToMainPage()
        {
            MainFrame.Navigate(_page1);
        }

        public void NavigateToQueryPage()
        {
            _queryPage = new QueryPage(this);
            MainFrame.Navigate(_queryPage);
        }

        // 입력 페이지로 이동
        public void NavigateToInputPage()
        {
            MainFrame.Navigate(_inputPage);
        }

        public void NavigateToMainPage()
        {
            MainFrame.Navigate(_page1);
        }

        // 조회 페이지로 이동
        public void NavigateToQueryPage()
        {
            // 조회 페이지가 매번 새로 로드되도록 처리
            _queryPage = new QueryPage(this);
            MainFrame.Navigate(_queryPage);
        }
    }
}