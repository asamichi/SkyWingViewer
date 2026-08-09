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

namespace SkyWingViewer.Views
{
    /// <summary>
    /// DirectoryView.xaml の相互作用ロジック
    /// </summary>
    public partial class DirectoryView : UserControl
    {
        public DirectoryView()
        {
            InitializeComponent();
        }

        //下記は ImageAssetView のコードビハインドと同様

        //データコンテキスト切り替え時、以前の VM を掃除するために保持する。
        private DirectoryViewModel? _lastVM;

        //イベントハンドラーが void 固定なので、この async void は回避できない
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_lastVM != null)
            {
                _lastVM.ViewCount--;
                if (_lastVM.ViewCount == 0)
                    _lastVM.UnloadThumbnail();
            }

            if (e.NewValue is DirectoryViewModel newVM)
            {
                _lastVM = newVM;
                newVM.ViewCount++;
                //await newVM.LoadThumbnail();
                _ = newVM.LoadThumbnail();
            }
        }
    }
}
