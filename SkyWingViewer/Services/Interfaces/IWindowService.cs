using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.Services;

public interface IWindowService
{
    public void CreateWindow(IWindowViewModel VM, WindowServiceOptions options);
}
