using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.ViewModels;

public partial class MemoViewModel : ObservableObject
{
    [ObservableProperty]
    public string memoText = "";

    [ObservableProperty]
    private int saveMsgFlag = 0;

    private MemoService _memoService;

    private AssetSelectionService _assetSelectionService;

    [ObservableProperty]
    private int selectedCount = 0;

    //[ObservableProperty]
    //private int memoAreaFlag = 0;
    
    public MemoViewModel(MemoService memoService,AssetSelectionService assetSelectionService)
    {
        _memoService = memoService;
        _assetSelectionService = assetSelectionService;

        _assetSelectionService.TargetItemsChanged += OnTargetItemsChanged;
    }

    private bool CanSaveMemo()
    {
        return SelectedCount == 1;
    }

    [RelayCommand(CanExecute = nameof(CanSaveMemo))]
    public async Task SaveMemo()
    {

        await _memoService.SetMemoAsync(_assetSelectionService.TargetItems, MemoText);
        SaveMsgFlag = 1;
        await Task.Delay(1000);
        SaveMsgFlag = 0;
    }

    public void OnTargetItemsChanged()
    {
        SelectedCount = _assetSelectionService.TargetItems.Count;
        SaveMemoCommand.NotifyCanExecuteChanged();
        if (SelectedCount != 1)
        {
            MemoText = "選択対象のアセットが１つでは無いためメモを表示しません";
            //MemoAreaFlag = 0;
            return;
        }
        //MemoAreaFlag = 1;
        _ = GetMemo(_assetSelectionService.TargetItems[0]);
    }

    private async Task GetMemo(FileSystemItemBase asset)
    {
        MemoText = await _memoService.GetMemoAsync(asset) ?? "";
    }

}
