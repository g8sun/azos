/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Azos.Data;
using Azos.Security;

namespace Azos.Instrumentation
{
  /// <summary>
  /// Denotes an entity that has parameters that can be get/set from an external source, such as an app terminal session.
  /// This API is sync only because setting properties is a CPU-bound fast operation
  /// </summary>
  public interface IExternallyParameterized
  {
    /// <summary>
    /// Gets names/types of supported external parameters or null if parameters are not supported in principle
    /// </summary>
    IEnumerable<KeyValuePair<string, Type>> ExternalParameters { get; }

    /// <summary>
    /// Gets names of supported external parameters or null if parameters are not supported in principle
    /// </summary>
    IEnumerable<KeyValuePair<string, Type>> ExternalParametersForGroups(params string[] groups);

    /// <summary>
    /// Gets external parameter value returning true if parameter was found
    /// </summary>
    bool ExternalGetParameter(string name, out object value, params string[] groups);

    /// <summary>
    /// Sets external parameter value, true if parameter name was found and set succeeded
    /// </summary>
    bool ExternalSetParameter(string name, object value, params string[] groups);

  }


  /// <summary>
  /// Specifies when security permissions should be checked while getting/setting external parameters
  /// </summary>
  [Flags]
  public enum ExternalParameterSecurityCheck { None = 0, OnGet = 1, OnSet = 2, OnGetSet = OnGet | OnSet }


  /// <summary>
  /// Decorates properties that may be used as bindable external parameters.
  /// Provides methods for extraction of parameter names, values and binding of external object values into
  ///  public read/write properties decorated by this attribute
  /// </summary>
  [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
  public sealed class ExternalParameterAttribute : Attribute
  {
    public ExternalParameterAttribute() { }

    /// <summary>
    /// Provides a list of group names where this parameter applies
    /// </summary>
    public ExternalParameterAttribute(params string[] groups)
    {
      Groups = groups;
    }

    /// <summary>
    /// Provides a name override with list of groups names where this parameter applies
    /// </summary>
    public ExternalParameterAttribute(string name, ExternalParameterSecurityCheck security, params string[] groups) : this(groups)
    {
      Name = (name ?? string.Empty).Trim();
      SecurityCheck = security;
    }

    /// <summary>
    /// Provides name override for external parameter. When this value is not set
    ///  the name is taken from the decorated member name
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Specifies what security check must be done on get/set
    /// </summary>
    public readonly ExternalParameterSecurityCheck SecurityCheck;

    /// <summary>
    /// Returns null or a set of group names where parameter is applicable.
    /// This is needed to disregard parameters that do not belong to things being managed,
    /// for example, some parameters may be only set for instrumentation, not for glue etc.
    /// </summary>
    public readonly IEnumerable<string> Groups;

    /// <summary>
    /// Returns external parameter names and type - names for read/write public properties decorated with this attribute.
    /// If groups is null then all parameters returned, else parameters must intersect in their group sets with the
    /// supplied value
    /// </summary>
    public static IEnumerable<KeyValuePair<string, Type>> GetParameters(object target, params string[] groups)
    {
      if (target == null) return Enumerable.Empty<KeyValuePair<string, Type>>();
      var tp = target.GetType();
      return GetParameters(tp, groups);
    }

    /// <summary>
    /// Returns external parameter names and type - names for read/write public properties decorated with this attribute.
    /// If groups is null then all parameters returned, else parameters must intersect in their group sets with the
    /// supplied value
    /// </summary>
    public static IEnumerable<KeyValuePair<string, Type>> GetParameters(Type target, params string[] groups)
    {
      return GetParametersWithAttrs(target, groups).Select(t => new KeyValuePair<string, Type>(t.Item1, t.Item2));
    }

    /// <summary>
    /// Returns external parameter names, type and attributes - names for read/write public properties decorated with this attribute.
    /// If groups is null then all parameters returned, else parameters must intersect in their group sets with the
    /// supplied value
    /// </summary>
    public static IEnumerable<Tuple<string, Type, ExternalParameterAttribute>> GetParametersWithAttrs(Type target, params string[] groups)
    {
      if (target == null) yield break;
      var props = suitableProperties(target);

      foreach (var prop in props)
      {
        var atr = prop.GetCustomAttributes(typeof(ExternalParameterAttribute), false).FirstOrDefault() as ExternalParameterAttribute;
        if (atr == null) continue;

        if (groups != null && groups.Length > 0)
        {
          if (atr.Groups == null) continue;
          if (!atr.Groups.Intersect(groups, StringComparer.InvariantCultureIgnoreCase).Any()) continue;
        }

        if (atr.Name.IsNullOrWhiteSpace())
          yield return Tuple.Create(prop.Name, prop.PropertyType, atr);
        else
          yield return Tuple.Create(atr.Name, prop.PropertyType, atr);
      }
    }

    /// <summary>
    /// Gets instrumentation parameter value returning true if parameter was found.
    /// Parameter names are case-insensitive.
    /// If groups is null then all parameters are searched, else parameters must intersect in
    /// their group sets with the supplied value
    /// </summary>
    public static bool GetParameter(IApplication app, object target, string name, out object value, params string[] groups)
    {
      var pi = findByName(app, false, target, name, groups);
      if (pi == null)
      {
        value = null;
        return false;
      }

      value = pi.GetValue(target);
      return true;
    }

    /// <summary>
    /// Sets instrumentation parameter value, true if parameter name was found and set succeeded.
    /// The property is tried to be set directly to the supplied value first, then, in case of assignment error,
    ///  the value is converted into string then tried to be re-converted to target type.
    /// Returns true for successful set. Parameter names are case-insensitive.
    /// If groups is null then all parameters are searched, else parameters must intersect in
    /// their group sets with the supplied value
    /// </summary>
    public static bool SetParameter(IApplication app, object target, string name, object value, params string[] groups)
    {
      var pi = findByName(app, true, target, name, groups);
      if (pi == null)
        return false;

      try
      {
        pi.SetValue(target, value);
      }
      catch
      {
        try
        {
          if (value == null) return false;//could not set
          var strvalue = value.ToString();

          value = strvalue.AsType(pi.PropertyType);
          pi.SetValue(target, value);
        }
        catch
        {
          return false;//could not convert/set
        }
      }
      return true;
    }

    private static PropertyInfo findByName(IApplication app, bool isSet, object target, string name, string[] groups)
    {
      if (target != null && name.IsNotNullOrWhiteSpace())
      {
        name = name.Trim();
        var tp = target.GetType();

        var props = suitableProperties(tp);

        foreach (var prop in props)
        {
          var atr = prop.GetCustomAttributes(typeof(ExternalParameterAttribute), false).FirstOrDefault() as ExternalParameterAttribute;
          if (atr == null) continue;

          if (groups != null && groups.Length > 0)
          {
            if (atr.Groups == null) continue;
            if (!atr.Groups.Intersect(groups).Any()) continue;
          }

          string pname;
          if (atr.Name.IsNullOrWhiteSpace())
            pname = prop.Name;
          else
            pname = atr.Name;

          if (string.Equals(name, pname, StringComparison.InvariantCultureIgnoreCase))//MATCH
          {
            if ( //check security
                (isSet && (atr.SecurityCheck & ExternalParameterSecurityCheck.OnSet) != 0) ||
                (!isSet && (atr.SecurityCheck & ExternalParameterSecurityCheck.OnGet) != 0)
                )
            {
              if (isSet)
              {
                //throws
                Permission.AuthorizeAndGuardAction(app.SecurityManager, prop);
              }
              else
              {
                //filter out, do not throw
                if (!Permission.AuthorizeAction(app.SecurityManager, prop)) return null;//not found because of lack of security
              }
            }
            return prop;
          }
        }
      }
      return null;
    }

    private static IEnumerable<PropertyInfo> suitableProperties(Type tp)
    {
      return tp.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             .Where(pi => pi.CanRead && pi.CanWrite);
    }

  }
}
