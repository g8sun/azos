﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using Azos.Conf;
using Azos.Scripting.Dsl;
using Azos.Serialization.JSON;

namespace Azos.Data.Dsl
{
  public class JsonObjectLoader : DataLoader<JsonObjectDataSource>
  {
    public JsonObjectLoader(StepRunner runner, IConfigSectionNode cfg, int idx) : base(runner, cfg, idx) { }

    [Config]
    public string FileName { get; set; }

    [Config]
    public string Json { get; set; }

    protected override IDataSource MakeDataSource(JsonDataMap state)
    {
      var fn = Eval(FileName, state);
      var json = Eval(Json, state);

      if (fn.IsNotNullOrWhiteSpace())
        return JsonObjectDataSource.FromFile(Name, fn);
      else
        return JsonObjectDataSource.FromJson(Name, json);
    }
  }

  public sealed class JsonObjectDataSource : DisposableObject, IDataSource<IJsonDataObject>
  {
    public static JsonObjectDataSource FromFile(string name, string fileName)
     => new JsonObjectDataSource(name.NonBlank(nameof(name)), JsonReader.DeserializeDataObjectFromFile(fileName));

    public static JsonObjectDataSource FromJson(string name, string json)
     => new JsonObjectDataSource(name.NonBlank(nameof(name)), JsonReader.DeserializeDataObject(json));

    private JsonObjectDataSource(string name, IJsonDataObject data)
    {
      m_Name = name;
      m_Data = data;
    }

    private string m_Name;
    private IJsonDataObject m_Data;

    public string Name => m_Name;
    public IJsonDataObject Data => m_Data;
    public object ObjectData => m_Data;
  }


}
