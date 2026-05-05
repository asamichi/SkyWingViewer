using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SkyWingViewer.ViewModels;

public partial class EmptyViewModel : FileSystemItemViewModelBase
{
    protected AssetBase _model { get; set; }
    public override FileSystemItemBase Model => _model;

    public EmptyViewModel(AssetBase model)
    {
        _model = model;
    }

}