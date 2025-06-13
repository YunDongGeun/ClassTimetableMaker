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

        private bool _isShowingDBClassrooms = false;
        private List<Classroom> _dbClassrooms = new List<Classroom>();
        private string _currentSearchText = "";
        private string _currentFilter = "전체";

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

        private void UpdateClassroomCount()
        {
            if (_isShowingDBClassrooms)
            {
                var filteredCount = GetFilteredClassrooms(_dbClassrooms.Select(c => ClassroomViewModel.FromClassroom(c)).ToList()).Count;
                txtClassroomCount.Text = $"{filteredCount}개 (전체 {_dbClassrooms.Count}개)";
            }
            else
            {
                var filteredCount = GetFilteredClassrooms(_tempClassrooms.Select(c => new ClassroomViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    CreatedAt = DateTime.Now,
                    IsTemp = true,
                    IsSaved = false
                }).ToList()).Count;
                txtClassroomCount.Text = $"{filteredCount}개 (전체 {_tempClassrooms.Count}개)";
            }

            btnSaveAll.IsEnabled = _tempClassrooms.Count > 0;
        }

        // ========================================
        // 새로운 이벤트 핸들러들
        // ========================================

        // 데이터 소스 변경
        private async void ClassroomDataSource_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            if (rbShowTempClassrooms.IsChecked == true)
            {
                _isShowingDBClassrooms = false;
                ApplyFiltersAndSearch();
                txtClassroomDataSourceStatus.Text = " | 임시 목록";
            }
            else if (rbShowDBClassrooms.IsChecked == true)
            {
                _isShowingDBClassrooms = true;
                await LoadClassroomsFromDatabase();
                txtClassroomDataSourceStatus.Text = " | 데이터베이스";
            }

            UpdateClassroomSelectionStatus();
        }

        // 검색 텍스트 변경
        private void txtSearchClassroom_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentSearchText = txtSearchClassroom.Text?.Trim() ?? "";
            ApplyFiltersAndSearch();
        }

        // 필터 초기화
        private void btnClearClassroomFilter_Click(object sender, RoutedEventArgs e)
        {
            txtSearchClassroom.Clear();
            _currentSearchText = "";
            _currentFilter = "전체";
            ApplyFiltersAndSearch();
        }

        // 필터 버튼 클릭
        private void btnFilterClassroom_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filter)
            {
                _currentFilter = filter;
                ApplyFiltersAndSearch();
            }
        }

        // ========================================
        // 데이터 관리 이벤트 핸들러들
        // ========================================

        // DB에서 강의실 불러오기
        private async void btnLoadClassroomsFromDB_Click(object sender, RoutedEventArgs e)
        {
            rbShowDBClassrooms.IsChecked = true;
            await LoadClassroomsFromDatabase();
        }

        // 데이터베이스에서 강의실 목록 로드
        private async Task LoadClassroomsFromDatabase()
        {
            try
            {
                txtClassroomDataSourceStatus.Text = " | DB 로딩 중...";
                btnLoadClassroomsFromDB.IsEnabled = false;

                _dbClassrooms = await _dbManager.GetClassroomsAsync();

                ApplyFiltersAndSearch();
                txtClassroomDataSourceStatus.Text = " | 데이터베이스";
            }
            catch (Exception ex)
            {
                ShowMessage($"데이터베이스에서 불러오기 실패: {ex.Message}", "오류", MessageBoxImage.Error);
                txtClassroomDataSourceStatus.Text = " | 로딩 실패";
            }
            finally
            {
                btnLoadClassroomsFromDB.IsEnabled = true;
            }
        }

        // 데이터 새로고침
        private async void btnRefreshClassroomData_Click(object sender, RoutedEventArgs e)
        {
            if (_isShowingDBClassrooms)
            {
                await LoadClassroomsFromDatabase();
            }
            else
            {
                ApplyFiltersAndSearch();
            }
            UpdateClassroomSelectionStatus();
        }

        // 목록 비우기
        private void btnClearClassroomList_Click(object sender, RoutedEventArgs e)
        {
            if (_isShowingDBClassrooms)
            {
                ShowMessage("데이터베이스 모드에서는 목록을 비울 수 없습니다.", "알림", MessageBoxImage.Information);
                return;
            }

            if (_tempClassrooms.Count == 0)
            {
                ShowMessage("비울 목록이 없습니다.", "알림", MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"임시 목록의 {_tempClassrooms.Count}개 강의실을 모두 삭제하시겠습니까?\n※ 이 작업은 되돌릴 수 없습니다.",
                "목록 비우기 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _tempClassrooms.Clear();
                ApplyFiltersAndSearch();
                UpdateClassroomSelectionStatus();
                ShowMessage("임시 목록이 비워졌습니다.", "완료", MessageBoxImage.Information);
            }
        }

        // 선택 삭제
        private async void btnDeleteSelectedClassrooms_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = listClassrooms.SelectedItems.Cast<ClassroomViewModel>().ToList();

            if (selectedItems.Count == 0)
            {
                ShowMessage("삭제할 강의실을 선택해주세요.", "알림", MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"선택한 {selectedItems.Count}개의 강의실을 삭제하시겠습니까?\n※ 이 작업은 되돌릴 수 없습니다.",
                "선택 삭제 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes) return;

            try
            {
                btnDeleteSelectedClassrooms.IsEnabled = false;
                int successCount = 0;
                int failCount = 0;
                var failedNames = new List<string>();

                foreach (var item in selectedItems)
                {
                    try
                    {
                        if (_isShowingDBClassrooms)
                        {
                            // DB에서 삭제
                            bool deleted = await _dbManager.DeleteClassroomAsync(item.Id);
                            if (deleted)
                            {
                                successCount++;
                            }
                            else
                            {
                                failCount++;
                                failedNames.Add(item.Name);
                            }
                        }
                        else
                        {
                            // 임시 목록에서 삭제
                            var tempItem = _tempClassrooms.FirstOrDefault(c => c.Id == item.Id);
                            if (tempItem != null)
                            {
                                _tempClassrooms.Remove(tempItem);
                                successCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        failedNames.Add($"{item.Name} ({ex.Message})");
                    }
                }

                // 결과 메시지
                string resultMessage = $"삭제 완료!\n\n✅ 성공: {successCount}개";
                if (failCount > 0)
                {
                    resultMessage += $"\n❌ 실패: {failCount}개";
                    if (failedNames.Count > 0)
                        resultMessage += $"\n\n실패한 항목:\n{string.Join("\n", failedNames)}";
                }

                ShowMessage(resultMessage, "삭제 결과", MessageBoxImage.Information);

                // 데이터 새로고침
                if (_isShowingDBClassrooms)
                {
                    await LoadClassroomsFromDatabase();
                }
                else
                {
                    ApplyFiltersAndSearch();
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"선택 삭제 중 오류: {ex.Message}", "오류", MessageBoxImage.Error);
            }
            finally
            {
                btnDeleteSelectedClassrooms.IsEnabled = true;
                UpdateClassroomSelectionStatus();
            }
        }

        // ========================================
        // 필터링 및 검색 로직
        // ========================================

        // 필터와 검색 적용
        private void ApplyFiltersAndSearch()
        {
            try
            {
                var sourceList = _isShowingDBClassrooms ?
                    _dbClassrooms.Select(c => ClassroomViewModel.FromClassroom(c)).ToList() :
                    _tempClassrooms.Select(c => new ClassroomViewModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        CreatedAt = DateTime.Now,
                        IsTemp = true,
                        IsSaved = false
                    }).ToList();

                var filteredList = GetFilteredClassrooms(sourceList);

                listClassrooms.ItemsSource = filteredList;
                UpdateClassroomCount();
            }
            catch (Exception ex)
            {
                ShowMessage($"필터 적용 중 오류: {ex.Message}", "오류", MessageBoxImage.Error);
            }
        }

        // 필터링된 강의실 목록 가져오기
        private List<ClassroomViewModel> GetFilteredClassrooms(List<ClassroomViewModel> sourceList)
        {
            var filtered = sourceList.AsEnumerable();

            // 카테고리 필터
            if (_currentFilter != "전체")
            {
                filtered = filtered.Where(c => c.Name.Contains(_currentFilter, StringComparison.OrdinalIgnoreCase));
            }

            // 검색 필터
            if (!string.IsNullOrEmpty(_currentSearchText))
            {
                filtered = filtered.Where(c =>
                    c.Name.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase));
            }

            return filtered.OrderBy(c => c.Name).ToList();
        }

        // 선택 상태 업데이트
        private void UpdateClassroomSelectionStatus()
        {
            int selectedCount = listClassrooms.SelectedItems.Count;

            if (selectedCount == 0)
            {
                txtClassroomSelectionStatus.Text = "선택된 강의실이 없습니다";
                btnEditClassroom.IsEnabled = false;
                btnDeleteClassroom.IsEnabled = false;
                btnDeleteSelectedClassrooms.IsEnabled = false;
            }
            else if (selectedCount == 1)
            {
                var selectedItem = listClassrooms.SelectedItem as ClassroomViewModel;
                txtClassroomSelectionStatus.Text = $"선택됨: {selectedItem?.Name}";
                btnEditClassroom.IsEnabled = true;
                btnDeleteClassroom.IsEnabled = true;
                btnDeleteSelectedClassrooms.IsEnabled = true;
            }
            else
            {
                txtClassroomSelectionStatus.Text = $"{selectedCount}개 강의실이 선택됨";
                btnEditClassroom.IsEnabled = false; // 다중 선택 시 수정 불가
                btnDeleteClassroom.IsEnabled = false;
                btnDeleteSelectedClassrooms.IsEnabled = true;
            }
        }

        private void listClassrooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateClassroomSelectionStatus();
        }

        // 전체 저장 버튼 클릭 (기존 메서드를 async Task<bool>로 분리)
        private async void btnSaveAll_Click(object sender, RoutedEventArgs e)
        {
            await SaveAllClassrooms();
        }

        // 전체 저장 로직 (네비게이션에서 재사용하기 위해 분리)
        private async Task<bool> SaveAllClassrooms()
        {
            if (_tempClassrooms.Count == 0)
            {
                ShowMessage("저장할 강의실이 없습니다.", "저장 실패", MessageBoxImage.Warning);
                return false;
            }

            var result = MessageBox.Show(
                $"총 {_tempClassrooms.Count}개의 강의실을 데이터베이스에 저장하시겠습니까?\n\n" +
                "※ 저장 후에는 되돌릴 수 없습니다.",
                "일괄 저장 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes) return false;

            try
            {
                // 프로그레스 표시
                btnSaveAll.IsEnabled = false;
                btnGoToProfessor.IsEnabled = false;
                btnGoToSubject.IsEnabled = false;
                btnSaveAll.Content = "💾 저장 중...";

                int successCount = 0;
                int failCount = 0;
                var failedClassrooms = new List<string>();

                foreach (var classroom in _tempClassrooms.ToList()) // ToList()로 복사본 생성
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
                            _tempClassrooms.Remove(classroom); // 성공한 것만 제거
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

                UpdateClassroomCount();

                // 모든 강의실이 성공적으로 저장되었는지 확인
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
                        return true;
                    }
                }

                return _tempClassrooms.Count == 0;
            }
            catch (Exception ex)
            {
                ShowMessage(
                    $"일괄 저장 중 심각한 오류가 발생했습니다: {ex.Message}",
                    "저장 실패",
                    MessageBoxImage.Error
                );
                return false;
            }
            finally
            {
                btnSaveAll.IsEnabled = _tempClassrooms.Count > 0;
                btnGoToProfessor.IsEnabled = true;
                btnGoToSubject.IsEnabled = true;
                btnSaveAll.Content = "💾 전체 저장";
            }
        }

        // 교수 정보 입력 페이지로 이동
        private async void btnGoToProfessor_Click(object sender, RoutedEventArgs e)
        {
            if (_tempClassrooms.Count > 0)
            {
                var result = MessageBox.Show(
                    $"입력 중인 {_tempClassrooms.Count}개의 강의실 정보가 있습니다.\n\n" +
                    "다음 중 하나를 선택하세요:\n" +
                    "• 예: 먼저 저장하고 교수 정보 입력 페이지로 이동\n" +
                    "• 아니오: 저장하지 않고 이동 (데이터 손실)\n" +
                    "• 취소: 현재 페이지에 머물기",
                    "페이지 이동 옵션",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Cancel) return;

                if (result == MessageBoxResult.Yes)
                {
                    // 먼저 저장 시도
                    bool allSaved = await SaveAllClassrooms();

                    // 저장 후에도 데이터가 남아있다면 저장 실패 (사용자가 취소했거나 오류 발생)
                    if (_tempClassrooms.Count > 0)
                    {
                        return; // 페이지 이동 취소
                    }
                }
            }

            _mainWindow.NavigateToProfessorInputPage();
        }

        // 교과목 정보 입력 페이지로 이동
        private async void btnGoToSubject_Click(object sender, RoutedEventArgs e)
        {
            if (_tempClassrooms.Count > 0)
            {
                var result = MessageBox.Show(
                    $"입력 중인 {_tempClassrooms.Count}개의 강의실 정보가 있습니다.\n\n" +
                    "다음 중 하나를 선택하세요:\n" +
                    "• 예: 먼저 저장하고 교과목 정보 입력 페이지로 이동\n" +
                    "• 아니오: 저장하지 않고 이동 (데이터 손실)\n" +
                    "• 취소: 현재 페이지에 머물기",
                    "페이지 이동 옵션",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Cancel) return;

                if (result == MessageBoxResult.Yes)
                {
                    // 먼저 저장 시도
                    bool allSaved = await SaveAllClassrooms();

                    // 저장 후에도 데이터가 남아있다면 저장 실패 (사용자가 취소했거나 오류 발생)
                    if (_tempClassrooms.Count > 0)
                    {
                        return; // 페이지 이동 취소
                    }
                }
            }

            _mainWindow.NavigateToSubjectInputPage();
        }

    }
}
