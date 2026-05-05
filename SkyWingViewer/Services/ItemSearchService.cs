using CommunityToolkit.Mvvm.ComponentModel;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SkyWingViewer.Services;

public partial class ItemSearchService : ObservableObject
{
    [ObservableProperty]
    public List<string> searchWords = new();

    public event Action? SearchWordsChanged;

    public ItemSearchService()
    {
       
    }

    public bool IsTarget(FileSystemItemBase item)
    {
        if (SearchWords == null || SearchWords.Count == 0) return true;

        return SearchWords.All(SearchWord => item.Metadata.Name.Contains(SearchWord, StringComparison.OrdinalIgnoreCase));
    }

    //検索
    //ex) .Search<FileSystemItemViewModelBase>(assets,x=>x._model.Name.Contains("検索ワード");
    public IEnumerable<T> Search<T>(IList<T> list, Func<T, bool> SearchRequirements)
    {
        //return list.Where(SearchRequirements);
        foreach (T item in list)
        {
            if (SearchRequirements(item))
            {
                yield return item;
            }
        }
    }

    partial void OnSearchWordsChanged(List<string> value)
    {
        SearchWordsChanged?.Invoke();
    }

}
