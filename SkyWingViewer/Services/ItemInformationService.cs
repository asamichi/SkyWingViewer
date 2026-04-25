using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using SkyWingViewer.Models;

namespace SkyWingViewer.Services;

public partial class ItemInformationService : ObservableObject
{

    //変更通知用のイベント
    public event Action? InformationItemChanged;

    //今選択されている対象
    [ObservableProperty]
    private List<IItemInformationProvider>? targetItems;

    //バインドする情報用
    public ObservableCollection<ItemInformation> InformationItem { get; set; } = new();

    public ItemInformationService()
    {
    }

    partial void OnTargetItemsChanged(List<IItemInformationProvider>? value)
    {
        if (value == null || value.Count == 0)
        {
            InformationItem = new ObservableCollection<ItemInformation>();
            OnInformationItemChanged();
            return;
        }

        //詳細を表示する対象が１つの場合
        if (value.Count == 1)
        {
            //まだ詳細データとラベルのセットを作っていない場合には、生成する
            foreach (var item in value)
            {
                if (item.InformationItems == null)
                {
                    item.CreateInformationItems();
                }
            }
            //(List<IItemInformationProvider>.InformationItems は (List<List<ItemInformation>>という状態。
            //ここからList<ItemInformation>を1要素として取り出す
            var processed = value.SelectMany(x => x.InformationItems);
            InformationItem = new ObservableCollection<ItemInformation>(processed);

            //そのまま表示
            OnInformationItemChanged();
            return;
        }
    }

    public static List<ItemInformation> ConvertItemMetadataToItemInformations(ItemMetadata itemMetadata)
    {
        List<ItemInformation> list = new();
        list.Add(new ItemInformation("名前", itemMetadata.Name));
        list.Add(new ItemInformation("作成日時", itemMetadata.CreatonTime.ToString("yyyy年MM月dd日 HH:mm:ss")));
        //いらないと思うのと、ラベルの長さがこれがボトルネックになるので一旦非表示に
        //list.Add(new ItemInformation("最終アクセス日時", itemMetadata.LastAccessTime.ToString("yyyy年MM月dd日 HH:mm:ss")));
        list.Add(new ItemInformation("最終更新日時", itemMetadata.LastModifiedTime.ToString("yyyy年MM月dd日 HH:mm:ss")));

        if (itemMetadata == null)
        {
            //フォルダが対象の場合はサイズが null なので、これで完了
            return list;
        }
        //!=nullではコンパイラに怒られたので、long 型かチェックして大丈夫なら length として使用
        if (itemMetadata.Length is long length)
        {
            list.Add(new ItemInformation("サイズ", ConvertLongValueToFileSize(length)));
        }

        return list;
    }

    public static string ConvertLongValueToFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
        int counter = 0;
        double number = bytes;

        // 1024を超えている間、単位を上げていく
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        // N2 は小数点第 2 位まで表示という意味。suffixes とカウントアップ回数に応じて単位を付けて返す
        // 例: "1.16 GB"
        return $"{number:N2} {suffixes[counter]}";
    }



    private void OnInformationItemChanged()
    {
        InformationItemChanged?.Invoke();
    }

}
