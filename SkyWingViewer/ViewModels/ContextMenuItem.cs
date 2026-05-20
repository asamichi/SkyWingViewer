using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace SkyWingViewer.ViewModels;

public class ContextMenuItem
{
    public string Name { get; }
    public ICommand? Command { get; }
    public object? CommandParameter { get; private set; } = null;
    public bool IsSeparator { get; } = false;

    public List<ContextMenuItem>? SubMenuItems { get; set; } = null;

    public ContextMenuItem(string name,ICommand command)
    {
        Name = name;
        Command = command;
    }

    public ContextMenuItem(string name, List<ContextMenuItem> subMenuItems)
    {
        Name = name;
        SubMenuItems = subMenuItems;
    }
    
    public ContextMenuItem()
    {
        Name = "";
        IsSeparator = true;
    }

}
