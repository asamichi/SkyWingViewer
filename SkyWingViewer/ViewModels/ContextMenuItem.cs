using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace SkyWingViewer.ViewModels;

public class ContextMenuItem
{
    public string Name { get; }
    public ICommand Command { get; }
    public object? CommandParameter { get; } = null;
    public bool IsSeparator { get; } = false;

    public ContextMenuItem(string name,ICommand command,object commandParameter = null,bool isSeparator = false)
    {
        Name = name;
        Command = command;
        CommandParameter = commandParameter;
        IsSeparator = isSeparator;
    }
}
