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
    public partial class ClassroomInputPage : Page
    {
        private readonly SQLiteDBManager _dbManager;
        private readonly MainWindow _mainWindow;

        // 임시 강의실 목록 (메모리에 저장)
        private ObservableCollection<Classroom> _tempClassrooms;
        private Classroom _selectedClassroom; // 수정 중인 강의실

        public ClassroomInputPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            // SQLite DB 매니저 초기화
            string databasePath = ConfigurationManager.AppSettings["DatabasePath"];
            _dbManager = new SQLiteDBManager(databasePath);

            // 임시 강의실 목록 초기화
            _tempClassrooms = new ObservableCollection<Classroom>();
            listClassrooms.ItemsSource = _tempClassrooms;

            // 카운트 업데이트
            UpdateClassroomCount();
        }

        // 강의실 수 업데이트
        private void UpdateClassroomCount()
        {
            txtClassroomCount.Text = $"{_tempClassrooms.Count}개";
            btnSaveAll.IsEnabled = _tempClassrooms.Count > 0;
        }

        // 강의실 추가 버튼 클릭
        private void btnAddClassroom_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                var classroomName = txtClassroomName.Text.Trim();

                // 임시 목록에서 중복 체크
                if (_tempClassrooms.Any(c => c.Name.Equals(classroomName, StringComparison.OrdinalIgnoreCase)))
                {
                    ShowMessage(
                        $"강의실명 '{classroomName}'은(는) 이미 추가되었습니다.\n다른 이름을 입력해주세요.",
                        "중복 강의실",
                        MessageBoxImage.Warning
                    );
                    txtClassroomName.Focus();
                    return;
                }

                var classroom = new Classroom
                {
                    Id = DateTime.Now.Millisecond, // 임시 ID (중복 방지용)
                    Name = classroomName
                };

                if (_selectedClassroom == null)
                {
                    // 새 강의실 추가
                    _tempClassrooms.Add(classroom);

                    ShowMessage(
                        $"강의실 '{classroom.Name}'이(가) 목록에 추가되었습니다.",
                        "추가 완료",
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    // 기존 강의실 수정
                    var index = _tempClassrooms.IndexOf(_selectedClassroom);
                    if (index >= 0)
                    {
                        _tempClassrooms[index] = classroom;
                        ShowMessage(
                            $"강의실 '{classroom.Name}'의 정보가 수정되었습니다.",
                            "수정 완료",
                            MessageBoxImage.Information
                        );
                    }

                    _selectedClassroom = null;
                    btnAddClassroom.Content = "➕ 추가";
                }

                // 입력 폼 초기화
                ClearInputForm();
                UpdateClassroomCount();
            }
            catch (Exception ex)
            {
                ShowMessage(
                    $"강의실 추가 중 오류가 발생했습니다: {ex.Message}",
                    "오류",
                    MessageBoxImage.Error
                );
            }
        }

        // 전체 저장 버튼 클릭
        private async void btnSaveAll_Click(object sender, RoutedEventArgs e)
        {
            if (_tempClassrooms.Count == 0)
            {
                ShowMessage("저장할 강의실이 없습니다.", "저장 실패", MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"총 {_tempClassrooms.Count}개의 강의실을 데이터베이스에 저장하시겠습니까?\n\n" +
                "※ 저장 후에는 되돌릴 수 없습니다.",
                "일괄 저장 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // 프로그레스 표시
                btnSaveAll.IsEnabled = false;
                btnSaveAll.Content = "💾 저장 중...";

                int successCount = 0;
                int failCount = 0;
                var failedClassrooms = new List<string>();

                foreach (var classroom in _tempClassrooms)
                {
                    try
                    {
                        // DB에서 중복 체크
                        bool isDuplicate = await _dbManager.IsClassroomNameExistsAsync(classroom.Name);

                        if (isDuplicate)
                        {
                            failCount++;
                            failedClassrooms.Add($"{classroom.Name} (중복)");
                            continue;
                        }

                        // 임시 ID 제거 (DB에서 자동 생성)
                        classroom.Id = 0;

                        bool isSuccess = await _dbManager.SaveClassroomAsync(classroom);

                        if (isSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                            failedClassrooms.Add($"{classroom.Name} (저장 오류)");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        failedClassrooms.Add($"{classroom.Name} ({ex.Message})");
                    }
                }

                // 결과 메시지
                string resultMessage = $"저장 완료!\n\n✅ 성공: {successCount}개";

                if (failCount > 0)
                {
                    resultMessage += $"\n❌ 실패: {failCount}개";
                    resultMessage += $"\n\n실패한 강의실:\n{string.Join("\n", failedClassrooms)}";
                }

                ShowMessage(resultMessage, "저장 결과", MessageBoxImage.Information);

                if (successCount > 0)
                {
                    // 성공한 강의실들은 목록에서 제거
                    var classroomsToRemove = _tempClassrooms.Where(c =>
                        !failedClassrooms.Any(f => f.StartsWith(c.Name))).ToList();

                    foreach (var classroom in classroomsToRemove)
                    {
                        _tempClassrooms.Remove(classroom);
                    }

                    UpdateClassroomCount();

                    // 모든 강의실이 성공적으로 저장되었다면 페이지 이동
                    if (_tempClassrooms.Count == 0)
                    {
                        var navResult = MessageBox.Show(
                            "모든 강의실이 성공적으로 저장되었습니다.\n메인 페이지로 이동하시겠습니까?",
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
                btnSaveAll.IsEnabled = _tempClassrooms.Count > 0;
                btnSaveAll.Content = "💾 전체 저장";
            }
        }

        // 강의실 목록 선택 변경
        private void listClassrooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedClassroom = listClassrooms.SelectedItem as Classroom;

            btnEditClassroom.IsEnabled = selectedClassroom != null;
            btnDeleteClassroom.IsEnabled = selectedClassroom != null;
        }

        // 강의실 수정 버튼 클릭
        private void btnEditClassroom_Click(object sender, RoutedEventArgs e)
        {
            var selectedClassroom = listClassrooms.SelectedItem as Classroom;
            if (selectedClassroom == null) return;

            _selectedClassroom = selectedClassroom;

            // 입력 폼에 데이터 로드
            txtClassroomName.Text = selectedClassroom.Name;

            // 버튼 텍스트 변경
            btnAddClassroom.Content = "✏️ 수정 완료";

            ShowMessage(
                $"'{selectedClassroom.Name}' 강의실의 이름을 수정합니다.\n좌측 폼에서 새 이름을 입력한 후 '수정 완료' 버튼을 클릭하세요.",
                "수정 모드",
                MessageBoxImage.Information
            );
        }

        // 강의실 삭제 버튼 클릭
        private void btnDeleteClassroom_Click(object sender, RoutedEventArgs e)
        {
            var selectedClassroom = listClassrooms.SelectedItem as Classroom;
            if (selectedClassroom == null) return;

            var result = MessageBox.Show(
                $"'{selectedClassroom.Name}' 강의실을 목록에서 삭제하시겠습니까?",
                "삭제 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _tempClassrooms.Remove(selectedClassroom);
                UpdateClassroomCount();

                // 수정 모드였다면 해제
                if (_selectedClassroom == selectedClassroom)
                {
                    _selectedClassroom = null;
                    btnAddClassroom.Content = "➕ 추가";
                    ClearInputForm();
                }

                ShowMessage(
                    $"'{selectedClassroom.Name}' 강의실이 목록에서 삭제되었습니다.",
                    "삭제 완료",
                    MessageBoxImage.Information
                );
            }
        }

        // 입력값 검증
        private bool ValidateInput()
        {
            // 강의실명 검증
            if (string.IsNullOrWhiteSpace(txtClassroomName.Text))
            {
                ShowMessage("강의실명을 입력해주세요.", "입력 오류", MessageBoxImage.Warning);
                txtClassroomName.Focus();
                return false;
            }

            return true;
        }

        // 초기화 버튼 클릭
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClassroom != null)
            {
                // 수정 모드 해제
                _selectedClassroom = null;
                btnAddClassroom.Content = "➕ 추가";
            }

            ClearInputForm();
        }

        // 취소 버튼 클릭
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_tempClassrooms.Count > 0)
            {
                var result = MessageBox.Show(
                    $"입력한 {_tempClassrooms.Count}개의 강의실 정보가 저장되지 않고 사라집니다.\n" +
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
            txtClassroomName.Clear();
        }

        // 메시지 표시 헬퍼
        private void ShowMessage(string message, string title, MessageBoxImage icon)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }
    }
}
