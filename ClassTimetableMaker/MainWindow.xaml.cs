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
        private readonly DBManager _dbManager;

        public MainWindow()
        {
            InitializeComponent();

            // DB 매니저 초기화
            string server = ConfigurationManager.AppSettings["DbServer"] ?? "localhost";
            int port = int.Parse(ConfigurationManager.AppSettings["DbPort"]);
            string database = ConfigurationManager.AppSettings["DbName"];
            string username = ConfigurationManager.AppSettings["DbUser"];
            string password = ConfigurationManager.AppSettings["DbPassword"];

            _dbManager = new DBManager(server, port, database, username, password);

            // 애플리케이션 로드 시 DB 연결 테스트
            TestDatabaseConnection();

            // 페이지 초기화
            _timeTablePage = new TimeTablePage(this);
            _inputPage = new InputPage(this);
            _queryPage = new QueryPage(this);

            // 기본 페이지 설정 (입력 페이지)
            MainFrame.Navigate(_timeTablePage);
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

        // 입력 페이지로 이동
        public void NavigateToInputPage()
        {
            MainFrame.Navigate(_inputPage);
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