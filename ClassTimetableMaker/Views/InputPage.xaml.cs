﻿using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Configuration;
using ClassTimetableMaker;

namespace ClassTimetableMaker.Views
{
    public partial class InputPage : Page
    {
        private readonly DBManager _dbManager;
        private readonly MainWindow _mainWindow;

        public InputPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            // App.config에서 DB 설정 불러오기
            string server = ConfigurationManager.AppSettings["DbServer"] ?? "localhost";
            int port = int.Parse(ConfigurationManager.AppSettings["DbPort"] ?? "3306");
            string database = ConfigurationManager.AppSettings["DbName"] ?? "class_time_table_maker";
            string username = ConfigurationManager.AppSettings["DbUser"];
            string password = ConfigurationManager.AppSettings["DbPassword"];

            // DB 매니저 초기화
            _dbManager = new DBManager(server, port, database, username, password);
        }

        // 학교 지정 시간 체크박스 이벤트
        private void chkFixedTime_Checked(object sender, RoutedEventArgs e)
        {
            // 학교 지정 시간에만 강의하는 경우 불가능한 시간 입력 비활성화
            spUnavailableSlots.IsEnabled = false;
            spUnavailableSlots.Opacity = 0.5;

            // 학교 지정 시간 선택 UI 표시
            spFixedTimeSlot.Visibility = Visibility.Visible;
        }

        private void chkFixedTime_Unchecked(object sender, RoutedEventArgs e)
        {
            spUnavailableSlots.IsEnabled = true;
            spUnavailableSlots.Opacity = 1.0;

            // 학교 지정 시간 선택 UI 숨김
            spFixedTimeSlot.Visibility = Visibility.Collapsed;
        }

        // 추가 업무 체크박스 이벤트
        private void chkAdditionalRestrictions_Checked(object sender, RoutedEventArgs e)
        {
            spAdditionalSlots.Visibility = Visibility.Visible;
        }

        private void chkAdditionalRestrictions_Unchecked(object sender, RoutedEventArgs e)
        {
            spAdditionalSlots.Visibility = Visibility.Collapsed;
        }

        // 강의 시간 ComboBox 선택 변경 이벤트
        private void cbCourseHour_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                var selectedValue = ((System.Windows.Controls.ComboBoxItem)comboBox.SelectedItem).Content.ToString();

                if (comboBox.Name == "cbCourseHour1")
                {
                    txtHour1Display.Text = selectedValue == "0" ? "0시간" : $"{selectedValue}시간";
                }
                else if (comboBox.Name == "cbCourseHour2")
                {
                    txtHour2Display.Text = selectedValue == "0" ? "0시간" : $"{selectedValue}시간";
                }
            }
        }

        // 숫자만 입력 가능하도록 검증 (TextBox 사용 시에만 필요, ComboBox로 변경 후 사용하지 않음)
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-3]");
            e.Handled = regex.IsMatch(e.Text);
        }

        // 저장 버튼 클릭 이벤트
        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // 입력값 검증
            if (!ValidateInputs())
            {
                return;
            }

            // 입력된 데이터 수집
            var timeTableBlock = new TimeTableBlock
            {
                ProfessorName = txtProfessorName.Text.Trim(),
                ClassName = txtClassName.Text.Trim(),
                Classroom = txtClassroom.Text.Trim(),
                Grade = GetSelectedGrade(),
                IsFixedTime = chkFixedTime.IsChecked.Value,
                FixedTimeSlot = chkFixedTime.IsChecked.Value ? GetUnavailableSlot(cbFixedTimeDay, cbFixedTimeSlot) : null,
                UnavailableSlot1 = GetUnavailableSlot(cbSlot1Day, cbSlot1Time),
                UnavailableSlot2 = GetUnavailableSlot(cbSlot2Day, cbSlot2Time),
                AdditionalUnavailableSlot1 = GetUnavailableSlot(cbAdditionalSlot1Day, cbAdditionalSlot1Time),
                AdditionalUnavailableSlot2 = GetUnavailableSlot(cbAdditionalSlot2Day, cbAdditionalSlot2Time),
                LectureHours_1 = GetSelectedCourseHour(cbCourseHour1),
                LectureHours_2 = GetSelectedCourseHour(cbCourseHour2)
            };

            try
            {
                // DB에 저장
                bool success = await _dbManager.SaveTimeTableBlockAsync(timeTableBlock);

                if (success)
                {
                    MessageBox.Show("시간표 블럭이 저장되었습니다.", "저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 입력 필드 초기화
                    ClearFields();
                }
                else
                {
                    MessageBox.Show("저장 중 오류가 발생했습니다.", "저장 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다: {ex.Message}", "저장 실패", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 취소 버튼 클릭 이벤트
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("입력을 취소하시겠습니까?", "취소 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                ClearFields();
            }
        }

        // 조회 버튼 클릭 이벤트
        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            // 조회 페이지로 이동
            _mainWindow.NavigateToQueryPage();
        }

        // 입력값 검증
        private bool ValidateInputs()
        {
            // 교수 성함 확인
            if (string.IsNullOrWhiteSpace(txtProfessorName.Text))
            {
                MessageBox.Show("교수 성함을 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtProfessorName.Focus();
                return false;
            }

            // 과목명 확인
            if (string.IsNullOrWhiteSpace(txtClassName.Text))
            {
                MessageBox.Show("과목명을 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClassName.Focus();
                return false;
            }

            // 강의실 확인
            if (string.IsNullOrWhiteSpace(txtClassroom.Text))
            {
                MessageBox.Show("강의실을 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClassroom.Focus();
                return false;
            }

            // 학년 선택 확인
            if (!rbGrade1.IsChecked.Value && !rbGrade2.IsChecked.Value &&
                !rbGrade3.IsChecked.Value && !rbGrade4.IsChecked.Value &&
                !rbGraduate.IsChecked.Value)
            {
                MessageBox.Show("학년을 선택해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 과목 구분 선택 확인
            if (!rbMajorRequired.IsChecked.Value && !rbMajorElective.IsChecked.Value &&
                !rbGeneralRequired.IsChecked.Value)
            {
                MessageBox.Show("과목 구분을 선택해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 학교 지정 시간 확인
            if (chkFixedTime.IsChecked.Value &&
                (cbFixedTimeDay.SelectedIndex == -1 || cbFixedTimeSlot.SelectedIndex == -1))
            {
                MessageBox.Show("학교 지정 시간을 선택해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 강의 시간 확인
            if (cbCourseHour1.SelectedIndex == -1 ||
                cbCourseHour2.SelectedIndex == -1)
            {
                MessageBox.Show("강의 시간을 모두 선택해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // 선택된 학년 가져오기
        private string GetSelectedGrade()
        {
            if (rbGrade1.IsChecked.Value) return "1학년";
            if (rbGrade2.IsChecked.Value) return "2학년";
            if (rbGrade3.IsChecked.Value) return "3학년";
            if (rbGrade4.IsChecked.Value) return "4학년";
            if (rbGraduate.IsChecked.Value) return "대학원";
            return "";
        }

        // 선택된 과목 구분 가져오기
        private string GetSelectedCourseType()
        {
            if (rbMajorRequired.IsChecked.Value) return "전공 필수";
            if (rbMajorElective.IsChecked.Value) return "전공 선택";
            if (rbGeneralRequired.IsChecked.Value) return "필수 교과목";
            return "";
        }

        // 선택된 강의 시간 가져오기
        private int GetSelectedCourseHour(System.Windows.Controls.ComboBox comboBox)
        {
            if (comboBox.SelectedItem != null)
            {
                string value = ((System.Windows.Controls.ComboBoxItem)comboBox.SelectedItem).Content.ToString();
                return int.Parse(value);
            }
            return 0;
        }

        // 불가능한 시간 가져오기
        private string GetUnavailableSlot(System.Windows.Controls.ComboBox dayCombo, System.Windows.Controls.ComboBox timeCombo)
        {
            if (dayCombo.SelectedItem != null && timeCombo.SelectedItem != null)
            {
                string day = ((System.Windows.Controls.ComboBoxItem)dayCombo.SelectedItem).Content.ToString();
                string time = ((System.Windows.Controls.ComboBoxItem)timeCombo.SelectedItem).Content.ToString();
                return $"{day} {time}";
            }
            return null;
        }

        // 입력 필드 초기화
        private void ClearFields()
        {
            txtProfessorName.Clear();
            txtClassName.Clear();
            txtClassroom.Clear();

            rbGrade1.IsChecked = false;
            rbGrade2.IsChecked = false;
            rbGrade3.IsChecked = false;
            rbGrade4.IsChecked = false;
            rbGraduate.IsChecked = false;

            rbMajorRequired.IsChecked = false;
            rbMajorElective.IsChecked = false;
            rbGeneralRequired.IsChecked = false;

            chkFixedTime.IsChecked = false;
            chkAdditionalRestrictions.IsChecked = false;

            cbFixedTimeDay.SelectedIndex = -1;
            cbFixedTimeSlot.SelectedIndex = -1;

            cbSlot1Day.SelectedIndex = -1;
            cbSlot1Time.SelectedIndex = -1;
            cbSlot2Day.SelectedIndex = -1;
            cbSlot2Time.SelectedIndex = -1;

            cbAdditionalSlot1Day.SelectedIndex = -1;
            cbAdditionalSlot1Time.SelectedIndex = -1;
            cbAdditionalSlot2Day.SelectedIndex = -1;
            cbAdditionalSlot2Time.SelectedIndex = -1;

            cbCourseHour1.SelectedIndex = -1;
            cbCourseHour2.SelectedIndex = -1;
            txtHour1Display.Text = "";
            txtHour2Display.Text = "";

            spUnavailableSlots.IsEnabled = true;
            spUnavailableSlots.Opacity = 1.0;
            spFixedTimeSlot.Visibility = Visibility.Collapsed;
            spAdditionalSlots.Visibility = Visibility.Collapsed;
        }
    }
}