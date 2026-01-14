using System.Windows;
using System.Windows.Input;

namespace DeskWarrior.Windows
{
    /// <summary>
    /// 드래그 이동 및 닫기 기능이 있는 커스텀 윈도우 베이스 클래스
    /// </summary>
    public class DraggableWindow : Window
    {
        protected void OnWindowDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        protected void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
