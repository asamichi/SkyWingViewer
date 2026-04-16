using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using System.IO;

namespace SkyWingViewer.Commons;



/*
 * 設定保持クラスのメンバを変更した際に、json で追えないような変更をするとユーザの設定が飛ぶ場合がある。
 * この場合、ExtraData にクラスに読めなかった内容が入っていくので、最悪サルベージできる。
 */
public abstract class JsonStorageSettingBase
{
    //Json とクラスのプロパティなんかに不一致があって行く先が無い内容がここに入っていく。
    //この内容もそのまま保持されて保存されるので、最悪事故ってもここから元々の内容をサルベージして、新しく対応する箇所に移植してあげればよい
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraData { get; set; }
}


//TODO: ディスク操作になるので、非同期版検討と思ったが、設定程度の頻度なら必要ないかも

//ジェネリッククラス。where 以下は T がクラスかつ、new() は引数無しのコンストラクタの存在を保証する
//設定が飛ぶことは必ず避ける必要があるため、JsonStorageSettingsBase を継承したクラスに利用を制限する
public class JsonStorage<T> where T : JsonStorageSettingBase, new()
{
    private readonly string _filePath;


    //下記は「どちらか片方の使用」を想定。デフォルトとして設定しやすい方を使う。
    //デフォルトの json を指定する場合使用
    private readonly string? _defaultJSONString = null;
    //デフォルトのインスタンスを指定する場合に使用
    private readonly T? _defaultInstance = null;



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

    //TODO: 文字列のバリデーション
    public JsonStorage(string fileName, string defaultJSONString) : this(fileName)
    {
        _defaultJSONString = defaultJSONString;
    }

    public JsonStorage(string fileName, T defaultInstance) : this(fileName)
    {
        _defaultInstance = defaultInstance;
    }


    //TODO: 絶対消失したくないので、ダブルライト機構にすることを検討。
    /*
     * https://zenn.dev/arika/articles/20251010-csharp-replace-is-atomic
     * https://learn.microsoft.com/ja-jp/windows/win32/fileio/deprecation-of-txf
     * replace でも良いかもしれない。MySQL のダブルライトはファイル単位の話ではないというのがあったが、今回はファイル単位なのでこれでも対応はできる。
     * https://learn.microsoft.com/ja-jp/dotnet/api/system.io.file.replace?view=net-8.0#system-io-file-replace(system-string-system-string-system-string-system-boolean)
     * ここにはそんな事書いてはいないので要検討。
     */
    public void SaveJson(T data)
    {
        string json = JsonSerializer.Serialize(data, _options);
        File.WriteAllText(_filePath, json);
    }

    public T LoadJson()
    {
        //ファイルが無い場合にはデフォルト Json のディクショナリを返す
        if (!File.Exists(_filePath))
        {
            return LoadDefault();
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<T>(json,_options) ?? LoadDefault();
        }
        catch
        {
            //ファイルが壊れている場合等、一旦仮。
            return LoadDefault();
            //WILL: JSON が読み込めなかった場合、現存する該当ファイルを日付などつけつつ別のところにコピーする。読み込んだ後保存した際に、壊れているファイルが失われないようにするため（壊れていても一部サルベージできるかもしれないので、残す価値がある） 
        }
    }


    private T LoadDefault()
    {
        if (_defaultInstance != null)
        {
            //参照渡しにならなようにコピーを複製して返す
            string json = JsonSerializer.Serialize(_defaultInstance, _options);
            return JsonSerializer.Deserialize<T>(json, _options) ?? new T();
        }

        if(_defaultJSONString != null)
        {
            return JsonSerializer.Deserialize<T>(_defaultJSONString) ?? new T(); ;

        }

        return new T();
    }
}
