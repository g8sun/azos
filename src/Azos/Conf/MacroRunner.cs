/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Globalization;

using Azos.Data;
using Azos.Time;
using Azos.Apps;
using System.Linq;

namespace Azos.Conf
{
  /// <summary>
  /// Represents an entity that runs config var macros
  /// </summary>
  public interface IMacroRunner
  {
    /// <summary>
    /// Runs macro
    /// </summary>
    string Run(IConfigSectionNode node, string inputValue, string macroName, IConfigSectionNode macroParams, object context = null);
  }


  /// <summary>
  /// Provides default implementation for configuration variable macros.
  /// NOTE: When serialized a new instance is created which will not equal by reference to static.Instance property
  /// </summary>
  [Serializable]
  public class DefaultMacroRunner : IMacroRunner
  {
    public const string AS_PREFIX = "as-";

    private static DefaultMacroRunner s_Instance = new DefaultMacroRunner();
    private DefaultMacroRunner() { }

    /// <summary>Returns a singleton class instance </summary>
    public static DefaultMacroRunner Instance => s_Instance;

    /// <summary>
    /// Returns a string value converted to desired type with optional default and format
    /// </summary>
    /// <param name="value">String value to convert</param>
    /// <param name="type">A type to convert string value into i.e. "decimal"</param>
    /// <param name="dflt">Default value which is used when conversion of original value can not be made</param>
    /// <param name="fmt">Format string that formats the converted value. Example: 'Goods: {0}'. The '0' index is the value</param>
    /// <returns>Converted value to desired type then back to string, using optional formatting and default if conversion did not succeed</returns>
    public static string GetValueAs(string value, string type, string dflt = null, string fmt = null)
    {
      var mn = "As" + type.CapitalizeFirstChar();

      var mi = typeof(StringValueConversion).GetMethods()
                                            .FirstOrDefault(i => i.IsStatic && i.IsPublic && i.Name == mn && i.GetParameters().Length == 2);

      if (mi == null) throw new ConfigException("Macro `{0}` not found".Args(mn));

      object result;
      if (!string.IsNullOrWhiteSpace(dflt))
      {
        var dval = mi.Invoke(null, new object[] { dflt, null });

        result = mi.Invoke(null, new object[] { value, dval });
      }
      else
      {
        result = mi.Invoke(null, new object[] { value, null });
      }

      if (result == null) return string.Empty;

      if (!string.IsNullOrWhiteSpace(fmt))
        return string.Format(fmt, result);
      else
        return result.ToString();
    }

    /// <summary>
    /// Virtual method to run the named Macro using the provided input section node, input string, params section node, and context object
    /// </summary>
    public virtual string Run(IConfigSectionNode node, string inputValue, string macroName, IConfigSectionNode macroParams, object context = null)
    {

      if (macroName.StartsWith(AS_PREFIX, StringComparison.InvariantCultureIgnoreCase) && macroName.Length > AS_PREFIX.Length)
      {
        var type = macroName.Substring(AS_PREFIX.Length);

        return GetValueAs(inputValue,
                          type,
                          macroParams.Navigate("$dflt|$default").Value,
                          macroParams.Navigate("$fmt|$format").Value);

      }
      else if (macroName.EqualsIgnoreCase("now"))
      {
        return Macro_now(macroParams, context);
      }
      else if (macroName.EqualsIgnoreCase("ctx-name"))
      {
        if (context is Collections.INamed)
          return ((Collections.INamed)context).Name;
      }
      else if (macroName.EqualsIgnoreCase("decipher")) //#870
      {
        return Macro_decipher(node, inputValue, macroName, macroParams, context);
      }


      return inputValue;
    }

    public virtual string Macro_now(IConfigSectionNode macroParams, object context)
    {
      var utc = macroParams.AttrByName("utc").ValueAsBool(false);

      var fmt = macroParams.Navigate("$fmt|$format").ValueAsString();

      var valueAttr = macroParams.AttrByName("value");


      var now = Ambient.UTCNow;
      if (!utc)
      {
        ILocalizedTimeProvider timeProvider = context as ILocalizedTimeProvider;
        if (timeProvider == null && context is IApplicationComponent cmp)
        {
          timeProvider = cmp.ComponentDirector as ILocalizedTimeProvider;
          if (timeProvider == null) timeProvider = cmp.App;
        }

        now = timeProvider != null ? timeProvider.LocalizedTime : now.ToLocalTime();
      }

      // We inspect the "value" param that may be provided for testing purposes
      if (valueAttr.Exists)
        now = valueAttr.Value.AsDateTimeFormat(now, fmt,
                 utc ? DateTimeStyles.AssumeUniversal : DateTimeStyles.AssumeLocal);

      return fmt == null ? now.ToString() : now.ToString(fmt);
    }

    //#870
    public virtual string Macro_decipher(IConfigSectionNode node, string inputValue, string macroName, IConfigSectionNode macroParams, object context)
    {
      if (inputValue.IsNullOrWhiteSpace()) //inputValue takes precedence
      {
        inputValue = macroParams.ValOf("value", "val", "v");//this is used if input value is empty
      }

      var algorithmName = macroParams.ValOf("algo", "alg", "a", "algorithm");//this may be blank
      var required      = macroParams.Of("req", "require", "required").ValueAsBool(true);
      var allowFailure  = macroParams.Of("allow-failure").ValueAsBool(false);
      var toString      = macroParams.Of("string", "str", "text", "txt").ValueAsBool(false);//true to decode into string vs base:64 byte array

      if (inputValue.IsNotNullOrWhiteSpace())
      {
        var result = Security.TheSafe.DecipherConfigValue(inputValue, toString, algorithmName);

        if (result == null && !allowFailure)
        {
          throw new ConfigException(StringConsts.CONFIG_MACRO_DECIPHER_FAILURE_ERROR.Args(inputValue.TakeFirstChars(8), inputValue.Length, node.RootPath));
        }

        return result;
      }

      if (required)
      {
        throw new ConfigException(StringConsts.CONFIG_MACRO_DECIPHER_RQUIRED_ERROR.Args(node.RootPath));
      }

      return null;
    }
  }

}
