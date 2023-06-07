/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Azos.Wave.Mvc
{
  /// <summary>
  /// Represents controller from which all Mvc controllers inherit
  /// </summary>
  [CustomMetadata(typeof(ControllerCustomMetadataProvider))]
  public abstract class Controller : DisposableObject
  {
      #region CONSTS

         public const string DEFAULT_VAR_MVC_ACTION = "mvc-action";
         public const string DEFAULT_MVC_ACTION = "index";

      #endregion

      [NonSerialized]
      internal WorkContext m_WorkContext;

      public virtual string ActionVarName{ get{ return DEFAULT_VAR_MVC_ACTION; }}
      public virtual string DefaultActionName{ get{ return DEFAULT_MVC_ACTION; }}

      /// <summary> Returns current application context</summary>
      public IApplication App => m_WorkContext?.App;

      /// <summary> Returns current work context </summary>
      public WorkContext WorkContext => m_WorkContext;

      /// <summary>
      /// Override to perform custom action name + params -> MethodInfo, param array resolution.
      /// This method rarely needs to be overridden because the framework does the resolution that suites most cases
      /// </summary>
      protected internal virtual MethodInfo FindMatchingAction(WorkContext work, string action, out object[] args)
      {
        args = null;
        return null;
      }


         private static Dictionary<Type, ActionFilterAttribute[]> m_ClassFilters;
         private static Dictionary<MethodInfo, ActionFilterAttribute[]> m_MethodFilters;

         /// <summary>
         /// Returns cached (for performance)ordered array of ActionFilterAttributes for type
         /// </summary>
         protected static ActionFilterAttribute[] GetActionFilters(Type tp)
         {
            var dict = m_ClassFilters;//thread safe copy
            ActionFilterAttribute[] filters = null;

            if (dict!=null && dict.TryGetValue(tp, out filters)) return filters;

            filters = tp.GetCustomAttributes().Where(atr => typeof(ActionFilterAttribute).IsAssignableFrom(atr.GetType()))
                                                              .Cast<ActionFilterAttribute>()
                                                              .OrderBy(a => a.Order)
                                                              .ToArray();

            var newDict = dict!=null ?  new Dictionary<Type, ActionFilterAttribute[]>( dict ) : new Dictionary<Type, ActionFilterAttribute[]>();
            newDict[tp] = filters;
            System.Threading.Thread.MemoryBarrier();
            m_ClassFilters = newDict; //thread safe swap

            return filters;
         }


         /// <summary>
         /// Returns cached (for performance)ordered array of ActionFilterAttributes for action method
         /// </summary>
         protected static ActionFilterAttribute[] GetActionFilters(MethodInfo mi)
         {
            var dict = m_MethodFilters;//thread safe copy
            ActionFilterAttribute[] filters = null;

            if (dict!=null && dict.TryGetValue(mi, out filters)) return filters;

            filters = mi.GetCustomAttributes().Where(atr => typeof(ActionFilterAttribute).IsAssignableFrom(atr.GetType()))
                                                              .Cast<ActionFilterAttribute>()
                                                              .OrderBy(a => a.Order)
                                                              .ToArray();

            var newDict = dict!=null ?  new Dictionary<MethodInfo, ActionFilterAttribute[]>( dict ) : new Dictionary<MethodInfo, ActionFilterAttribute[]>();
            newDict[mi] = filters;
            System.Threading.Thread.MemoryBarrier();
            m_MethodFilters = newDict; //thread safe swap

            return filters;
         }



      /// <summary>
      /// Override to add logic/filtering right before the invocation of action method.
      /// Return TRUE to indicate that request has already been handled and no need to call method body and AfterActionInvocation in which case
      ///  return result via 'out result' parameter.
      ///  The default implementation calls ActionFilters
      /// </summary>
      protected internal virtual async ValueTask<(bool, object)> BeforeActionInvocationAsync(WorkContext work, string action, MethodInfo method, object[] args, object result)
      {
        bool handled;

        //1 Class-level
        var filters = GetActionFilters(GetType());
        if (filters!=null)
        {
          foreach(var filter in filters)
          {
            (handled, result) = await filter.BeforeActionInvocationAsync(this, work, action, method, args, result).ConfigureAwait(false);
            if (handled) return (true, result);
          }
        }

        //2 Method Level
        filters = GetActionFilters(method);
        if (filters != null)
        {
          foreach (var filter in filters)
          {
            (handled, result) = await filter.BeforeActionInvocationAsync(this, work, action, method, args, result).ConfigureAwait(false);
            if (handled) return (true, result);
          }
        }

        return (false, null);
      }

      /// <summary>
      /// Override to add logic/filtering right after the invocation of action method. Must return the result (which can be altered/filtered).
      /// The default implementation calls ActionFilters
      /// </summary>
      protected internal virtual async ValueTask<(bool, object)> AfterActionInvocationAsync(WorkContext work, string action, MethodInfo method, object[] args, object result)
      {
        bool handled;

        //1 Method Level
        var filters = GetActionFilters(method);
        if (filters!=null)
        {
          for(var i=filters.Length-1; i>=0; i--)
          {
            var filter = filters[i];
            (handled, result) = await filter.AfterActionInvocationAsync(this, work, action, method, args, result).ConfigureAwait(false);
            if (handled) return (true, result);
          }
        }

        //2 Class Level
        filters = GetActionFilters(GetType());
        if (filters != null)
        {
          for (var i = filters.Length - 1; i >= 0; i--)
          {
            var filter = filters[i];
            (handled, result) = await filter.AfterActionInvocationAsync(this, work, action, method, args, result).ConfigureAwait(false);
            if (handled) return (true, result);
          }
        }

        return (false, result);
      }


      /// <summary>
      /// Override to add logic/filtering finally after the invocation of action method
      /// </summary>
      protected internal virtual async ValueTask<object> ActionInvocationFinally(WorkContext work, string action, MethodInfo method, object[] args, object result)
      {
         //1 Method Level
         var filters = GetActionFilters(method);
         if (filters!=null)
           for(var i=filters.Length-1; i>=0; i--)
             result = await filters[i].ActionInvocationFinallyAsync(this, work, action, method, args, result);

         //2 Class Level
         filters = GetActionFilters(GetType());
         if (filters!=null)
           for(var i=filters.Length-1; i>=0; i--)
             result = await filters[i].ActionInvocationFinallyAsync(this, work, action, method, args, result);

         return result;
      }

  }
}
