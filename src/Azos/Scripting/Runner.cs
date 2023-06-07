/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Azos.Apps;
using Azos.Conf;
using Azos.Text;

namespace Azos.Scripting
{
  /// <summary>
  /// Provides default implementation of runner that executes code decorated with Runnable/Run attributes.
  /// This class IS NOT thread-safe
  /// </summary>
  public class Runner : ApplicationComponent
  {
    public static readonly char[] DELIMITERS = new []{',', ';', '|'};
    public const char NOT = '~';

    public const string CONFIG_ARGS_SECTION = "args";

            protected class argsVarResolver : IEnvironmentVariableResolver
            {
              public argsVarResolver(Runner runner) => m_Runner = runner;

              private Runner m_Runner;

              public bool ResolveEnvironmentVariable(string name, out string value)
              {
                value = null;
                if (name.IsNullOrWhiteSpace() || m_Runner.Args == null) return false;

                var i = name.IndexOf("@");
                if (i<0 || i==name.Length-1) return false;

                var aname = name.Substring(i+1);

                value =  m_Runner.Args.AttrByName(aname).Value;
                return true;
              }
            }


    public Runner(IRunnerHost host, Assembly asm, IConfigSectionNode config) : base(host)
    {
      Assembly = asm;
      Host = host;
      ConfigAttribute.Apply(this, config);

      var args = config.AttrByName(CONFIG_ARGS_SECTION).Value;
      m_Args = args.AsLaconicConfig();
      m_ArgsResolver = new argsVarResolver(this);
    }

    protected IConfigSectionNode m_Args;
    protected argsVarResolver m_ArgsResolver;

    protected (string val, bool not)[] m_Categories;
    protected (string val, bool not)[] m_Namespaces;
    protected (string val, bool not)[] m_Methods;
    protected (string val, bool not)[] m_Names;


    /// <summary>
    /// Runs artifacts from this assembly
    /// </summary>
    public Assembly Assembly { get; }

    /// <summary>
    /// Host which gets the output of runner
    /// </summary>
    public IRunnerHost Host { get; }


    /// <summary>
    /// Provides Runner instance arguments, default reads the sub-section of initial create args
    /// </summary>
    public virtual IConfigSectionNode Args => m_Args;

    public override string ComponentLogTopic => CoreConsts.RUN_TOPIC;


    protected static string JoinPredicateList((string val, bool not)[] list)
    {
      if (list==null) return string.Empty;
      return string.Join(",", list.Select(one =>  one.not ? (NOT + one.val) : (one.val)  ));
    }

    protected static (string val, bool not)[] ParsePredicateList(string val)
    {
      if (val.IsNullOrWhiteSpace()) return null;
      var segs = val.Split(DELIMITERS);
      return segs.Where(seg => seg.IsNotNullOrWhiteSpace())
                 .Select( seg =>
                 {
                   seg = seg.Trim();
                   return seg.Length>1 && seg[0]==NOT ?(seg.Substring(1), true) : (seg, false);
                 }).ToArray();
    }

    /// <summary>
    /// If set, applies category filter to fixtures and test methods in the specified categories.
    /// Multiple categories are delimited by ',' or ';' or '|'. Use "~" for NOT e.g. "~Disk*" = "All but the ones starting with `Disk`"
    /// </summary>
    [Config]
    public string Categories
    {
      get => JoinPredicateList(m_Categories);
      set => m_Categories = ParsePredicateList(value);
    }

    /// <summary>
    /// If set filters the namespace names.
    /// Multiple patterns delimited by ',' or ';' or '|'. Use "~" for NOT e.g. "~Disk*" = "All but the ones starting with `Disk`"
    /// </summary>
    [Config]
    public string Namespaces
    {
      get => JoinPredicateList(m_Namespaces);
      set => m_Namespaces = ParsePredicateList(value);
    }

    /// <summary>
    /// If set filters the method names.
    /// Multiple patterns delimited by ',' or ';' or '|'. Use "~" for NOT e.g. "~Disk*" = "All but the ones starting with `Disk`"
    /// </summary>
    [Config]
    public string Methods
    {
      get => JoinPredicateList(m_Methods);
      set => m_Methods = ParsePredicateList(value);
    }

    /// <summary>
    /// If set filters by method case names (attribute instance names).
    /// Multiple patterns delimited by ',' or ';' or '|'. Use "~" for NOT e.g. "~Disk*" = "All but the ones starting with `Disk`"
    /// </summary>
    [Config]
    public string Names
    {
      get => JoinPredicateList(m_Names);
      set => m_Names = ParsePredicateList(value);
    }


    /// <summary>
    /// If set, does not call method bodies, only preps for run but does not run methods
    /// </summary>
    [Config]
    public bool Emulate{ get; set; }

    /// <summary>
    /// Runs the methods. This default implementation is single-threaded sequential
    /// </summary>
    public virtual void Run()
    {
      var allRuns = GetRunnables().OrderBy( r => r.attr.Order );
      Host.Start(this);
      foreach(var run in allRuns)
      {
          if (!App.Active) break;
          var fid = FID.Generate();
          Exception error = null;
          object runnable = null;
          try
          {
            runnable = Activator.CreateInstance(run.t);
          }
          catch(Exception ex)
          {
            error = new RunnerException($"Run() -> Activator.CreateInstance(`{run.t.FullNameWithExpandedGenericArgs()}`): {ex.ToMessageWithType()}" , ex);
            Host.EndRunnable(this, fid, null, error);
            continue;
          }

          try
          {
              Host.BeginRunnable(this, fid, runnable);
              try
              {
                var torun = GetRunMethods(run.t).OrderBy( r => r.attr.Order );
                RunAllMethods(fid, runnable, torun);
              }
              catch(Exception ex)
              {
                error = ex;
              }
          }
          finally
          {
              Host.EndRunnable(this, fid, runnable, error);
              var drun = runnable as IDisposable;
              drun?.Dispose();
          }
      }
      Host.Summarize(this);
    }

    /// <summary>
    /// Returns all runnables that satisfy filter (specified by this instance filter properties).
    /// The enumeration is not ordered
    /// </summary>
    public virtual IEnumerable<(Type t, RunnableAttribute attr)> GetRunnables()
    {
      var allTypes = Assembly.GetTypes();

      var allSuitableClasses = allTypes.Where( t => t.IsClass && !t.IsAbstract);

      foreach(var t in allSuitableClasses)
      {
        var attr = t.GetCustomAttribute<RunnableAttribute>(false);
        if (attr == null) continue;

        if (FilterRunnable(t, attr))
          yield return (t, attr);
      }
    }

    /// <summary>
    /// Determines if the runnable should run in which case returns true.
    /// Filter is based on this instance properties (such as Categories etc.)
    /// </summary>
    public virtual bool FilterRunnable(Type tRunnable,  RunnableAttribute attr)
    {
      if (m_Categories!=null)
      {
        if (attr.Category.IsNotNullOrWhiteSpace())
        {
          if (!MatchPredicates(m_Categories, attr.Category, false)) return false;
        }
        else return false;
      }

      return true;
    }

    /// <summary>
    /// Returns all run methods that satisfy filter (specified by this instance filter properties).
    /// The enumeration is not ordered
    /// </summary>
    public virtual IEnumerable<(MethodInfo mi, RunAttribute attr)> GetRunMethods(Type tRunnable)
    {
      var allMethods = tRunnable.GetMethods(BindingFlags.Public | BindingFlags.Instance);

      var hasMethodLevelCategories = allMethods.Any( mi => mi.GetCustomAttributes<RunAttribute>().Any( a => a.Category.IsNotNullOrWhiteSpace()) );

      foreach(var mi in allMethods)
      {
        var attrs = mi.GetCustomAttributes<RunAttribute>();
        foreach(var attr in attrs)
        {
          if (FilterMethod(tRunnable, mi, attr, hasMethodLevelCategories))
          {
            if (mi.IsAsyncMethod() && mi.ReturnType==typeof(void))
              throw new RunnerException(StringConsts.RUN_ASYNC_VOID_NOT_SUPPORTED_METHOD_ERROR.Args(mi.ToDescription()));

            yield return (mi, attr);
          }
        }
      }
    }

    protected static bool MatchPredicates(IEnumerable<(string val, bool not)> predicates, string value, bool caseSensitive)
    {
      return
        (
          !predicates.Any( one => !one.not) ||
           predicates.Where( one => !one.not ).Any( one => value.MatchPattern(one.val, senseCase: caseSensitive))
        )
        &&
        predicates.Where( one => one.not).All(one => !value.MatchPattern(one.val, senseCase: caseSensitive));
    }

    /// <summary>
    /// Determines if the runnable should run in which case returns true.
    /// Filter is based on this instance properties (such as Categories etc.)
    /// </summary>
    public virtual bool FilterMethod(Type tRunnable, MethodInfo mi,  RunAttribute attr, bool runnableHasMethodLevelCategories)
    {
      if (m_Categories!=null)
      {
        if (runnableHasMethodLevelCategories || attr.Category.IsNotNullOrWhiteSpace())
        {
          if (!MatchPredicates(m_Categories, attr.Category, false)) return false;
        }
      }

      if (m_Namespaces!=null)
      {
        if (!MatchPredicates(m_Namespaces, mi.DeclaringType.Namespace, true)) return false;
      }

      if (m_Methods!=null)
      {
        if (!MatchPredicates(m_Methods, "{0}.{1}".Args(mi.DeclaringType.Name, mi.Name), true)) return false;
      }


      if (m_Names==null) return !attr.ExplicitName;
      return MatchPredicates(m_Names, attr.Name, true);
    }

    /// <summary>
    /// Override to do parallel execution etc...
    /// </summary>
    protected virtual void RunAllMethods(FID id, object runnable, IEnumerable<(MethodInfo mi, RunAttribute attr)> methods)
    {
      if (!methods.Any()) return;

      var hook = runnable as IRunnableHook;

      try
      {
        if (hook!=null) hook.Prologue(this, id);

        foreach(var method in methods)
        {
          if (!App.Active) break;
          SafeRunMethod(runnable, method);
        }

        if (hook!=null) hook.Epilogue(this, id, null);
      }
      catch(Exception error)
      {
        var handled = false;
        try
        {
          if (hook!=null) handled = hook.Epilogue(this, id, error);
        }
        catch(Exception e)
        {
          throw new RunnerException(StringConsts.RUN_RUNNER_ALL_METHODS_LEAKED2_ERROR.Args(error.ToMessageWithType(), e.ToMessageWithType() ), e);
        }

        if (!handled) throw new RunnerException(StringConsts.RUN_RUNNER_ALL_METHODS_LEAKED1_ERROR.Args(error.ToMessageWithType()), error);
      }
    }

    /// <summary>
    /// Keep in mind this method may need to be thread-safe if RunAllMethods() is multi-threaded.
    /// Should not throw execution errors
    /// </summary>
    protected virtual void SafeRunMethod(object runnable, (MethodInfo mi, RunAttribute attr) method)
    {
      var fid = FID.Generate();
      Host.BeforeMethodRun(this, fid, method.mi, method.attr);

      var hook = runnable as IRunHook;
      Exception error = null;
      //setup Console redirect
//#576
////      var wasOut = Console.Out;
////      var wasError = Console.Error;
      var alreadyHandled = false;
      try
      {
//commented when Conout was introduced in 2019-2020
 ////       Console.SetOut( Host.ConsoleOut );
////        Console.SetError( Host.ConsoleError );

        var args = MakeMethodParameters(method);
        try
        {
          if (hook!=null) alreadyHandled = hook.Prologue(this, fid, method.mi, method.attr, ref args);

          if (!alreadyHandled)
          {
            if (!this.Emulate) InvokeRunMethod(runnable, fid, method, ref args); //<---- CALL the method

            if (hook!=null) hook.Epilogue(this, fid, method.mi, method.attr, null);
          }
        }
        finally
        {
          args.OfType<IDisposable>().ForEach( d => d.Dispose() );
        }
      }
      catch(Exception ex)
      {
        if (ex is TargetInvocationException)
          ex = ((TargetInvocationException)ex).InnerException;

        error = ex;

        var handled = false;

        try
        {
          if (hook!=null) handled = hook.Epilogue(this, fid, method.mi, method.attr, error);
          if (handled) error = null;
        }
        catch(Exception eex)
        {
          error = new RunnerException(StringConsts.RUN_RUNNER_RUN_LEAKED2_ERROR.Args(error.ToMessageWithType(), eex.ToMessageWithType() ), error);
        }
      }
      finally
      {
//#576
////        Console.SetOut(wasOut);
////        Console.SetOut(wasError);
      }

      Host.AfterMethodRun(this, fid, method.mi, method.attr, error);
    }

    /// <summary>
    /// Calls the actual method checking for RunHookAttributes like averments. This method is not called for emulation
    /// </summary>
    protected virtual void InvokeRunMethod(object runnable, FID id, (MethodInfo mi, RunAttribute attr) method, ref object[] args)
    {
      var hatrs = RunHookAttribute.GetAllOf(method.mi);

      List<(RunHookAttribute a, object s)> state = null;
      foreach(var hatr in hatrs)
      {
        if (state==null) state = new List<(RunHookAttribute, object)>();
        state.Add( (hatr, hatr.Before(this, runnable, id, method.mi, method.attr, ref args) ));
      }

      try
      {
        var ret = method.mi.Invoke(runnable, args); //<------------------ MAKE  A CALL !!!!!
        if (ret is Task t)
          t.GetAwaiter().GetResult();//discard the result, if this throws it throws as-if from Invoke()
      }
      catch(Exception error)
      {
        if (error is TargetInvocationException tie) error = tie.InnerException;

        var original = error;

        if (state!=null)
         state.ForEach(tpl => tpl.a.After(this, runnable, id, method.mi, method.attr, tpl.s, ref error));

        if (error!=null)
        {
          if (error==original) throw;//keep original stack frame
          throw error;
        }
        else
          return;//error suppressed by attribute
      }

      if (state != null)
      {
        Exception error = null;
        state.ForEach(tpl => tpl.a.After(this, runnable, id, method.mi, method.attr, tpl.s, ref error));
        if (error!=null) throw error;
      }
    }

    /// <summary>
    /// Binds parameters supplied by RunAttribute into object[] as required by the supplied MethodInfo
    /// </summary>
    protected virtual object[] MakeMethodParameters((MethodInfo m, RunAttribute a) method)
    {
      var mpars = method.m.GetParameters();
      if (mpars.Length==0) return new object[0];

      if (method.a.ConfigContent.IsNullOrWhiteSpace())
        throw new RunMethodBinderException(StringConsts.RUN_BINDER_METHOD_ARGS_MISSING_ERROR.Args(method.m.Name));

      var conf = method.a.Config;

      //env resolver is specific to this Runner instance
      var clone = new MemoryConfiguration();
      clone.CreateFromNode(conf);
      clone.EnvironmentVarResolver = m_ArgsResolver;
      conf = clone.Root;

      var args = conf["@"];// the "@" section represents the subsection for arguments: 'a=1 b=2' vs 'parallel=true @{a=1 b=2}'
      if (!args.Exists) args = conf;

      var result = new object[mpars.Length];

      for(var i=0; i<result.Length; i++)
      {
        var mpar = mpars[i];

        try
        {
            var arg = args.Navigate("{0}|${0}".Args(mpar.Name));
            if (!arg.Exists )
            {
              if (!mpar.HasDefaultValue)
                throw new RunMethodBinderException(StringConsts.RUN_BINDER_ARG_MISSING_ERROR.Args(method.m.Name, mpar.Name));

              result[i] = mpar.RawDefaultValue;
              continue;
            }

            if (mpar.ParameterType==typeof(IConfigSectionNode) ||
                mpar.ParameterType==typeof(ConfigSectionNode))
            {
              if (!(arg is IConfigSectionNode))
                throw new RunMethodBinderException(StringConsts.RUN_BINDER_SECTION_MISSING_ERROR.Args(method.m.Name, mpar.Name));

              result[i] = arg;
              continue;
            }

            if (typeof(IConfigurable).IsAssignableFrom(mpar.ParameterType))
            {
              var node = arg as IConfigSectionNode;
              if (node==null)
                throw new RunMethodBinderException(StringConsts.RUN_BINDER_SECTION_MISSING_ERROR.Args(method.m.Name, mpar.Name));

              result[i] = FactoryUtils.MakeAndConfigure(node, mpar.ParameterType); //inject the type
              continue;
            }

            //Scalar/simple
            result[i] = arg.ValueAsType(mpar.ParameterType, verbatim: false, strict: false);
        }
        catch(Exception error)
        {
          throw new RunMethodBinderException(StringConsts.RUN_BINDER_BINDING_ERROR.Args(
                                              method.m.DeclaringType.DisplayNameWithExpandedGenericArgs(),
                                              method.m.Name,
                                              mpar.Name,
                                              error.ToMessageWithType(),
                                              args.ToLaconicString(CodeAnalysis.Laconfig.LaconfigWritingOptions.Compact)
                                              ), error);
        }
      }

      return result;
    }

  }
}
