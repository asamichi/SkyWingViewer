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
                /*
                DependencyPropertyChangedEventArgs.NewValue プロパティ (System.Windows) | Microsoft Learn
                https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.dependencypropertychangedeventargs.newvalue?view=windowsdesktop-10.0
                UIElement.IsVisible Property (System.Windows) | Microsoft Learn
                https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.uielement.isvisible?view=windowsdesktop-9.0                
                */
                bool isVisible = (bool)e.NewValue;

                if (isVisible)
                {
                    //サムネイルが読み込まれているならもう Load する必要は無いので return する
                    if (vm.Thumbnail != null)
                    {
                        return;
                    }
                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        await vm.LoadThumbnail(cancellationTokenSource.Token);
                    }
                }
                else
                {
                    //TODO: サムネイルメモリに乗りすぎて問題になりそうなら、ここに必要に応じて解放するような処理を入れる
                }

            }
        }
    }
}
