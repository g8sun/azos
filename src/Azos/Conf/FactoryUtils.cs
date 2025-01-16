/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Azos.Apps;
using Azos.Data;

namespace Azos.Conf
{
  /// <summary>
  /// Provides helper methods for dynamic object creation and configuration
  /// </summary>
  public static class FactoryUtils
  {
    #region CONSTS

    public const string CONFIG_TYPE_ATTR = "type";
    public const string CONFIG_TYPE_PATH_ATTR = "type-path";

    #endregion

    #region Public

    /// <summary>
    /// Creates and configures an instance of appropriate configurable object as specified by the supplied config node.
    /// Applies configured behaviors
    /// </summary>
    public static IConfigurable MakeAndConfigure(IConfigSectionNode node, Type defaultType = null, object[] args = null)
     => MakeAndConfigure<IConfigurable>(node, defaultType, args);

    /// <summary>
    /// Creates and configures an instance of appropriate configurable component object as specified by the supplied config node.
    /// Applies configured behaviors. By convention passes application as the first constructor argument.
    /// Extra arguments shall not contain application
    /// </summary>
    public static T MakeAndConfigureComponent<T>(IApplication app, IConfigSectionNode node, Type defaultType = null, object[] extraArgs = null)
            where T : IApplicationComponent, IConfigurable
      => MakeAndConfigure<T>(node, defaultType, app.ConcatArray(extraArgs));

    /// <summary>
    /// Creates and configures an instance of appropriate configurable component object as specified by the supplied config node.
    /// Applies configured behaviors. By convention passes director as the first constructor argument.
    /// Extra arguments shall not contain director
    /// </summary>
    public static T MakeAndConfigureDirectedComponent<T>(IApplicationComponent director, IConfigSectionNode node, Type defaultType = null, object[] extraArgs = null)
            where T : IApplicationComponent, IConfigurable
     => MakeAndConfigure<T>(node, defaultType, director.ConcatArray(extraArgs));

    /// <summary>
    /// Creates and configures an instance of appropriate configurable object as specified by the supplied config node.
    /// Applies configured behaviors
    /// </summary>
    public static T MakeAndConfigure<T>(IConfigSectionNode node, Type defaultType = null, object[] args = null)
            where T : IConfigurable
    {
      var result = Make<T>(node, defaultType, args);

      //Check [ConfigMacroContext] injection
      var etp = result.GetType();
      var macroAttr = etp.GetCustomAttributes(typeof(ConfigMacroContextAttribute), true).FirstOrDefault() as ConfigMacroContextAttribute;
      if (macroAttr!=null)
          node.Configuration.MacroRunnerContext = result;

      result.Configure(node);

      Behavior.ApplyConfiguredBehaviors(result, node);

      return result;
    }

    /// <summary>
    /// Creates an instance of appropriate component object as specified by the supplied config node.
    /// By convention passes application as the first constructor argument.
    /// Extra arguments shall not contain application
    /// </summary>
    public static T MakeComponent<T>(IApplication app, IConfigSectionNode node, Type defaultType = null, object[] extraArgs = null)
            where T : IApplicationComponent
     =>  Make<T>(node, defaultType, app.ConcatArray(extraArgs));

    /// <summary>
    /// Creates an instance of appropriate component object as specified by the supplied config node.
    /// By convention passes director as the first constructor argument.
    /// Extra arguments shall not contain director
    /// </summary>
    public static T MakeDirectedComponent<T>(IApplicationComponent director, IConfigSectionNode node, Type defaultType = null, object[] extraArgs = null)
            where T : IApplicationComponent
     => Make<T>(node, defaultType, director.ConcatArray(extraArgs));

    /// <summary>
    /// Creates an instance of appropriate configurable object as specified by the supplied config node.
    /// This function does not configure the instance
    /// </summary>
    public static T Make<T>(IConfigSectionNode node, Type defaultType = null, object[] args = null)
    {
      try
      {
          var tName = (node!=null && node.Exists) ? node.AttrByName(CONFIG_TYPE_ATTR).Value : null;
          return make<T>(node, tName, defaultType, args);
      }
      catch(Exception error)
      {
          throw new ConfigException(string.Format(StringConsts.CONFIGURATION_TYPE_CREATION_ERROR,
                                                    (node!=null) ? node.RootPath : CoreConsts.NULL_STRING,
                                                    error.ToMessageWithType()
                                                  ),
                                    error);
      }
    }

    /// <summary>
    /// Invokes a constructor for a type supplying the .ctor with the specified args:
    ///  node{type="NS.Type, Assembly" arg0=1 arg1=true....}
    /// If the typePattern is passed, then the '*' in pattern is replaced with 'type' attr content.
    /// This is needed for security, as this method allows to inject any type with any ctor params when typePattern is null
    /// </summary>
    public static T MakeUsingCtor<T>(IConfigSectionNode node, string typePattern = null)
    {
      string tpn = CoreConsts.UNKNOWN;
      try
      {
        if (node==null || !node.Exists)
          throw new ConfigException(StringConsts.ARGUMENT_ERROR+"FactoryUtils.Make(node==null|empty)");

        tpn = node.AttrByName(CONFIG_TYPE_ATTR).Value;

        if (tpn.IsNullOrWhiteSpace())
          tpn = typeof(T).AssemblyQualifiedName;
        else
          if (typePattern.IsNotNullOrWhiteSpace())
            tpn = typePattern.Replace("*", tpn);

        var tp = Type.GetType(tpn, true);

        var args = new List<object>();
        for(var i=0; true; i++)
        {
          var attr = node.AttrByName("arg{0}".Args(i));
          if (!attr.Exists) break;
          args.Add(attr.Value);
        }

        var cinfo = tp.GetConstructors().FirstOrDefault(ci => ci.GetParameters().Length == args.Count);
        if (cinfo==null) throw new AzosException(".ctor arg count mismatch");

        //dynamically re-cast argument types
        for(var i=0; i<args.Count; i++)
          args[i] = args[i].ToString().AsType(cinfo.GetParameters()[i].ParameterType);

        try
        {
          return (T)Activator.CreateInstance(tp, args.ToArray());
        }
        catch(TargetInvocationException tie)
        {
          throw tie.InnerException;
        }
      }
      catch(Exception error)
      {
        throw new ConfigException(StringConsts.CONFIGURATION_MAKE_USING_CTOR_ERROR.Args(tpn, error.ToMessageWithType()), error);
      }
    }

    #endregion


    #region .pvt

    private static T make<T>(IConfigSectionNode scope, string tName, Type defaultType, object[] args)
    {
      T result;

      Type t;

      if (tName.IsNullOrWhiteSpace())
      {
        if (defaultType == null)
          throw new ConfigException(StringConsts.CONFIGURATION_TYPE_NOT_SUPPLIED_ERROR);

        tName = defaultType.FullName;
        t = defaultType;
      }
      else
      {
        //search type-path
        t = tryResolveTypeNameInScope(scope, tName);
        if (t == null)
           throw new ConfigException(string.Format(StringConsts.CONFIGURATION_TYPE_RESOLVE_ERROR, tName, CONFIG_TYPE_PATH_ATTR));
      }

      //This MUST be BEFORE allocation attempt for extra security
      if (!typeof(T).IsAssignableFrom(t))
          throw new ConfigException(string.Format(StringConsts.CONFIGURATION_TYPE_ASSIGNABILITY_ERROR, tName, typeof(T).FullName));

      try
      {
        if (args != null)
          result = (T)Activator.CreateInstance(
                    t,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance,
                    null, args, null, null);
        else
          result = (T)Activator.CreateInstance(t, true);
      }
      catch(TargetInvocationException tie)
      {
        throw tie.InnerException;
      }

      return result;
    }

    private static Type tryResolveTypeNameInScope(IConfigSectionNode scope, string tName)
    {
      //is it fully-qualified name?
      var isFqn = tName.IndexOf('.') > 0;

      if (isFqn) return Type.GetType(tName);//or null if it is a bad type spec

      //Scope chain
      while(scope != null && scope.Exists)
      {
        //20240226 DKh #904 Add TypeSearchPath to config scope - has HIGHER precedence than CONFIG attribute
        if (scope.TypeSearchPaths != null)
        {
          var paths = scope.TypeSearchPaths.Where(p => p.IsNotNullOrWhiteSpace()).ToArray();
          foreach (var path in paths)
          {
            var kvp = path.SplitKVP(',');
            var fqn = $"{kvp.Key}.{tName}, {kvp.Value}"; //recompose NS.Type, Assembly key etc..

            var result = Type.GetType(fqn);
            if (result != null) return result; //trip on the first match
          }
        }//20240226 DKh #904 Add TypeSearchPath to config scope


        var atrPaths = scope.AttrByName(CONFIG_TYPE_PATH_ATTR);
        if (atrPaths.Exists) //found attribute but it may be empty which signifies the "reset" higher-level paths behavior
        {
          //or throw that non-fully qualified path does not have any type-path defined
          if (atrPaths.Value.IsNullOrWhiteSpace()) return null;

          var segs = atrPaths.Value.Split(';');
          foreach(var seg in segs.Where(s => s.IsNotNullOrWhiteSpace()))
          {
            var kvp = seg.SplitKVP(',');
            var fqn = $"{kvp.Key}.{tName}, {kvp.Value}"; //recompose NS.Type, Assembly key etc..

            var result =  Type.GetType(fqn);
            if (result != null) return result; //trip on the first match
          }

          //none of the searched segments of type-path matched, so we fail
          return null;
        }//if attr was found

        scope = scope.Parent;//chain
      }//while SCOPE chain

      return null;//not found anywhere
    }

    #endregion
  }
}
