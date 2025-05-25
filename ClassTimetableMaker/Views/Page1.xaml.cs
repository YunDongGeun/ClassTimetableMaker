using System;
using System.Collections.Generic;
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
        private Dictionary<string, Slot> originalPositions = new();  // 드래그 시작 위치 저장
        private Stack<ConstraintViolation> constraintStack = new();  // 제약사항 스택

        public Page1()
        {
            InitializeComponent();
        }
        private void SubjectBlock_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border subjectBlock)
            {
                DragDrop.DoDragDrop(subjectBlock, subjectBlock, DragDropEffects.Move);
                originalPositions[subjectBlock.Name] = new Slot
                {
                    Parent = VisualTreeHelper.GetParent(subjectBlock) as Panel,
                    Index = (VisualTreeHelper.GetParent(subjectBlock) as Panel)?.Children.IndexOf(subjectBlock) ?? -1
                };
            }
        }

        private void TimeSlot_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is Border slot)
            {
                if (!IsValidDropTarget(slot))
                {
                    slot.Opacity = 0.4;  // 블러 처리 효과
                    e.Effects = DragDropEffects.None;
                }
                else
                {
                    slot.Opacity = 1.0;
                    e.Effects = DragDropEffects.Move;
                }
            }
        }

        private void TimeSlot_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is Border slot)
            {
                slot.Opacity = 1.0; // 블러 해제
            }
        }

        private void TimeSlot_Drop(object sender, DragEventArgs e)
        {
            if (sender is Border dropTarget && e.Data.GetData(typeof(Border)) is Border subjectBlock)
            {
                if (!IsValidDropTarget(dropTarget))
                {
                    MessageBox.Show("이 위치에는 수업을 배치할 수 없습니다. 제약조건을 확인하세요.", "제약사항 위반", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // 원위치 복구
                    if (originalPositions.TryGetValue(subjectBlock.Name, out Slot origin))
                    {
                        origin.Parent?.Children.Insert(origin.Index, subjectBlock);
                    }
                    return;
                }

                var parent = VisualTreeHelper.GetParent(subjectBlock) as Panel;
                parent?.Children.Remove(subjectBlock);
                (dropTarget.Child as Panel)?.Children.Add(subjectBlock);  // 시간표 셀 안에 Panel이 있는 경우
            }
        }

        private bool IsValidDropTarget(Border target)
        {
            // 예: 같은 시간대에 같은 학년 수업 3개 이상 금지
            if (ViolatesTimeSlotConstraint(target))
            {
                constraintStack.Push(new ConstraintViolation
                {
                    Reason = "동일 학년 수업 3개 초과",
                    Target = target
                });
                return false;
            }

            // 예: 지정된 시간 외에는 입력 금지 (이미 시간 정해져 있는 경우)
            if (target.Tag is string tag && tag == "Fixed")
            {
                constraintStack.Push(new ConstraintViolation
                {
                    Reason = "지정된 시간 외 입력 금지",
                    Target = target
                });
                return false;
            }

            return true;
        }

        private bool ViolatesTimeSlotConstraint(Border target)
        {
            // 해당 시간대에 배치된 블럭 개수 세기
            if (target.Parent is Grid grid)
            {
                int count = 0;
                foreach (UIElement child in grid.Children)
                {
                    if (child is Border b && b.Child != null)
                        count++;
                }
                return count >= 3;
            }
            return false;
        }

        private class Slot
        {
            public Panel? Parent { get; set; }
            public int Index { get; set; }
        }

        private class ConstraintViolation
        {
            public string Reason { get; set; } = string.Empty;
            public Border? Target { get; set; }
        }
    }
}
