/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Linq;
using System.Text;

using Azos.Data;
using Azos.Serialization.JSON;

namespace Azos.Conf
{
  /// <summary>
  /// Provides implementation of configuration based on a classic JSON content
  /// </summary>
  [Serializable]
  public class JSONConfiguration : FileConfiguration
  {
    #region CONSTS

    public const string SECTION_VALUE_ATTR = "-section-value";

    #endregion


    #region .ctor / static

    /// <summary>
    /// Creates an instance of a new configuration not bound to any JSON file
    /// </summary>
    public JSONConfiguration() : base()
    {

    }

    /// <summary>
    /// Creates an isntance of the new configuration and reads contents from a JSON file
    /// </summary>
    public JSONConfiguration(string filename) : base(filename)
    {
      readFromFile();
    }

    /// <summary>
    /// Creates an instance of configuration initialized from JSON content passed as string
    /// </summary>
    public static JSONConfiguration CreateFromJson(string content)
    {
      var result = new JSONConfiguration();
      result.readFromString(content);
      return result;
    }

    /// <summary>
    /// Creates an instance of configuration initialized from JSON content passed as JsonDataMap
    /// </summary>
    public static JSONConfiguration CreateFromJson(JsonDataMap content)
    {
      var result = new JSONConfiguration();
      result.read(content);
      return result;
    }

    #endregion


    #region Public Properties

    #endregion


    #region Public

    /// <summary>
    /// Saves configuration into a JSON file
    /// </summary>
    public override void SaveAs(string filename)
    {
      SaveAs(filename, null, null);

      base.SaveAs(filename);
    }

    /// <summary>
    /// Saves configuration into a JSON file
    /// </summary>
    public void SaveAs(string filename, JsonWritingOptions options = null, Encoding encoding = null)
    {
      var data = ToConfigurationJSONDataMap();
      if (options == null) options = JsonWritingOptions.PrettyPrint;
      JsonWriter.WriteToFile(data, filename, options, encoding);

      base.SaveAs(filename);
    }

    /// <summary>
    /// Saves JSON configuration to string
    /// </summary>
    public string SaveToString(JsonWritingOptions options = null)
    {
      var data = ToConfigurationJSONDataMap();
      return JsonWriter.Write(data, options);
    }

    /// <inheritdoc/>
    public override void Refresh()
    {
      readFromFile();
    }

    /// <inheritdoc/>
    public override void Save()
    {
      SaveAs(m_FileName);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
      return SaveToString();
    }

    #endregion

    #region Private Utils

    private void readFromFile()
    {
      var data = JsonReader.DeserializeDataObjectFromFile(m_FileName, useBom: true, ropt: JsonReadingOptions.DefaultLimitsCaseInsensitive) as JsonDataMap;
      read(data);
    }

    private void readFromString(string content)
    {
      var data = JsonReader.DeserializeDataObject(content, JsonReadingOptions.DefaultLimitsCaseInsensitive) as JsonDataMap;
      read(data);
    }

    private void read(JsonDataMap data)
    {
      if (data == null || data.Count == 0 || data.Count > 1)
        throw new ConfigException(StringConsts.CONFIG_JSON_MAP_ERROR);

      var root = data.First();
      var sect = root.Value as JsonDataMap;
      if (sect == null)
        throw new ConfigException(StringConsts.CONFIG_JSON_MAP_ERROR);

      m_Root = buildSection(root.Key, sect, null);
      m_Root.ResetModified();
    }

    private ConfigSectionNode buildSection(string name, JsonDataMap sectData, ConfigSectionNode parent)
    {
      var value = sectData[SECTION_VALUE_ATTR].AsString();
      ConfigSectionNode result = parent == null ? new ConfigSectionNode(this, null, name, value)
                                              : parent.AddChildNode(name, value);

      foreach (var kvp in sectData)
      {
        if (kvp.Value is JsonDataMap)
          buildSection(kvp.Key, (JsonDataMap)kvp.Value, result);
        else if (kvp.Value is JsonDataArray)
        {
          var lst = (JsonDataArray)kvp.Value;
          foreach (var lnode in lst)
          {
            if (lnode is JsonDataMap lmap) buildSection(kvp.Key, lmap, result);
            else
            if (lnode is JsonDataArray larray) throw new ConfigException(StringConsts.CONFIG_JSON_STRUCTURE_ERROR, new ConfigException("Bad structure: " + sectData.ToJson()));
            else
              result.AddAttributeNode(kvp.Key, "{0}".Args(lnode));
          }
        }
        else
        {
          if (!kvp.Key.EqualsIgnoreCase(SECTION_VALUE_ATTR))
            result.AddAttributeNode(kvp.Key, kvp.Value);
        }
      }

      return result;
    }

    #endregion
  }
}
