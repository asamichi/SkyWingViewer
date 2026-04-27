using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SkyWingViewer.Services;

public interface IItemInformationProvider
{
    List<ItemInformation> InformationItems { get; }

    //InformationItems に情報を入れる。表示ラベルとの対応付けは常に必要な処理では無いので、必要になった時に実行できるようにするメソッド
    void CreateInformationItems();
}


/*
 実装例：
    public void CreateInformationItems()
    {
        InformationItems = ConvertItemMetadataToItemInformations(_asset.Metadata);
    }

    //表示用のデータのラベル付け。_asset.Metadata をラベル付きの ItemInformation リストに。InformationItems にそのまま入れれる。
    public List<ItemInformation> ConvertItemMetadataToItemInformations(ItemMetadata itemMetadata)
    {
        List<ItemInformation> list = new();
        list.Add(new ItemInformation("名前", itemMetadata.Name));
        list.Add(new ItemInformation("作成日時", itemMetadata.CreationFileTime.ToString("yyyy年MM月dd日 HH:mm:ss")));
        list.Add(new ItemInformation("最終アクセス日時", itemMetadata.LastAccessTime.ToString("yyyy年MM月dd日 HH:mm:ss")));
        list.Add(new ItemInformation("最終更新日時", itemMetadata.LastAccessTime.ToString("yyyy年MM月dd日 HH:mm:ss")));

        //!=nullではコンパイラに怒られたので、long 型かチェックして大丈夫なら length として使用
        if (itemMetadata.Length is long length)
        {
            list.Add(new ItemInformation("サイズ", ConvertLongValueToFileSize(length)));
        }

        return list;
    }
 
 
 
 */