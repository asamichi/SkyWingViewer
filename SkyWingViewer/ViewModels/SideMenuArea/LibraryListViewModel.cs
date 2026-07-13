using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows;
using Microsoft.Win32;


namespace SkyWingViewer.ViewModels;

public partial class LibraryListViewModel : ObservableObject,IContextMenu,IOpenCommand
{

    [ObservableProperty]
    public ObservableCollection<LibraryModel> libraryList;

    private ILogger _logger;
    private LibraryService _libraryService;
    private TargetNavigationService _targetNavigationService;
    private IDialogService _dialogService;

    public ICommand OpenCommand { get; }
    public IList<ContextMenuItem> ContextMenuItems { get; }




    public LibraryListViewModel(LibraryService libraryService,ILogger<LibraryListViewModel> logger, TargetNavigationService targetNavigationService, IDialogService dialogService)
    {
        _logger = logger;
        _libraryService = libraryService;
        _targetNavigationService = targetNavigationService;

        libraryList = _libraryService.LibraryList;

        ContextMenuItems = GetContextMenu();
        OpenCommand = LibraryOpenCommand;
        _dialogService = dialogService;
    }

    [RelayCommand]
    public async Task AddLibrary()
    {
        string rootPath = _targetNavigationService.Path;
        string name = new DirectoryInfo(rootPath).Name;

        //LibraryModel newLibrary = new LibraryModel(name, rootPath);

        await _libraryService.AddLibraryListAsync(name,rootPath);
    }



    public IList<ContextMenuItem> GetContextMenu()
    {
        List<ContextMenuItem> list = new();
        list.Add(new ContextMenuItem("ライブラリを削除", RemoveLibraryCommand));
        list.Add(new ContextMenuItem("ライブラリのルートパスの修正", MoveLibraryCommand));
        return list;
    }

    [RelayCommand]
    public async Task RemoveLibrary(LibraryModel target)
    {
        DialogServiceOptions dialogOptions = new DialogServiceOptions();
        dialogOptions.Title = "ライブラリ削除";
        dialogOptions.Message = $"ライブラリ {target.Name} を削除します。よろしいですか？\nライブラリ内に存在する大量のアセットが DB に登録されている場合、この操作には時間がかかる可能性があります。";
        dialogOptions.MessageBoxImage = MessageBoxImage.Warning;

        if(_dialogService.OpenMessageBox(dialogOptions) != true)
        {
            return;
        }

        await _libraryService.RemoveLibraryListAsync(target);
    }


    [RelayCommand]
    public async Task MoveLibrary(LibraryModel target)
    {
        DialogServiceOptions dialogOptions = new DialogServiceOptions();
        dialogOptions.Title = "移動したライブラリフォルダを選択してください（先にエクスプローラー等でライブラリフォルダを移動後、本操作から移動後のライブラリフォルダを指定してください）";
        dialogOptions.InitialDirectory = _targetNavigationService.Path;

        string? targetPath = _dialogService.OpenFolderDialog(dialogOptions);

        if (targetPath != null)
        {
            await _libraryService.MoveLibraryAsync(target, targetPath);
        }

    }


    [RelayCommand]
    public void LibraryOpen(LibraryModel? value)
    {
        if (value == null) return;

        string targetPath = value.RootPath;
        if (System.IO.Directory.Exists(targetPath) == false)
        {
            MessageBox.Show("選択したライブラリが見つかりませんでした");
            _logger.LogWarning("ライブラリ一覧から選択したライブラリの、ルートフォルダにアクセスできませんでした Path:{path}", targetPath);
            return;
        }

        _targetNavigationService.SetPath(targetPath);
    }
}
