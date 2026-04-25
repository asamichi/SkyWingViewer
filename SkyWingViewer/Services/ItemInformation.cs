using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.Services;

//各アイテムの詳細の 1 項目。表示名と値
public class ItemInformation
{
    public string Label { get; set; }
    public string Value { get; set; }
    public ItemInformation(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public ItemInformation(ItemInformation itemInformation)
    {
        Label = itemInformation.Label;
        Value = itemInformation.Value;
    }


}
