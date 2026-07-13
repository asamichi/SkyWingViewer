using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text;
using System.Windows;

namespace SkyWingViewer.ViewModels;

public partial class TagLabelViewModel :ObservableObject,IContextMenu
{
    public ItemTagModel Model { get; set;  }

    public IList<ContextMenuItem> ContextMenuItems { get; }

    private ItemTagService _itemTagService;


    public TagLabelViewModel(ItemTagModel model,ItemTagService itemTagService)
    {
        _itemTagService = itemTagService;
        Model = model;

        ContextMenuItems = GetContextMenu();
    }


    public List<ContextMenuItem> GetContextMenu()
    {
        List<ContextMenuItem> list = new();
        list.Add(new ContextMenuItem("タグを削除", DeleteTagCommand));

        return list;
    }

    [RelayCommand]
    public async Task DeleteTag()
    {
        // メッセージボックスで確認する
        MessageBoxResult result = MessageBox.Show(
            //メッセージ
            "本当に削除してよろしいですか？\nタグを削除した場合、タグにどのアセットが関連付けられていたかの情報も削除されます。\n削除した場合、これらの情報は復元できません。",
            //メッセージボックスのタイトル
            "削除の確認",
            //ボタンの種類の指定
            MessageBoxButton.YesNo,
            // アイコンの種類（警告）
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            await _itemTagService.UnregisterTagAsync(Model.TagName);
            MessageBox.Show("削除しました。");
        }
        else
        {
            MessageBox.Show("削除操作をキャンセルしました。");
        }
        
    }



}
