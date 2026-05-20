using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls.Primitives;

namespace SkyWingViewer.Services;

public class PopupServiceOptions
{
    public PlacementMode PlacementMode { get; set; } = PlacementMode.Left;
    public PopupAnimation PopupAnimation { get; set; } = PopupAnimation.Fade;


    //下記はタイミング次第でエラーになりそう（ウィンドウ生成前）なので念のため、ポップアップの性質上本来はおそらく問題無いがサービス側で 0 以下の時に初期値を代入する方式とする
    public double MaxHeight { get; set; } = 0;
    public double MaxWidth { get; set; } = 0;
}
