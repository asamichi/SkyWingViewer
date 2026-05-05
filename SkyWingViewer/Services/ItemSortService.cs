using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Primitives;
using SkyWingViewer.Models;
using SkyWingViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SkyWingViewer.Services;

public partial class ItemSortService : ObservableObject
{

    public event Action? SortKeyChanged;

    //これが現在選択されているソートタイプ
    [ObservableProperty]
    private SortType sortKey;

    public List<SortType> SortTypes { get; set; } = new();

    public List<SortTypeList> SortTypeLists { get; set; } = new List<SortTypeList>
    {
        new SortTypeList("名前",  "Model.Metadata.Name",x => x.Metadata.Name),
        new SortTypeList("作成日", "Model.Metadata.CreationFileTime",x => x.Metadata.CreationFileTime),
        new SortTypeList("更新日", "Model.Metadata.ModifiedTime", x => x.Metadata.ModifiedTime)
    };

    public ItemSortService()
    {
        foreach(var sortType in SortTypeLists)
        {
            SortTypes.Add(new SortType(sortType.SortName + "(昇順)", new SortDescription(sortType.SortDescription, ListSortDirection.Ascending),sortType.GetSortTarget));
            SortTypes.Add(new SortType(sortType.SortName + "(降順)", new SortDescription(sortType.SortDescription, ListSortDirection.Descending), sortType.GetSortTarget));
        }

        SortKey = SortTypes[0];
    }

    partial void OnSortKeyChanged(SortType value)
    {
        SortKeyChanged?.Invoke();
    }


}


//利用用。昇順降順両方用意する。
public class SortType
{
    public string SortName { get; private set; }
    public SortDescription SortDescription { get; private set; }

    public Func<FileSystemItemBase, Object> GetSortTarget { get; set; }

    public SortType(string sortName, SortDescription sortDescription, Func<FileSystemItemBase, Object> getSortTarget)
    {
        SortName = sortName;
        SortDescription = sortDescription;
        GetSortTarget = getSortTarget;
    }
}


//登録用
public class SortTypeList
{
    public string SortName { get;private set; }
    public string SortDescription { get; private set; }

    public Func<FileSystemItemBase, Object> GetSortTarget { get; set; }


    public SortTypeList(string sortName, string sortDescription, Func<FileSystemItemBase, Object> getSortTarget)
    {
        SortName=sortName;
        SortDescription=sortDescription;

        GetSortTarget = getSortTarget;
    }
}