using SkyWingViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SkyWingViewer.Views;

/// <summary>
/// AssetListView.xaml の相互作用ロジック
/// </summary>
public partial class AssetListView : UserControl
{
    public AssetListView()
    {
        InitializeComponent();
    }




    /* ***** ターゲット変更時にスクロール位置を先頭に戻す ***** */
    //AI: このスクロールを上の戻す処理は AI 生成
    private void AssetsListTargetUpdated(object sender, DataTransferEventArgs e)
    {
        if (sender is ListView listView)
        {
            // ListView内部のScrollViewerを探して最上部へ
            var scrollViewer = GetVisualChild<ScrollViewer>(listView);
            scrollViewer?.ScrollToHome(); // または ScrollToTop()
        }
    }

    // 汎用的な VisualTree 探索メソッド
    private T GetVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var result = GetVisualChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }
    /* ***** ターゲット変更時にスクロール位置を先頭に戻す ここまで ***** */

    private void ListViewItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        //IOpenCommand を実装しているなら
        if (sender is ListViewItem { DataContext: IOpenCommand vm })
        {
            vm.OpenCommand.Execute(null);
        }
    }

}


