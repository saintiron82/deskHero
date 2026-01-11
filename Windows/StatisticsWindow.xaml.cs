using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DeskWarrior.Models;

namespace DeskWarrior.Windows
{
    public partial class StatisticsWindow : Window
    {
        private readonly UserStats _stats;

        public StatisticsWindow(UserStats stats)
        {
            InitializeComponent();
            _stats = stats;
            
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadOverview();
            DrawGraph();
        }

        private void LoadOverview()
        {
            if (_stats == null) return;

            TxtTotalDamage.Text = $"{_stats.TotalDamage:N0}";
            TxtMaxDamage.Text = $"{_stats.MaxDamage:N0}";
            TxtMonsterKills.Text = $"{_stats.MonsterKills:N0}";
            TxtTotalInputs.Text = $"{_stats.TotalInputs:N0}";
        }

        private void DrawGraph()
        {
            if (_stats == null || _stats.History.Count == 0) return;

            GraphCanvas.Children.Clear();

            double width = GraphCanvas.ActualWidth;
            double height = GraphCanvas.ActualHeight;
            
            // 데이터가 없거나 캔버스 크기가 0이면 리턴 (안전장치)
            if (width <= 0 || height <= 0) return;

            // 최대값 찾기 (그래프 스케일링용)
            // 최소 1은 보장하여 0나누기 방지
            long maxVal = _stats.History.Max(h => h.Damage);
            if (maxVal == 0) maxVal = 1;

            int count = _stats.History.Count;
            // 최근 24개까지만 보여주지만, 리스트 자체가 24개로 제한되어 있음.
            
            // 바 너비 계산
            // 전체 24칸으로 가정하고 그리기? 아니면 있는 데이터만큼?
            // "24H History"니까 24칸 기준으로 우측 정렬이 좋음.
            double barWidth = width / 24.0;
            double barSpacing = 2.0;
            double activeBarWidth = Math.Max(1.0, barWidth - barSpacing);

            // 우측(최신)부터 그리기
            for (int i = 0; i < count; i++)
            {
                // 역순 인덱스 (0이 최신)
                // History 리스트는 [Old -> New] 순서
                // 따라서 History[count - 1]이 최신임.
                
                var data = _stats.History[count - 1 - i];
                double val = (double)data.Damage;
                
                double barHeight = (val / maxVal) * (height * 0.9); // 90% 높이까지 사용
                if (barHeight < 1 && val > 0) barHeight = 1; // 최소 1픽셀

                // X 위치: 우측 끝에서부터 i번째 칸
                double x = width - ((i + 1) * barWidth);
                double y = height - barHeight;

                var rect = new Rectangle
                {
                    Width = activeBarWidth,
                    Height = barHeight,
                    Fill = new SolidColorBrush(Color.FromRgb(255, 170, 0)), // #FFAA00
                    ToolTip = $"Time: {data.TimeStamp:HH:mm}\nDamage: {val:N0}\nKills: {data.Kills}"
                };

                Canvas.SetLeft(rect, x + (barSpacing / 2));
                Canvas.SetTop(rect, y);

                GraphCanvas.Children.Add(rect);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
