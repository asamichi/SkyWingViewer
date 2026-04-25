using SkyWingViewer.ViewModels;
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
using SkyWingViewer.Models;

namespace SkyWingViewer.Views;

/// <summary>
/// FavoriteListView.xaml の相互作用ロジック
/// </summary>
public partial class FavoriteListView : UserControl
{
    public FavoriteListView()
    {
        InitializeComponent();
    }

    //TODO: Microsoft.Xaml.Behaviors.Wpf でコードビハインド無しできれいにできそうなので余裕ができたら改造
    private void ListViewItemMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        //sender が ListViewItem で、持っているデータが DirectoryModel なら target に格納。また、ListViewItem 自体は item として格納
        if (sender is ListViewItem { DataContext: DirectoryModel target } item )
        {
            //親コントロール（item の親、つまりリスト自体）のデータコンテキストが IOpenCommand を持っているなら取得
            IOpenCommand viewModel = ItemsControl.ItemsControlFromItemContainer(item)?.DataContext as IOpenCommand;

            if (viewModel == null) return;
            viewModel.OpenCommand.Execute(target);
        }
    }
}
