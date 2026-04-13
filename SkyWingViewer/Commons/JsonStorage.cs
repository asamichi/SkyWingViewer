using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using System.IO;

namespace SkyWingViewer.Commons;

//TODO: ディスク操作になるので、非同期版検討

//ジェネリッククラス。where 以下は TKey が null を許容しないということ。
public class JsonStorage<TKey, TValue> where TKey : notnull
{
    private readonly string _filePath;
    //デフォルトの json を指定する場合使用
    private readonly string _defaultJSONString = "{}";
    //デフォルトのディクショナリを指定する場合に使用
    private readonly Dictionary<TKey, TValue>? _defaultDictionary = null;
    //保存時のオプション
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        WriteIndented = true, // これで改行とインデントが入る
        Converters = { new JsonStringEnumConverter() } //これで Enum が文字列に変換して保存されるように
    };


    public JsonStorage(string fileName)
    {
        //実行ファイル + fileName で json ファイルのパスを生成
        _filePath = Path.Combine(AppContext.BaseDirectory, fileName);
    }
    public JsonStorage(string fileName, string defaultJSONString = "{}") : this(fileName)
    {
        _defaultJSONString = defaultJSONString;
    }
    public JsonStorage(string fileName, Dictionary<TKey, TValue> defaultDictionary) : this(fileName)
    {
        _defaultDictionary = defaultDictionary;
    }

    public void SaveJson(Dictionary<TKey, TValue> data)
    {
        string json = JsonSerializer.Serialize(data, _options);
        File.WriteAllText(_filePath, json);
    }

    public Dictionary<TKey, TValue> LoadJson()
    {
        //ファイルが無い場合にはデフォルト Json のディクショナリを返す
        if (!File.Exists(_filePath))
        {
            return LoadDefault();
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(json) ?? LoadDefault();
        }
        catch
        {
            //ファイルが壊れている場合等、一旦仮。
            return LoadDefault();
            //WILL: JSON が読み込めなかった場合、現存する該当ファイルを日付などつけつつ別のところにコピーする。読み込んだ後保存した際に、壊れているファイルが失われないようにするため（壊れていても一部サルベージできるかもしれないので、残す価値がある） 
        }
    }


    private Dictionary<TKey, TValue> LoadDefault()
    {
        if (_defaultDictionary != null)
        {
            return _defaultDictionary;
        }

        string defaultJson = _defaultJSONString;
        return JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(defaultJson) ?? new Dictionary<TKey, TValue>(); ;
    }
}
