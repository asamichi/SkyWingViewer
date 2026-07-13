using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace SkyWingViewer.ViewModels;


//TODO: popup を閉じたときにまとめてタグ追加、の方がパフォーマンスで改善がありそう

public partial class TagEditViewModel : ObservableObject, IPopupControl
{
    private AssetSelectionService _assetSelectionService;

    //タグの検索欄に入力された文字
    [ObservableProperty]
    private string searchWord = "";

    //登録済みタグのリスト
    [ObservableProperty]
    private ObservableCollection<TagCheckBoxViewModel> tagList = new();

    //タグ登録ボタンを表示するか
    [ObservableProperty]
    private bool tagRegistrationBtnFlag = false;
    [ObservableProperty]
    private TagCheckBoxViewModel? tagRegistrationBtn;

    private ItemTagService _itemTagService;

    private ILogger _logger;
    private IServiceProvider _serviceProvider;


    public TagEditViewModel(AssetSelectionService assetSelectionService,ItemTagService itemTagService,ILogger<TagEditViewModel> logger,IServiceProvider serviceProvider)
    {
        _assetSelectionService = assetSelectionService;
        _itemTagService = itemTagService;
        _logger = logger;
        _serviceProvider = serviceProvider;

        _ = CreateTagList();
    }


    private async Task CreateTagList()
    {
        try
        {
            List<ItemTagModel> tagList = await _itemTagService.GetAllTagsAsync();
            List<string> list = tagList.Select(t => t.TagName).ToList();

            //TODO: 全アセット共通のタグは、既にタグインフォメーションの方が必要としている。その結果を参照するか共通のサービスから情報を得るようにすると、ここでのDBアクセスが1回省略できる。タグ一覧のレスポンスも良くなって UX も向上する。
            HashSet<string> IntersectTags = await _itemTagService.GetIntersectTagsFromAssetListAsync(_assetSelectionService.TargetItems);

            foreach (string tag in list)
            {
                TagList.Add(CreateTagCheckBoxViewModel(tag, IntersectTags.Contains(tag)));
            }
        }
        catch(Exception ex)
        {
            _logger.LogError("CreateTagList の際に例外が発生しました。{ex}", ex);
        }

    }

    private TagCheckBoxViewModel CreateTagCheckBoxViewModel(string name,bool isAttached = false)
    {
        return ActivatorUtilities.CreateInstance<TagCheckBoxViewModel>(_serviceProvider,name, isAttached);
    }


   
    partial void OnSearchWordChanged(string value)
    {
        if (string.IsNullOrEmpty(value) == true)
        {
            TagRegistrationBtnFlag = false;
            return;
        }
        _ = CreateTagRegistrationBtnAsync(value);
    }

    private async Task CreateTagRegistrationBtnAsync(string tagName)
    {
        try
        {
            bool exists = await _itemTagService.IsTagExists(tagName);
            TagRegistrationBtnFlag = !exists;
            if (exists == true) return;
            TagRegistrationBtn = CreateTagCheckBoxViewModel(tagName);
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateTagRegistrationBtnAsync の中で例外が発生しました。{ex}", ex);
        }

    }

}
