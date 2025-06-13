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
    public partial class SubjectInputPage : Page
    {
        private readonly SQLiteDBManager _dbManager;
        private readonly MainWindow _mainWindow;

        // 임시 교과목 목록 (메모리에 저장)
        private ObservableCollection<SubjectViewModel> _tempSubjects;
        private SubjectViewModel _selectedSubject; // 수정 중인 교과목

        // 교수 관련 데이터
        private List<Professor> _availableProfessors;
        private List<ProfessorCheckBox> _professorCheckBoxes;

        public SubjectInputPage(MainWindow mainWindow, Subject subjectToEdit = null)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            // SQLite DB 매니저 초기화
            string databasePath = ConfigurationManager.AppSettings["DatabasePath"];
            _dbManager = new SQLiteDBManager(databasePath);

            // 임시 교과목 목록 초기화
            _tempSubjects = new ObservableCollection<SubjectViewModel>();
            listSubjects.ItemsSource = _tempSubjects;

            // 교수 체크박스 리스트 초기화
            _professorCheckBoxes = new List<ProfessorCheckBox>();

            // 기본값 설정
            cbLectureHours1.SelectedIndex = 0; // 1시간
            cbLectureHours2.SelectedIndex = 0; // 0시간
            cbSectionCount.SelectedIndex = 0;  // 1개
            cbContinuousHours.SelectedIndex = 0; // 1시간

            // 카운트 업데이트
            UpdateSubjectCount();

            // 교수 목록 로드
            LoadProfessors();

            // 수정 모드인 경우
            if (subjectToEdit != null)
            {
                var viewModel = SubjectViewModel.FromSubject(subjectToEdit);
                LoadSubjectToForm(viewModel);
                _selectedSubject = viewModel;
                btnAddSubject.Content = "✏️ 수정 완료";
            }
        }

        // 교과목 수 업데이트
        private void UpdateSubjectCount()
        {
            txtSubjectCount.Text = $"{_tempSubjects.Count}개";
            btnSaveAll.IsEnabled = _tempSubjects.Count > 0;
        }

        // 교수 목록 로드
        private async void LoadProfessors()
        {
            try
            {
                txtProfessorLoadingStatus.Text = "교수 목록을 불러오는 중...";
                spProfessorList.Children.Clear();
                _professorCheckBoxes.Clear();

                _availableProfessors = await _dbManager.GetProfessorsAsync();

                if (_availableProfessors.Count == 0)
                {
                    txtProfessorLoadingStatus.Text = "등록된 교수가 없습니다.";
                    return;
                }

                txtProfessorLoadingStatus.Text = $"총 {_availableProfessors.Count}명의 교수를 불러왔습니다.";

                // 교수별 체크박스 생성
                foreach (var professor in _availableProfessors)
                {
                    CreateProfessorCheckBox(professor);
                }

                UpdateSelectedProfessorsDisplay();
            }
            catch (Exception ex)
            {
                txtProfessorLoadingStatus.Text = $"교수 목록 로드 실패: {ex.Message}";
                ShowMessage(
                    $"교수 목록을 불러오는 중 오류가 발생했습니다: {ex.Message}",
                    "오류",
                    MessageBoxImage.Error
                );
            }
        }

        // 교수 체크박스 UI 생성
        private void CreateProfessorCheckBox(Professor professor)
        {
            var border = new Border
            {
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8),
                Margin = new Thickness(2)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            // 교수 선택 체크박스
            var checkBox = new CheckBox
            {
                Content = professor.Name,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = professor.Id
            };
            checkBox.Checked += ProfessorCheckBox_Changed;
            checkBox.Unchecked += ProfessorCheckBox_Changed;

            // 주담당 라디오버튼
            var radioButton = new RadioButton
            {
                Content = "주담당",
                GroupName = "PrimaryProfessor",
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                IsEnabled = false,
                Tag = professor.Id
            };
            radioButton.Checked += PrimaryRadioButton_Changed;

            Grid.SetColumn(checkBox, 0);
            Grid.SetColumn(radioButton, 1);

            grid.Children.Add(checkBox);
            grid.Children.Add(radioButton);
            border.Child = grid;

            spProfessorList.Children.Add(border);

            // 관리용 객체 생성
            var professorCheckBox = new ProfessorCheckBox
            {
                CheckBox = checkBox,
                PrimaryRadioButton = radioButton,
                Professor = professor
            };

            _professorCheckBoxes.Add(professorCheckBox);
        }

        // 교수 선택 체크박스 변경 이벤트
        private void ProfessorCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var professorId = (int)checkBox.Tag;
            var professorCheckBox = _professorCheckBoxes.Find(pc => pc.Professor.Id == professorId);

            if (checkBox.IsChecked == true)
            {
                // 체크박스가 선택되면 라디오버튼 활성화
                professorCheckBox.PrimaryRadioButton.IsEnabled = true;

                // 첫 번째로 선택된 교수는 자동으로 주담당으로 설정
                var selectedCount = _professorCheckBoxes.Count(pc => pc.IsSelected);
                if (selectedCount == 1)
                {
                    professorCheckBox.PrimaryRadioButton.IsChecked = true;
                }
            }
            else
            {
                // 체크박스가 해제되면 라디오버튼 비활성화 및 해제
                professorCheckBox.PrimaryRadioButton.IsEnabled = false;
                professorCheckBox.PrimaryRadioButton.IsChecked = false;

                // 주담당이 해제된 경우 다른 선택된 교수 중 첫 번째를 주담당으로 설정
                var selectedProfessors = _professorCheckBoxes.Where(pc => pc.IsSelected).ToList();
                if (selectedProfessors.Count > 0 && !selectedProfessors.Any(pc => pc.IsPrimary))
                {
                    selectedProfessors.First().PrimaryRadioButton.IsChecked = true;
                }
            }

            UpdateSelectedProfessorsDisplay();
        }

        // 주담당 라디오버튼 변경 이벤트
        private void PrimaryRadioButton_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectedProfessorsDisplay();
        }

        // 선택된 교수 표시 업데이트
        private void UpdateSelectedProfessorsDisplay()
        {
            var selectedProfessors = _professorCheckBoxes.Where(pc => pc.IsSelected).ToList();

            if (selectedProfessors.Count == 0)
            {
                txtSelectedProfessors.Text = "선택된 교수가 없습니다";
                return;
            }

            var professorNames = new List<string>();
            foreach (var prof in selectedProfessors.OrderByDescending(p => p.IsPrimary))
            {
                var name = prof.Professor.Name;
                if (prof.IsPrimary)
                    name += "(주담당)";
                professorNames.Add(name);
            }

            txtSelectedProfessors.Text = string.Join(", ", professorNames);
        }

        // 강의시간 변경 이벤트
        private void cbLectureHours_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            UpdateContinuousHours();
        }

        // 연강 설정 체크박스 이벤트
        private void chkIsContinuous_Checked(object sender, RoutedEventArgs e)
        {
            spContinuousSettings.Visibility = Visibility.Visible;
            UpdateContinuousHours();
        }

        private void chkIsContinuous_Unchecked(object sender, RoutedEventArgs e)
        {
            spContinuousSettings.Visibility = Visibility.Collapsed;
        }

        // 연강 시간 업데이트
        private void UpdateContinuousHours()
        {
            if (cbLectureHours1.SelectedItem != null && cbContinuousHours.SelectedItem != null)
            {
                int lectureHours1 = int.Parse(((ComboBoxItem)cbLectureHours1.SelectedItem).Content.ToString());
                cbContinuousHours.SelectedIndex = lectureHours1 - 1; // 1차시 시간과 동일하게 설정
            }
        }

        // 분반 개수 변경 이벤트
        private void cbSectionCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || cbSectionCount.SelectedItem == null) return;

            int sectionCount = int.Parse(((ComboBoxItem)cbSectionCount.SelectedItem).Content.ToString());
            var sections = new List<string>();
            for (int i = 0; i < sectionCount; i++)
            {
                sections.Add(((char)('A' + i)).ToString());
            }

            txtSectionPreview.Text = $"분반: {string.Join(", ", sections)}";
        }

        // 교수 목록 새로고침
        private void btnRefreshProfessors_Click(object sender, RoutedEventArgs e)
        {
            LoadProfessors();
        }

        // 교과목 추가 버튼 클릭
        private void btnAddSubject_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                var subjectName = txtSubjectName.Text.Trim();

                // 임시 목록에서 중복 체크
                if (_tempSubjects.Any(s => s.Name.Equals(subjectName, StringComparison.OrdinalIgnoreCase)))
                {
                    ShowMessage(
                        $"교과목명 '{subjectName}'은(는) 이미 추가되었습니다.\n다른 이름을 입력해주세요.",
                        "중복 교과목",
                        MessageBoxImage.Warning
                    );
                    txtSubjectName.Focus();
                    return;
                }

                var subjectViewModel = new SubjectViewModel
                {
                    Id = DateTime.Now.Millisecond, // 임시 ID
                    Name = subjectName,
                    Grade = ((ComboBoxItem)cbGrade.SelectedItem).Content.ToString(),
                    CourseType = ((ComboBoxItem)cbCourseType.SelectedItem).Content.ToString(),
                    LectureHours1 = int.Parse(((ComboBoxItem)cbLectureHours1.SelectedItem).Content.ToString()),
                    LectureHours2 = int.Parse(((ComboBoxItem)cbLectureHours2.SelectedItem).Content.ToString()),
                    SectionCount = int.Parse(((ComboBoxItem)cbSectionCount.SelectedItem).Content.ToString()),
                    IsContinuous = chkIsContinuous.IsChecked == true,
                    ContinuousHours = cbContinuousHours.SelectedItem != null ?
                        int.Parse(((ComboBoxItem)cbContinuousHours.SelectedItem).Content.ToString()) : 1,
                    SelectedProfessors = GetSelectedProfessors()
                };

                if (_selectedSubject == null)
                {
                    // 새 교과목 추가
                    _tempSubjects.Add(subjectViewModel);

                    ShowMessage(
                        $"교과목 '{subjectViewModel.Name}'이(가) 목록에 추가되었습니다.",
                        "추가 완료",
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    // 기존 교과목 수정
                    var index = _tempSubjects.IndexOf(_selectedSubject);
                    if (index >= 0)
                    {
                        _tempSubjects[index] = subjectViewModel;
                        ShowMessage(
                            $"교과목 '{subjectViewModel.Name}'의 정보가 수정되었습니다.",
                            "수정 완료",
                            MessageBoxImage.Information
                        );
                    }

                    _selectedSubject = null;
                    btnAddSubject.Content = "➕ 추가";
                }

                // 입력 폼 초기화
                ClearInputForm();
                UpdateSubjectCount();
            }
            catch (Exception ex)
            {
                ShowMessage(
                    $"교과목 추가 중 오류가 발생했습니다: {ex.Message}",
                    "오류",
                    MessageBoxImage.Error
                );
            }
        }

        // 선택된 교수 정보 가져오기
        private List<ProfessorSelection> GetSelectedProfessors()
        {
            var selectedProfessors = new List<ProfessorSelection>();

            foreach (var professorCheckBox in _professorCheckBoxes.Where(pc => pc.IsSelected))
            {
                selectedProfessors.Add(new ProfessorSelection
                {
                    ProfessorId = professorCheckBox.Professor.Id,
                    ProfessorName = professorCheckBox.Professor.Name,
                    IsPrimary = professorCheckBox.IsPrimary
                });
            }

            return selectedProfessors;
        }

        // 전체 저장 버튼 클릭
        private async void btnSaveAll_Click(object sender, RoutedEventArgs e)
        {
            if (_tempSubjects.Count == 0)
            {
                ShowMessage("저장할 교과목이 없습니다.", "저장 실패", MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"총 {_tempSubjects.Count}개의 교과목을 데이터베이스에 저장하시겠습니까?\n\n" +
                "※ 저장 후에는 되돌릴 수 없습니다.",
                "일괄 저장 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes) return;

            bool allSaved = await SaveAllSubjects();

            if (allSaved)
            {
                var navResult = MessageBox.Show(
                    "모든 교과목이 성공적으로 저장되었습니다.\n메인 페이지로 이동하시겠습니까?",
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

        // 교과목 목록 선택 변경
        private void listSubjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedSubject = listSubjects.SelectedItem as SubjectViewModel;

            btnEditSubject.IsEnabled = selectedSubject != null;
            btnDeleteSubject.IsEnabled = selectedSubject != null;
        }

        // 교과목 수정 버튼 클릭
        private void btnEditSubject_Click(object sender, RoutedEventArgs e)
        {
            var selectedSubject = listSubjects.SelectedItem as SubjectViewModel;
            if (selectedSubject == null) return;

            _selectedSubject = selectedSubject;

            // 입력 폼에 데이터 로드
            LoadSubjectToForm(selectedSubject);

            // 버튼 텍스트 변경
            btnAddSubject.Content = "✏️ 수정 완료";

            ShowMessage(
                $"'{selectedSubject.Name}' 교과목의 정보를 수정합니다.\n좌측 폼에서 정보를 수정한 후 '수정 완료' 버튼을 클릭하세요.",
                "수정 모드",
                MessageBoxImage.Information
            );
        }

        // 교과목 삭제 버튼 클릭
        private void btnDeleteSubject_Click(object sender, RoutedEventArgs e)
        {
            var selectedSubject = listSubjects.SelectedItem as SubjectViewModel;
            if (selectedSubject == null) return;

            var result = MessageBox.Show(
                $"'{selectedSubject.Name}' 교과목을 목록에서 삭제하시겠습니까?",
                "삭제 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _tempSubjects.Remove(selectedSubject);
                UpdateSubjectCount();

                // 수정 모드였다면 해제
                if (_selectedSubject == selectedSubject)
                {
                    _selectedSubject = null;
                    btnAddSubject.Content = "➕ 추가";
                    ClearInputForm();
                }

                ShowMessage(
                    $"'{selectedSubject.Name}' 교과목이 목록에서 삭제되었습니다.",
                    "삭제 완료",
                    MessageBoxImage.Information
                );
            }
        }

        // 교과목 정보를 입력 폼에 로드
        private void LoadSubjectToForm(SubjectViewModel subject)
        {
            // 기본 정보
            txtSubjectName.Text = subject.Name;
            SetComboBoxValue(cbGrade, subject.Grade);
            SetComboBoxValue(cbCourseType, subject.CourseType);

            // 강의시간
            SetComboBoxValue(cbLectureHours1, subject.LectureHours1.ToString());
            SetComboBoxValue(cbLectureHours2, subject.LectureHours2.ToString());

            // 분반
            SetComboBoxValue(cbSectionCount, subject.SectionCount.ToString());

            // 연강
            chkIsContinuous.IsChecked = subject.IsContinuous;
            if (subject.IsContinuous)
            {
                spContinuousSettings.Visibility = Visibility.Visible;
                SetComboBoxValue(cbContinuousHours, subject.ContinuousHours.ToString());
            }

            // 교수 선택 상태 복원
            RestoreProfessorSelection(subject.SelectedProfessors);
        }

        // 교수 선택 상태 복원
        private void RestoreProfessorSelection(List<ProfessorSelection> selectedProfessors)
        {
            // 모든 체크박스 해제
            foreach (var professorCheckBox in _professorCheckBoxes)
            {
                professorCheckBox.CheckBox.IsChecked = false;
                professorCheckBox.PrimaryRadioButton.IsChecked = false;
                professorCheckBox.PrimaryRadioButton.IsEnabled = false;
            }

            // 선택된 교수들 복원
            foreach (var selectedProf in selectedProfessors)
            {
                var professorCheckBox = _professorCheckBoxes.Find(pc => pc.Professor.Id == selectedProf.ProfessorId);
                if (professorCheckBox != null)
                {
                    professorCheckBox.CheckBox.IsChecked = true;
                    professorCheckBox.PrimaryRadioButton.IsEnabled = true;
                    if (selectedProf.IsPrimary)
                    {
                        professorCheckBox.PrimaryRadioButton.IsChecked = true;
                    }
                }
            }

            UpdateSelectedProfessorsDisplay();
        }

        // ComboBox 값 설정 헬퍼
        private void SetComboBoxValue(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        // 입력값 검증
        private bool ValidateInput()
        {
            // 교과목명 검증
            if (string.IsNullOrWhiteSpace(txtSubjectName.Text))
            {
                ShowMessage("교과목명을 입력해주세요.", "입력 오류", MessageBoxImage.Warning);
                txtSubjectName.Focus();
                return false;
            }

            // 학년 검증
            if (cbGrade.SelectedItem == null)
            {
                ShowMessage("학년을 선택해주세요.", "입력 오류", MessageBoxImage.Warning);
                cbGrade.Focus();
                return false;
            }

            // 과목구분 검증
            if (cbCourseType.SelectedItem == null)
            {
                ShowMessage("과목구분을 선택해주세요.", "입력 오류", MessageBoxImage.Warning);
                cbCourseType.Focus();
                return false;
            }

            // 1차시 시간 검증
            if (cbLectureHours1.SelectedItem == null)
            {
                ShowMessage("1차시 시간을 선택해주세요.", "입력 오류", MessageBoxImage.Warning);
                cbLectureHours1.Focus();
                return false;
            }

            // 교수 선택 검증
            var selectedProfessors = _professorCheckBoxes.Where(pc => pc.IsSelected).ToList();
            if (selectedProfessors.Count == 0)
            {
                ShowMessage("최소 1명의 담당 교수를 선택해주세요.", "입력 오류", MessageBoxImage.Warning);
                return false;
            }

            // 주담당 교수 검증
            if (!selectedProfessors.Any(pc => pc.IsPrimary))
            {
                ShowMessage("주담당 교수를 선택해주세요.", "입력 오류", MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // 초기화 버튼 클릭
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSubject != null)
            {
                // 수정 모드 해제
                _selectedSubject = null;
                btnAddSubject.Content = "➕ 추가";
            }

            ClearInputForm();
        }

        // 취소 버튼 클릭
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_tempSubjects.Count > 0)
            {
                var result = MessageBox.Show(
                    $"입력한 {_tempSubjects.Count}개의 교과목 정보가 저장되지 않고 사라집니다.\n" +
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
            // 기본 정보 초기화
            txtSubjectName.Clear();
            cbGrade.SelectedIndex = -1;
            cbCourseType.SelectedIndex = -1;

            // 강의시간 초기화
            cbLectureHours1.SelectedIndex = 0; // 1시간
            cbLectureHours2.SelectedIndex = 0; // 0시간

            // 분반 초기화
            cbSectionCount.SelectedIndex = 0; // 1개
            txtSectionPreview.Text = "분반: A";

            // 연강 초기화
            chkIsContinuous.IsChecked = false;
            spContinuousSettings.Visibility = Visibility.Collapsed;
            cbContinuousHours.SelectedIndex = 0; // 1시간

            // 교수 선택 초기화
            foreach (var professorCheckBox in _professorCheckBoxes)
            {
                professorCheckBox.CheckBox.IsChecked = false;
                professorCheckBox.PrimaryRadioButton.IsChecked = false;
                professorCheckBox.PrimaryRadioButton.IsEnabled = false;
            }

            UpdateSelectedProfessorsDisplay();
        }

        // 메시지 표시 헬퍼
        private void ShowMessage(string message, string title, MessageBoxImage icon)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        // ========================================
        // 네비게이션 버튼 이벤트 핸들러들
        // ========================================

        // 교수 정보 입력 페이지로 이동
        private async void btnGoToProfessor_Click(object sender, RoutedEventArgs e)
        {
            if (_tempSubjects.Count > 0)
            {
                var result = MessageBox.Show(
                    $"입력 중인 {_tempSubjects.Count}개의 교과목 정보가 있습니다.\n\n" +
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
                    await SaveAllSubjects();

                    // 저장 후에도 데이터가 남아있다면 저장 실패 (사용자가 취소했거나 오류 발생)
                    if (_tempSubjects.Count > 0)
                    {
                        return; // 페이지 이동 취소
                    }
                }
            }

            _mainWindow.NavigateToProfessorInputPage();
        }

        private async Task<bool> SaveAllSubjects()
        {
            if (_tempSubjects.Count == 0)
            {
                ShowMessage("저장할 교과목이 없습니다.", "저장 실패", MessageBoxImage.Warning);
                return false;
            }

            try
            {
                // 프로그레스 표시
                btnSaveAll.IsEnabled = false;
                btnGoToProfessor.IsEnabled = false;
                btnGoToClassroom.IsEnabled = false;
                btnSaveAll.Content = "💾 저장 중...";

                int successCount = 0;
                int failCount = 0;
                var failedSubjects = new List<string>();

                foreach (var subjectViewModel in _tempSubjects.ToList()) // ToList()로 복사본 생성
                {
                    try
                    {
                        // DB에서 중복 체크
                        bool isDuplicate = await _dbManager.IsSubjectNameExistsAsync(subjectViewModel.Name);

                        if (isDuplicate)
                        {
                            failCount++;
                            failedSubjects.Add($"{subjectViewModel.Name} (중복)");
                            continue;
                        }

                        // ViewModel을 Subject로 변환
                        var subject = subjectViewModel.ToSubject();
                        subject.Id = 0; // 임시 ID 제거

                        // 교수 ID 리스트 생성 (주담당이 첫 번째가 되도록)
                        var professorIds = subjectViewModel.SelectedProfessors
                            .OrderByDescending(p => p.IsPrimary)
                            .Select(p => p.ProfessorId)
                            .ToList();

                        bool isSuccess = await _dbManager.SaveSubjectAsync(subject, professorIds);

                        if (isSuccess)
                        {
                            successCount++;
                            _tempSubjects.Remove(subjectViewModel); // 성공한 것만 제거
                        }
                        else
                        {
                            failCount++;
                            failedSubjects.Add($"{subjectViewModel.Name} (저장 오류)");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        failedSubjects.Add($"{subjectViewModel.Name} ({ex.Message})");
                    }
                }

                // 결과 메시지
                string resultMessage = $"저장 완료!\n\n✅ 성공: {successCount}개";

                if (failCount > 0)
                {
                    resultMessage += $"\n❌ 실패: {failCount}개";
                    resultMessage += $"\n\n실패한 교과목:\n{string.Join("\n", failedSubjects)}";
                }

                ShowMessage(resultMessage, "저장 결과", MessageBoxImage.Information);

                UpdateSubjectCount();

                // 모든 교과목이 성공적으로 저장되었는지 확인
                return _tempSubjects.Count == 0;
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
                // UI 상태 복원
                btnSaveAll.IsEnabled = _tempSubjects.Count > 0;
                btnGoToProfessor.IsEnabled = true;
                btnGoToClassroom.IsEnabled = true;
                btnSaveAll.Content = "💾 전체 저장";
            }
        }

        // 강의실 정보 입력 페이지로 이동
        private async void btnGoToClassroom_Click(object sender, RoutedEventArgs e)
        {
            if (_tempSubjects.Count > 0)
            {
                var result = MessageBox.Show(
                    $"입력 중인 {_tempSubjects.Count}개의 교과목 정보가 있습니다.\n\n" +
                    "다음 중 하나를 선택하세요:\n" +
                    "• 예: 먼저 저장하고 강의실 정보 입력 페이지로 이동\n" +
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
                    await SaveAllSubjects();

                    // 저장 후에도 데이터가 남아있다면 저장 실패 (사용자가 취소했거나 오류 발생)
                    if (_tempSubjects.Count > 0)
                    {
                        return; // 페이지 이동 취소
                    }
                }
            }

            _mainWindow.NavigateToClassroomInputPage();
        }

    }
}