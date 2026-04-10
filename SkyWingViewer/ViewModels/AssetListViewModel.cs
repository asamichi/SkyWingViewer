using CommunityToolkit.Mvvm.ComponentModel;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace SkyWingViewer.ViewModels;

//TODO: Bitmap を使用してサムネイルを作成して読み込むようにすること
//TODO: サムネイルは一定の規則に応じて保存して、サムネイルが無いかチェックする処理を追加すること
//TODO: 画像以外のものについては、対応するアイコンなどを用意して表示するようにする


public class AssetListViewModel : ObservableObject
{
    public ObservableCollection<object> Assets { get; } = new();

    public void LoadDirectory(string directoryPath)
    {
        //初期化
        Assets.Clear();

        foreach (var filePath in Directory.EnumerateFiles(directoryPath))
        {
            var asset = AssetFactory.CreateAssetInstance(filePath);
            var vm = AssetListViewModelFactory.Create(asset);
            Assets.Add(vm);
        }
    }
}
