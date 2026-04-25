using SkyWingViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// ImageAssetView.xaml の相互作用ロジック
    /// </summary>
    public partial class ImageAssetView : UserControl
    {
        public ImageAssetView()
        {
            InitializeComponent();
        }

        private async void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is ImageAssetViewModel vm)
            {
                //イベント発火テスト用
                //vm.test1();
                /*
                DependencyPropertyChangedEventArgs.NewValue プロパティ (System.Windows) | Microsoft Learn
                https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.dependencypropertychangedeventargs.newvalue?view=windowsdesktop-10.0
                UIElement.IsVisible Property (System.Windows) | Microsoft Learn
                https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.uielement.isvisible?view=windowsdesktop-9.0                
                */
                bool isVisible = (bool)e.NewValue;

                if (isVisible == true)
                {
                    //サムネイルが読み込まれているならもう Load する必要は無いので return する
                    if (vm.Thumbnail != null)
                    {
                        return;
                    }

                    await vm.LoadThumbnail();

                }
                else
                {
                    //TODO: サムネイルメモリに乗りすぎて問題になりそうなら、ここに必要に応じて解放するような処理を入れる
                    //サムネイルがすでに無いならアンロードする必要はない
                    vm.UnloadThumbnail();
                }

            }
        }




        //イベント発火テスト用
        //private async void OnLoaded(object sender, RoutedEventArgs e)
        //{
        //    //if (this.DataContext is ImageAssetViewModel vm)
        //    //{
        //    //    vm.test2();

        //    //}

        //}
    }
}
