using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SkyWingViewer.Models;

//検索サービスとかが string だけじゃなくて対応する　TagID も持ってると若干効率的になるので、そのためのモデル。タグについての情報を保持する。
public class ItemTagModel
{
 
    //未登録のタグの場合、 -1 としておき、利用時に DB に登録してから ID を割り振る事にしておく。
    public int TagId { get; set; }

    public string TagName { get; set; }

    public ItemTagModel(string name,int id = -1)
    {
        TagId = id;
        TagName = name;
    }
}
