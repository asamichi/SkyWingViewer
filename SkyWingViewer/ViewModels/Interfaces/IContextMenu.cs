using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.ViewModels;

public interface IContextMenu
{
    IList<ContextMenuItem> ContextMenuItems { get; }
}
