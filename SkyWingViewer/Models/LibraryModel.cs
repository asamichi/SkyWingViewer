using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.Models;

public class LibraryModel
{
    public string Name { get; set; }

    public string RootPath { get; set; }

    public LibraryModel(string name, string rootPath){
        Name = name;
        RootPath = rootPath;
    }
}
