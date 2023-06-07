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
using Azos.Conf;

namespace Azos.Scripting
{
  /// <summary>
  /// Base attribute for runnable
  /// </summary>
  public abstract class RunnableBaseAttribute : Attribute
  {
  }


  /// <summary>
  /// Base attribute for runnable
  /// </summary>
  public abstract class CategorizedRunnableAttribute : RunnableBaseAttribute
  {
    public const string CONFIG_MESSAGE_ATTR = "message";

    protected CategorizedRunnableAttribute(string category, int order, string config)
    {
      Category      = category;
      Order         = order;
      ConfigContent = config;
    }

    /// <summary>
    /// Category name of the runnable
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// Order of execution
    /// </summary>
    public int    Order  { get; set; }

    /// <summary>
    /// Configuration content in Laconic format
    /// </summary>
    public readonly string ConfigContent;

    private IConfigSectionNode m_Config;

    /// <summary>
    /// Returns parsed configuration content
    /// </summary>
    public IConfigSectionNode Config
    {
      get
      {
        try
        {
          if (m_Config==null)
            m_Config =  ConfigContent.AsLaconicConfig(wrapRootName: "a", handling: Data.ConvertErrorHandling.Throw);
        }
        catch(Exception error)
        {
          throw new ScriptingException(StringConsts.RUN_ATTR_BAD_CONFIG_ERROR + error.ToMessageWithType(), error);
        }

        return m_Config;
      }
    }

    /// <summary>
    /// Returns null or the value of existing root config "message" attribute
    /// </summary>
    public string Message
    {
      get
      {
        if (ConfigContent.IsNullOrWhiteSpace()) return null;
        return Config.AttrByName(CONFIG_MESSAGE_ATTR).Value;
      }
    }
  }


  /// <summary>
  /// Decorates classes that contain runnable methods - the ones that can be  run externally
  /// through scripting runtime or unit testing.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
  public sealed class RunnableAttribute : CategorizedRunnableAttribute
  {
    public RunnableAttribute() : this(null, 0, null) {}
    public RunnableAttribute(string category, int order = 0, string config = null) : base(category, order, config)
    {
    }
  }


  /// <summary>
  /// Decorates methods of classes that can be run
  /// </summary>
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
  public class RunAttribute : CategorizedRunnableAttribute
  {
    public RunAttribute() : this(null, null, 0, null) {}
    public RunAttribute(int order) : this(null, null, order, null) {}
    public RunAttribute(int order, string config) : this(null, null, order, config) {}
    public RunAttribute(string name, int order, string config) : this(null, name, order, config) {}
    public RunAttribute(string config) : this(null, null, 0, config) {}
    public RunAttribute(string name, string config) : this(null, name, 0, config) {}
    public RunAttribute(string category, string name, int order, string config = null) : base(category, order, config)
    {
      if (name.IsNullOrWhiteSpace()) return;

      name = name.Trim();

      if (name.Length>1 && name[0]=='!')
      {
        name = name.Substring(1);
        ExplicitName = true;
      }

      Name = name;
    }


    /// <summary>
    /// Indicates that the run case name was supplied with '!' switch-
    /// the case will run ONLY when explicitly specified in the runner
    /// </summary>
    public readonly bool ExplicitName;

    /// <summary>
    /// Provides an alternate name for the method run case
    /// </summary>
    public readonly string Name;
  }


  /// <summary>
  /// Denotes Runnable entities that hook into initialization and finalization of the instance
  /// </summary>
  public interface IRunnableHook
  {
    /// <summary>
    /// Invoked before the first method in runnable instance. Unit testing may use this as fixture setup
    /// </summary>
    void Prologue(Runner runner, FID id);

    /// <summary>
    /// Handles the post factum call, return true if exception was handled by this method and should NOT be thrown
    /// </summary>
    bool Epilogue(Runner runner, FID id, Exception error);
  }

  /// <summary>
  /// Denotes Runnable entities that explicitly hook into method prologue and epilogue
  /// </summary>
  public interface IRunHook
  {
    /// <summary>
    /// Invoked before every method invocation in a runnable instance. May mutate method call arguments.
    /// Returns true when Prologue handles the method call and runner should NOT continue with method execution and Epilogue calls
    /// </summary>
    bool Prologue(Runner runner, FID id, MethodInfo method, RunAttribute attr, ref object[] args);

    /// <summary>
    /// Handles the post-factum method call, return true if exception (if any) was handled by this method and should NOT be handled/thrown by runner
    /// </summary>
    bool Epilogue(Runner runner, FID id, MethodInfo method, RunAttribute attr, Exception error);
  }

  /// <summary>
  /// Provides base attribute for hooking into run method invocation
  /// </summary>
  public abstract class RunHookAttribute : Attribute
  {
    /// <summary>
    /// Gets all attribute decorations
    /// </summary>
    public static IEnumerable<RunHookAttribute> GetAllOf(MethodInfo mi)
    {
      if (mi==null) return Enumerable.Empty<RunHookAttribute>();
      var all = mi.GetCustomAttributes<RunHookAttribute>(true);
      return all;
    }

    /// <summary>
    /// Returns an optional state then fed into a corresponding After() call
    /// </summary>
    public abstract object Before(Runner runner, object runnable, FID id, MethodInfo mi, RunAttribute attr, ref object[] args);

    /// <summary>
    /// Complementary method for Before(). The optional state is populated by Before().
    /// </summary>
    public abstract void After(Runner runner, object runnable, FID id, MethodInfo mi, RunAttribute attr, object state, ref Exception error);
  }

}
