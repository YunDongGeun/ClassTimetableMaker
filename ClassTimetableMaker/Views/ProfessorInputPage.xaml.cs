using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClassTimetableMaker.Model;

namespace ClassTimetableMaker.Views
{
    public partial class ProfessorInputPage : Page
    {
        private readonly SQLiteDBManager _dbManager;
        private readonly MainWindow _mainWindow;

        // 임시 교수 목록 (메모리에 저장)
        private ObservableCollection<Professor> _tempProfessors;
        private Professor _selectedProfessor; // 수정 중인 교수

        public ProfessorInputPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            // SQLite DB 매니저 초기화
            string databasePath = ConfigurationManager.AppSettings["DatabasePath"];
            _dbManager = new SQLiteDBManager(databasePath);

            // 임시 교수 목록 초기화
            _tempProfessors = new ObservableCollection<Professor>();
            listProfessors.ItemsSource = _tempProfessors;

            // 카운트 업데이트
            UpdateProfessorCount();
        }

        // 교수 수 업데이트
        private void UpdateProfessorCount()
        {
            txtProfessorCount.Text = $"{_tempProfessors.Count}명";
            btnSaveAll.IsEnabled = _tempProfessors.Count > 0;
        }

        // 불가능한 시간 타입 변경 이벤트
        private void rbUnavailableByTime_Checked(object sender, RoutedEventArgs e)
        {
            if (spTimeBasedUnavailable != null && spPeriodBasedUnavailable != null)
            {
                spTimeBasedUnavailable.Visibility = Visibility.Visible;
                spPeriodBasedUnavailable.Visibility = Visibility.Collapsed;
            }
        }

        private void rbUnavailableByPeriod_Checked(object sender, RoutedEventArgs e)
        {
            if (spTimeBasedUnavailable != null && spPeriodBasedUnavailable != null)
            {
                spTimeBasedUnavailable.Visibility = Visibility.Collapsed;
                spPeriodBasedUnavailable.Visibility = Visibility.Visible;
            }
        }

        // 교수 추가 버튼 클릭
        private void btnAddProfessor_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                var professorName = txtProfessorName.Text.Trim();

                // 임시 목록에서 중복 체크
                if (_tempProfessors.Any(p => p.Name.Equals(professorName, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show(
                        $"교수명 '{professorName}'은(는) 이미 추가되었습니다.\n다른 이름을 입력해주세요.",
                        "중복 교수",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    txtProfessorName.Focus();
                    return;
                }

                var professor = new Professor
                {
                    Id = DateTime.Now.Millisecond, // 임시 ID (중복 방지용)
                    Name = professorName,
                    PreferredTimeSlots = GetPreferredTimeSlots(),
                    UnavailableTimeSlots = GetUnavailableTimeSlots()
                };

                if (_selectedProfessor == null)
                {
                    // 새 교수 추가
                    _tempProfessors.Add(professor);

                    ShowMessage(
                        $"교수 '{professor.Name}'님이 목록에 추가되었습니다.",
                        "추가 완료",
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    // 기존 교수 수정
                    var index = _tempProfessors.IndexOf(_selectedProfessor);
                    if (index >= 0)
                    {
                        _tempProfessors[index] = professor;
                        ShowMessage(
                            $"교수 '{professor.Name}'님의 정보가 수정되었습니다.",
                            "수정 완료",
                            MessageBoxImage.Information
                        );
                    }

                    _selectedProfessor = null;
                    btnAddProfessor.Content = "➕ 추가";
                }

                // 입력 폼 초기화
                ClearInputForm();
                UpdateProfessorCount();
            }
            catch (Exception ex)
            {
                ShowMessage(
                    $"교수 추가 중 오류가 발생했습니다: {ex.Message}",
                    "오류",
                    MessageBoxImage.Error
                );
            }
        }

        // 전체 저장 버튼 클릭
        private async void btnSaveAll_Click(object sender, RoutedEventArgs e)
        {
            if (_tempProfessors.Count == 0)
            {
                ShowMessage("저장할 교수가 없습니다.", "저장 실패", MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"총 {_tempProfessors.Count}명의 교수 정보를 데이터베이스에 저장하시겠습니까?\n\n" +
                "※ 저장 후에는 되돌릴 수 없습니다.",
                "일괄 저장 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // 프로그레스 표시 (간단한 방법)
                btnSaveAll.IsEnabled = false;
                btnSaveAll.Content = "💾 저장 중...";

                int successCount = 0;
                int failCount = 0;
                var failedProfessors = new List<string>();

                foreach (var professor in _tempProfessors)
                {
                    try
                    {
                        // DB에서 중복 체크
                        bool isDuplicate = await _dbManager.IsProfessorNameExistsAsync(professor.Name);

                        if (isDuplicate)
                        {
                            failCount++;
                            failedProfessors.Add($"{professor.Name} (중복)");
                            continue;
                        }

                        // 임시 ID 제거 (DB에서 자동 생성)
                        professor.Id = 0;

                        bool isSuccess = await _dbManager.SaveProfessorAsync(professor);

                        if (isSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                            failedProfessors.Add($"{professor.Name} (저장 오류)");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        failedProfessors.Add($"{professor.Name} ({ex.Message})");
                    }
                }

                // 결과 메시지
                string resultMessage = $"저장 완료!\n\n✅ 성공: {successCount}명";

                if (failCount > 0)
                {
                    resultMessage += $"\n❌ 실패: {failCount}명";
                    resultMessage += $"\n\n실패한 교수:\n{string.Join("\n", failedProfessors)}";
                }

                ShowMessage(resultMessage, "저장 결과", MessageBoxImage.Information);

                if (successCount > 0)
                {
                    // 성공한 교수들은 목록에서 제거
                    var professorsToRemove = _tempProfessors.Where(p =>
                        !failedProfessors.Any(f => f.StartsWith(p.Name))).ToList();

                    foreach (var professor in professorsToRemove)
                    {
                        _tempProfessors.Remove(professor);
                    }

                    UpdateProfessorCount();

                    // 모든 교수가 성공적으로 저장되었다면 페이지 이동
                    if (_tempProfessors.Count == 0)
                    {
                        var navResult = MessageBox.Show(
                            "모든 교수가 성공적으로 저장되었습니다.\n메인 페이지로 이동하시겠습니까?",
                            "저장 완료",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );

                        if (navResult == MessageBoxResult.Yes)
                        {
                            _mainWindow.NavigateToMainPage();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage(
                    $"일괄 저장 중 심각한 오류가 발생했습니다: {ex.Message}",
                    "저장 실패",
                    MessageBoxImage.Error
                );
            }
            finally
            {
                btnSaveAll.IsEnabled = _tempProfessors.Count > 0;
                btnSaveAll.Content = "💾 전체 저장";
            }
        }

        // 교수 목록 선택 변경
        private void listProfessors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProfessor = listProfessors.SelectedItem as Professor;

            btnEditProfessor.IsEnabled = selectedProfessor != null;
            btnDeleteProfessor.IsEnabled = selectedProfessor != null;
        }

        // 교수 수정 버튼 클릭
        private void btnEditProfessor_Click(object sender, RoutedEventArgs e)
        {
            var selectedProfessor = listProfessors.SelectedItem as Professor;
            if (selectedProfessor == null) return;

            _selectedProfessor = selectedProfessor;

            // 입력 폼에 데이터 로드
            LoadProfessorToForm(selectedProfessor);

            // 버튼 텍스트 변경
            btnAddProfessor.Content = "✏️ 수정 완료";

            ShowMessage(
                $"'{selectedProfessor.Name}' 교수님의 정보를 수정합니다.\n좌측 폼에서 정보를 수정한 후 '수정 완료' 버튼을 클릭하세요.",
                "수정 모드",
                MessageBoxImage.Information
            );
        }

        // 교수 삭제 버튼 클릭
        private void btnDeleteProfessor_Click(object sender, RoutedEventArgs e)
        {
            var selectedProfessor = listProfessors.SelectedItem as Professor;
            if (selectedProfessor == null) return;

            var result = MessageBox.Show(
                $"'{selectedProfessor.Name}' 교수님을 목록에서 삭제하시겠습니까?",
                "삭제 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _tempProfessors.Remove(selectedProfessor);
                UpdateProfessorCount();

                // 수정 모드였다면 해제
                if (_selectedProfessor == selectedProfessor)
                {
                    _selectedProfessor = null;
                    btnAddProfessor.Content = "➕ 추가";
                    ClearInputForm();
                }

                ShowMessage(
                    $"'{selectedProfessor.Name}' 교수님이 목록에서 삭제되었습니다.",
                    "삭제 완료",
                    MessageBoxImage.Information
                );
            }
        }

        // 교수 정보를 입력 폼에 로드
        private void LoadProfessorToForm(Professor professor)
        {
            // 교수명
            txtProfessorName.Text = professor.Name;

            // 선호 시간 로드
            var preferredDays = professor.GetPreferredDays();
            chkPreferMon.IsChecked = preferredDays.Contains("월요일");
            chkPreferTue.IsChecked = preferredDays.Contains("화요일");
            chkPreferWed.IsChecked = preferredDays.Contains("수요일");
            chkPreferThu.IsChecked = preferredDays.Contains("목요일");
            chkPreferFri.IsChecked = preferredDays.Contains("금요일");

            // 불가능한 시간 로드
            LoadUnavailableSlots(professor.UnavailableTimeSlots);
        }

        // 불가능한 시간 슬롯 파싱 및 UI에 반영
        private void LoadUnavailableSlots(string unavailableSlots)
        {
            if (string.IsNullOrEmpty(unavailableSlots)) return;

            var slots = unavailableSlots.Split(',', StringSplitOptions.RemoveEmptyEntries);
            int slotIndex = 0;

            foreach (var slot in slots.Take(4))
            {
                var trimmedSlot = slot.Trim();

                if (trimmedSlot.Contains("오전") || trimmedSlot.Contains("오후") || trimmedSlot.Contains("전체"))
                {
                    rbUnavailableByTime.IsChecked = true;

                    if (slotIndex < 2)
                    {
                        LoadTimeBasedSlot(trimmedSlot, slotIndex);
                        slotIndex++;
                    }
                }
                else if (trimmedSlot.Contains("교시"))
                {
                    rbUnavailableByPeriod.IsChecked = true;
                    spTimeBasedUnavailable.Visibility = Visibility.Collapsed;
                    spPeriodBasedUnavailable.Visibility = Visibility.Visible;

                    LoadPeriodBasedSlot(trimmedSlot, slotIndex);
                    slotIndex++;
                }
            }
        }

        private void LoadTimeBasedSlot(string slot, int index)
        {
            var parts = slot.Split(' ');
            if (parts.Length >= 2)
            {
                string day = parts[0];
                string time = parts[1];

                if (index == 0)
                {
                    SetComboBoxValue(cbSlot1Day, day);
                    SetComboBoxValue(cbSlot1Time, time);
                }
                else if (index == 1)
                {
                    SetComboBoxValue(cbSlot2Day, day);
                    SetComboBoxValue(cbSlot2Time, time);
                }
            }
        }

        private void LoadPeriodBasedSlot(string slot, int index)
        {
            // 교시 파싱 로직 (기존과 동일)
            var parts = slot.Split(new char[] { '교', '시', '-' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 3)
            {
                string day = parts[0].Replace("1", "").Replace("2", "").Replace("3", "").Replace("4", "")
                                 .Replace("5", "").Replace("6", "").Replace("7", "").Replace("8", "").Replace("9", "");

                var numbers = new List<int>();
                foreach (char c in slot)
                {
                    if (char.IsDigit(c))
                    {
                        numbers.Add(int.Parse(c.ToString()));
                    }
                }

                if (numbers.Count >= 2)
                {
                    int start = numbers[0];
                    int end = numbers[1];

                    if (index == 0)
                    {
                        SetComboBoxValue(cbPeriodSlot1Day, day);
                        SetComboBoxValue(cbPeriodSlot1Start, start.ToString());
                        SetComboBoxValue(cbPeriodSlot1End, end.ToString());
                    }
                    else if (index == 1)
                    {
                        SetComboBoxValue(cbPeriodSlot2Day, day);
                        SetComboBoxValue(cbPeriodSlot2Start, start.ToString());
                        SetComboBoxValue(cbPeriodSlot2End, end.ToString());
                    }
                }
            }
        }

        private void SetComboBoxValue(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString().Contains(value))
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        // 선호 시간 슬롯 생성
        private string GetPreferredTimeSlots()
        {
            var preferredDays = new List<string>();

            if (chkPreferMon.IsChecked == true) preferredDays.Add("월요일");
            if (chkPreferTue.IsChecked == true) preferredDays.Add("화요일");
            if (chkPreferWed.IsChecked == true) preferredDays.Add("수요일");
            if (chkPreferThu.IsChecked == true) preferredDays.Add("목요일");
            if (chkPreferFri.IsChecked == true) preferredDays.Add("금요일");

            return string.Join(",", preferredDays);
        }

        // 불가능한 시간 슬롯 생성
        private string GetUnavailableTimeSlots()
        {
            var unavailableSlots = new List<string>();

            if (rbUnavailableByTime.IsChecked == true)
            {
                var slot1 = GetTimeBasedSlot(cbSlot1Day, cbSlot1Time);
                if (!string.IsNullOrEmpty(slot1)) unavailableSlots.Add(slot1);

                var slot2 = GetTimeBasedSlot(cbSlot2Day, cbSlot2Time);
                if (!string.IsNullOrEmpty(slot2)) unavailableSlots.Add(slot2);
            }
            else if (rbUnavailableByPeriod.IsChecked == true)
            {
                var periodSlot1 = GetPeriodBasedSlot(cbPeriodSlot1Day, cbPeriodSlot1Start, cbPeriodSlot1End);
                if (!string.IsNullOrEmpty(periodSlot1)) unavailableSlots.Add(periodSlot1);

                var periodSlot2 = GetPeriodBasedSlot(cbPeriodSlot2Day, cbPeriodSlot2Start, cbPeriodSlot2End);
                if (!string.IsNullOrEmpty(periodSlot2)) unavailableSlots.Add(periodSlot2);
            }

            return string.Join(",", unavailableSlots);
        }

        private string GetTimeBasedSlot(ComboBox dayCombo, ComboBox timeCombo)
        {
            if (dayCombo.SelectedItem != null && timeCombo.SelectedItem != null)
            {
                string day = ((ComboBoxItem)dayCombo.SelectedItem).Content.ToString();
                string time = ((ComboBoxItem)timeCombo.SelectedItem).Content.ToString();
                return $"{day}{time}";
            }
            return null;
        }

        private string GetPeriodBasedSlot(ComboBox dayCombo, ComboBox startCombo, ComboBox endCombo)
        {
            if (dayCombo.SelectedItem != null && startCombo.SelectedItem != null && endCombo.SelectedItem != null)
            {
                string day = ((ComboBoxItem)dayCombo.SelectedItem).Content.ToString();
                string start = ((ComboBoxItem)startCombo.SelectedItem).Content.ToString();
                string end = ((ComboBoxItem)endCombo.SelectedItem).Content.ToString();

                if (start == end)
                {
                    return $"{day}{start}교시";
                }
                else
                {
                    return $"{day}{start}-{end}교시";
                }
            }
            return null;
        }

        // 입력값 검증
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtProfessorName.Text))
            {
                ShowMessage("교수명을 입력해주세요.", "입력 오류", MessageBoxImage.Warning);
                txtProfessorName.Focus();
                return false;
            }

            if (rbUnavailableByPeriod.IsChecked == true)
            {
                if (!ValidatePeriodRange(cbPeriodSlot1Start, cbPeriodSlot1End) ||
                    !ValidatePeriodRange(cbPeriodSlot2Start, cbPeriodSlot2End))
                {
                    ShowMessage("교시 범위가 올바르지 않습니다. 시작 교시는 끝 교시보다 작거나 같아야 합니다.",
                               "입력 오류", MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        private bool ValidatePeriodRange(ComboBox startCombo, ComboBox endCombo)
        {
            if (startCombo.SelectedItem != null && endCombo.SelectedItem != null)
            {
                int start = int.Parse(((ComboBoxItem)startCombo.SelectedItem).Content.ToString());
                int end = int.Parse(((ComboBoxItem)endCombo.SelectedItem).Content.ToString());
                return start <= end;
            }
            return true;
        }

        // 초기화 버튼 클릭
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProfessor != null)
            {
                // 수정 모드 해제
                _selectedProfessor = null;
                btnAddProfessor.Content = "➕ 추가";
            }

            ClearInputForm();
        }

        // 취소 버튼 클릭
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_tempProfessors.Count > 0)
            {
                var result = MessageBox.Show(
                    $"입력한 {_tempProfessors.Count}명의 교수 정보가 저장되지 않고 사라집니다.\n" +
                    "정말로 취소하시겠습니까?",
                    "취소 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result != MessageBoxResult.Yes) return;
            }

            _mainWindow.NavigateToMainPage();
        }

        // 입력 폼 초기화
        private void ClearInputForm()
        {
            txtProfessorName.Clear();

            // 선호 시간 초기화
            chkPreferMon.IsChecked = false;
            chkPreferTue.IsChecked = false;
            chkPreferWed.IsChecked = false;
            chkPreferThu.IsChecked = false;
            chkPreferFri.IsChecked = false;

            // 불가능한 시간 초기화
            rbUnavailableByTime.IsChecked = true;
            cbSlot1Day.SelectedIndex = -1;
            cbSlot1Time.SelectedIndex = -1;
            cbSlot2Day.SelectedIndex = -1;
            cbSlot2Time.SelectedIndex = -1;

            // 교시별 불가능 시간 초기화
            cbPeriodSlot1Day.SelectedIndex = -1;
            cbPeriodSlot1Start.SelectedIndex = -1;
            cbPeriodSlot1End.SelectedIndex = -1;
            cbPeriodSlot2Day.SelectedIndex = -1;
            cbPeriodSlot2Start.SelectedIndex = -1;
            cbPeriodSlot2End.SelectedIndex = -1;

            // UI 상태 초기화
            spTimeBasedUnavailable.Visibility = Visibility.Visible;
            spPeriodBasedUnavailable.Visibility = Visibility.Collapsed;
        }

        // 메시지 표시 헬퍼
        private void ShowMessage(string message, string title, MessageBoxImage icon)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }
    }
}