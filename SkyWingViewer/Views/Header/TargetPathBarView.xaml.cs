using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SkyWingViewer.Views
{
    /// <summary>
    /// TargetPathBarView.xaml の相互作用ロジック
    /// </summary>
    public partial class TargetPathBarView : UserControl
    {
        public TargetPathBarView()
        {
            InitializeComponent();
        }

        // 空きスペースが押されたら TextBox に切り替えてフォーカス
        private void BreadcrumbContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            BreadcrumbContainer.Visibility = Visibility.Collapsed;
            TargetPathTextBox.Focus();
            TargetPathTextBox.SelectAll();
        }

        // フォーカスが外れたらパンくず表示に戻す
        private void TargetPathTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            BreadcrumbContainer.Visibility = Visibility.Visible;
        }

        private void BreadcrumbScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 中身の横幅が変わった（パスが更新された、またはウィンドウ幅が変わった）時
            if (e.ExtentWidthChange != 0)
            {
                // 常に右端までスクロールさせる
                BreadcrumbScrollViewer.ScrollToRightEnd();
            }
        }
    }
}
