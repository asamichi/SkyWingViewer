using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using SkyWingViewer.Services;
using System.Windows.Data;

namespace SkyWingViewer.ViewModels;

public partial class ImageViewerMainViewModel : ObservableObject, IWindowViewModel
{
    [ObservableProperty]
    public string title = "SkyWingViewer - 内蔵画像ビューワ";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaxIndex))]
    private ObservableCollection<FileSystemItemBase> imageModels = new();

    public ListCollectionView ImageView { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentImagePath))]
    [NotifyCanExecuteChangedFor(nameof(MovePrevCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveNextCommand))]
    private int currentIndex = 0;

    //public string? CurrentImagePath => (ImagePaths.Count > 0 && CurrentIndex >= 0 && CurrentIndex < ImagePaths.Count)
    //    ? ImagePaths[CurrentIndex] : null;
    public string? CurrentImagePath => (CurrentIndex >= 0 && CurrentIndex < ImageView.Count)
    ? (ImageView.GetItemAt(CurrentIndex) as ImageAsset)?.Path  : null;


    //public int MaxIndex => Math.Max(0, ImagePaths.Count - 1);
    public int MaxIndex => Math.Max(0, ImageView.Count - 1);


    private ItemSearchService _itemSearchService;
    public ImageViewerMainViewModel(IEnumerable<FileSystemItemBase> imageModelList,ItemSearchService itemSearchService,FileSystemItemBase? targetImage = null)
    {
        _itemSearchService = itemSearchService;

        if(targetImage != null && targetImage.GetType()  != typeof(ImageAsset))
        {
            targetImage = null; 
        }
        //ImagePaths = new ObservableCollection<string>(imageModelList.OfType<ImageAsset>().Select(i => i.Path));
        ImageModels = new ObservableCollection<FileSystemItemBase>(imageModelList.OfType<ImageAsset>());
        ImageView = new ListCollectionView(ImageModels);

        ImageView.Filter = (obj =>
        {
            if (obj is ImageAsset image)
            {
                return _itemSearchService.IsTarget(image);
            }
            return true;
        });

        ImageView.Refresh();

        if (targetImage != null)
        {
            currentIndex = ImageView.IndexOf(targetImage);
        }

    }


    [RelayCommand(CanExecute =nameof(CanMovePrev))]
    private void MovePrev()
    {
        CurrentIndex--;
    }

    private bool CanMovePrev()
    {
        return CurrentIndex > 0;
    }


    [RelayCommand(CanExecute = nameof(CanMoveNext))]
    private void MoveNext()
    {
        CurrentIndex++;
    }

    private bool CanMoveNext()
    {
        //return ImagePaths.Count - 1 > CurrentIndex;
        return ImageView.Count - 1 > CurrentIndex;

    }


}
