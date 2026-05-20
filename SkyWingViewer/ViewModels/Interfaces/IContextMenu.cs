using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.ViewModels;

public interface IContextMenu
{
    IList<ContextMenuItem> ContextMenuItems { get; }
}


/*
 1. IContextMenu を継承して実装
 2. 下記のように ContextMenu に対して、AssetContextMenu を適用

            <ListView.ItemContainerStyle>
                <Style TargetType="ListViewItem">
                    <!--ダブルクリック時の動作-->
                    <!--<EventSetter Event="MouseDoubleClick" Handler="ListViewItemMouseDoubleClick"/>-->
                    <!--右クリックの操作-->
                    <Setter Property="ContextMenu" Value="{StaticResource AssetContextMenu}"/>
                </Style>
            </ListView.ItemContainerStyle>

 
 */