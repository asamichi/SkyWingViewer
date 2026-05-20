using MahApps.Metro.Controls;
using Microsoft.Extensions.Logging;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SkyWingViewer.Views.Services;

public class PopupService : IPopupService
{
    private ILogger _logger;

    public PopupService(ILogger<PopupService> logger)
    {
        _logger = logger;
    }
    public void CreatePopup(IPopupControl VM, Object targetPlaceElement, PopupServiceOptions? options = null)
    {
        FrameworkElement? targetElement = targetPlaceElement as FrameworkElement;
        if (targetElement == null)
        {
            _logger.LogWarning("受け取った targetPlaceElement が FrameworkElement では無いか、Null でした。targetPlaceElement : {targetPlaceElement}", targetPlaceElement);
            return;
        }

        if(options == null) options = new PopupServiceOptions();
        if(options.MaxHeight <= 0) options.MaxHeight = Application.Current.MainWindow.ActualHeight;
        if(options.MaxWidth <= 0) options.MaxWidth = Application.Current.MainWindow.ActualWidth;

        // 汎用コンテナとしてのContentControl
        var CC = new ContentControl
        {
            //VM を受け取る。IPopupControl を付けているので、popup として表示されることが想定されており、かつ DataTemplate に登録済みと想定する 
            Content = VM,
        };

        //https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.controls.primitives.popup?view=windowsdesktop-10.0
        var popup = new Popup
        {
            //ポップアップが表示される場所の基準となる要素。
            PlacementTarget = targetElement,
            //ターゲットのすぐ左。今後パラメータ持たせて指定できるようにするべきかも
            Placement = options.PlacementMode,
            //外側をクリックしたら閉じるように
            StaysOpen = false,
            //透明なコントロールを許可するか。セキュリティ的な観点のものっぽく、今回は true 固定で良い
            AllowsTransparency = true,
            Focusable = true,
            PopupAnimation = options.PopupAnimation
        };

        //popup はスクロールしてくれないので、縦幅制限しつつスクロールできるように箱を作る
        var scrollViewer = new ScrollViewer
        {
            Content = CC,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            //横スクロールは許可しない場合下記を有効にする。一旦そのまま
            //HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        //TODO: 必要に応じてパラメータで受け取れるようにする等考える。専用のクラスを作るべきか
        //縦幅の制限
        scrollViewer.MaxHeight = options.MaxHeight;
        scrollViewer.MaxWidth = options.MaxWidth;

        //MahApps: 背景色。App.xaml 指定では反映されなかったためこちらで指定する
        // これが「土台」となる共通のガワ。この中に対応する View が表示される

        //AI: border は細かい調整以外 AI 作。人間は指示のみ
        // リソースからブラシを取得
        var backgroundBrush = ((Brush)Application.Current.FindResource("MahApps.Brushes.ThemeBackground")).Clone();
        // 透明度を設定 (0.0 が完全に透明、1.0 が不透明)
        backgroundBrush.Opacity = 0.9;
        var border = new Border
        {
            Background = backgroundBrush,
            BorderBrush = (Brush)Application.Current.FindResource("MahApps.Brushes.Control.Border"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5), // 角丸
            Padding = new Thickness(10),       // 中身との隙間

            // 中身のViewをセット
            Child = scrollViewer
        };

        popup.Child = border;

        // Popupが閉じた時の掃除。参照を切って消えた後もメモリに居座らないようにする
        popup.Closed += PopupDispose;

        popup.IsOpen = true;

    }

    private void PopupDispose(object? sender, EventArgs e)
    {
        if (sender is Popup popup)
        {
            if(popup.Child is Border border &&
                border.Child is ScrollViewer scroll &&
                scroll.Content is ContentControl cc &&
                cc.Content is IPopupControl vm)
            {
                vm.OnPopupClosed();
            }
            popup.Closed -= PopupDispose;
            popup.PlacementTarget = null;
            popup.Child = null;
        }
    }
}
   