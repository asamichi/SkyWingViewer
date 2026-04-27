using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace SkyWingViewer.Models;


public class DirectoryModel : FileSystemItemBase
{
    public DirectoryModel(string path) : base(path)
    {

    }

}
