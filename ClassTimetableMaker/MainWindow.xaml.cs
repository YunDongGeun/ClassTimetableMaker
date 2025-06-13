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
using ClassTimetableMaker.Views;
using ClassTimetableMaker.Model;

namespace ClassTimetableMaker
{
    public partial class MainWindow : Window
    {
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

        // 메인 시간표 페이지로 이동 (데이터 새로고침 포함)
        public void NavigateToMainPage()
        {
            // 페이지가 이미 생성되어 있다면 데이터 새로고침
            if (_page1 != null)
            {
                _page1.RefreshData();
            }
            else
            {
                // 페이지가 없다면 새로 생성
                _page1 = new Page1(this);
            }

            MainFrame.Navigate(_page1);
        }

        // 교수 입력 페이지로 이동 (새 교수 추가)
        public void NavigateToProfessorInputPage()
        {
            _professorInputPage = new ProfessorInputPage(this);
            MainFrame.Navigate(_professorInputPage);
        }

        // 강의실 입력 페이지로 이동
        public void NavigateToClassroomInputPage()
        {
            _classroomInputPage = new ClassroomInputPage(this);
            MainFrame.Navigate(_classroomInputPage);
        }

        // 교과목 입력 페이지로 이동 (새 교과목 추가)
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

        // 조회 페이지로 이동
        public void NavigateToQueryPage()
        {
            // 조회 페이지가 매번 새로 로드되도록 처리
            _queryPage = new QueryPage(this);
            MainFrame.Navigate(_queryPage);
        }

        public async void RefreshAllPagesData()
        {
            try
            {
                // 메인 페이지 새로고침
                _page1?.RefreshData();

                // 조회 페이지가 있다면 새로고침
                if (_queryPage != null)
                {
                    // QueryPage에 새로고침 메서드가 있다면 호출
                    // _queryPage.RefreshData();
                }

                // 다른 입력 페이지들은 새로 생성될 때 최신 데이터를 로드하므로 별도 처리 불필요
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 새로고침 중 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}