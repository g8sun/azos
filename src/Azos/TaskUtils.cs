/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using Azos.Platform;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Azos
{
  /// <summary>
  /// Provides task-related utility functions used by the majority of projects
  /// </summary>
  public static class TaskUtils
  {
    /// <summary>
    /// Chains task 'first' with 'next' if first is completed, not canceled and not faulted.
    /// Returns task that completes when 'next' completes
    /// </summary>
    public static Task OnOk(this Task first, Action next, TaskContinuationOptions options = TaskContinuationOptions.ExecuteSynchronously)
    {
      var tcs = new TaskCompletionSource<object>();

      first.ContinueWith(_ =>
      {
        if (first.IsCanceled)
        {
          tcs.TrySetCanceled();
        }
        else if (first.IsFaulted)
        {
          tcs.TrySetException(first.Exception.InnerExceptions);
        }
        else
        {
          try
          {
            next();
            tcs.TrySetResult(null);
          }
          catch (Exception ex)
          {
            tcs.TrySetException(ex);
          }
        }
      }, options);

      return tcs.Task;
    }

    /// <summary>
    /// Chains task 'first' with 'next' passing result of 'first' to 'next' if first is completed, not cancelled and not faulted.
    /// Returns task that completes when 'next' completes
    /// </summary>
    public static Task OnOk<T1>(this Task<T1> first, Action<T1> next, TaskContinuationOptions options = TaskContinuationOptions.ExecuteSynchronously)
    {
      var tcs = new TaskCompletionSource<object>();

      first.ContinueWith(_ =>
      {
          if (first.IsFaulted)
        {
          tcs.TrySetException(first.Exception.InnerExceptions);
        }
        else if (first.IsCanceled)
        {
          tcs.TrySetCanceled();
        }
        else
        {
          try
          {
            next(first.Result);
            tcs.TrySetResult(null);
          }
          catch (Exception ex)
          {
            tcs.TrySetException(ex);
          }
        }
      }, options);

      return tcs.Task;
    }

    /// <summary>
    /// Chains task 'first' with task returned by 'next' passing result of 'first' to 'next' if first is completed, not cancelled and not faulted.
    /// Returns task that completes after task returned by 'next' completes
    /// </summary>
    public static Task OnOk<T1>(this Task<T1> first, Func<T1, Task> next,
                                TaskContinuationOptions firstOptions = TaskContinuationOptions.ExecuteSynchronously,
                                TaskContinuationOptions nextOptions = TaskContinuationOptions.ExecuteSynchronously)
    {
      var tcs = new TaskCompletionSource<object>();

      first.ContinueWith(_ =>
      {
        if (first.IsFaulted)
          tcs.TrySetException(first.Exception.InnerExceptions);
        else if (first.IsCanceled)
          tcs.TrySetCanceled();
        else
        {
          try
          {
            var t = next(first.Result);
            if (t == null)
              tcs.TrySetException( new AzosException(StringConsts.CANNOT_RETURN_NULL_ERROR + typeof(TaskUtils).FullName + ".Then" ));
            else
            {
              t.ContinueWith(__ =>
              {
                if (t.IsFaulted)
                  tcs.TrySetException(first.Exception.InnerExceptions);
                else if (t.IsCanceled)
                  tcs.TrySetCanceled();
                else
                  tcs.TrySetResult(null);
              }, nextOptions);
            }
          }
          catch (Exception ex)
          {
            tcs.TrySetException(ex);
          }
        }
      }, firstOptions);

      return tcs.Task;
    }

    /// <summary>
    /// Chains task 'first' with task returned by 'next' if first is completed, not cancelled and not faulted.
    /// Returns task that completes after task returned by 'next' completes with result from 'next' task
    /// </summary>
    public static Task<T1> OnOk<T1>(this Task first, Func<Task<T1>> next,
                                TaskContinuationOptions firstOptions = TaskContinuationOptions.ExecuteSynchronously,
                                TaskContinuationOptions nextOptions = TaskContinuationOptions.ExecuteSynchronously)
    {
      var tcs = new TaskCompletionSource<T1>();

      first.ContinueWith(_ =>
      {
        if (first.IsFaulted)
          tcs.TrySetException(first.Exception.InnerExceptions);
        else if (first.IsCanceled)
          tcs.TrySetCanceled();
        else
        {
          try
          {
            var t = next();
            if (t == null)
              tcs.TrySetException( new AzosException(StringConsts.CANNOT_RETURN_NULL_ERROR + typeof(TaskUtils).FullName + ".Then" ));
            else
            {
              t.ContinueWith(__ =>
              {
                if (t.IsFaulted)
                  tcs.TrySetException(first.Exception.InnerExceptions);
                else if (t.IsCanceled)
                  tcs.TrySetCanceled();
                else
                  tcs.TrySetResult(t.Result);
              }, nextOptions);
            }
          }
          catch (Exception ex)
          {
            tcs.TrySetException(ex);
          }
        }
      }, firstOptions);

      return tcs.Task;
    }

    /// <summary>
    /// Chains task 'first' with task returned by 'next' passing result of 'first' to 'next' if first is completed, not cancelled and not faulted.
    /// Returns task that completes after task returned by 'next' completes with result from 'next' task
    /// </summary>
    public static Task<T2> OnOk<T1, T2>(this Task<T1> first, Func<T1, Task<T2>> next,
                                        TaskContinuationOptions firstOptions = TaskContinuationOptions.ExecuteSynchronously,
                                        TaskContinuationOptions nextOptions = TaskContinuationOptions.ExecuteSynchronously)
    {
      var tcs = new TaskCompletionSource<T2>();

      first.ContinueWith( _ =>
      {
        if (first.IsFaulted)
          tcs.TrySetException(first.Exception.InnerExceptions);
        else if (first.IsCanceled)
          tcs.TrySetCanceled();
        else
        {
          try
          {
            var t = next(first.Result);
            if (t == null)
              tcs.TrySetException( new AzosException(StringConsts.CANNOT_RETURN_NULL_ERROR + typeof(TaskUtils).FullName + ".Then" ));
            else
            {
              t.ContinueWith( __ =>
              {
                if (t.IsFaulted)
                  tcs.TrySetException(first.Exception.InnerExceptions);
                else if (t.IsCanceled)
                  tcs.TrySetCanceled();
                else
                  tcs.TrySetResult(t.Result);
              }, nextOptions);
            }
          }
          catch (Exception ex)
          {
            tcs.TrySetException(ex);
          }
        }
      }, firstOptions);

      return tcs.Task;
    }

    /// <summary>
    /// Registers action executed if task was faulted or cancelled
    /// </summary>
    public static Task OnError(this Task task, Action handler)
    {
      var tcs = new TaskCompletionSource<object>();

      task.ContinueWith(_ => {

        if (task.IsFaulted || task.IsCanceled)
        {
          try
          {
            handler();
          }
          catch (Exception ex)
          {
            tcs.TrySetException(ex);
            return;
          }
        }

        if (task.IsFaulted)
          tcs.TrySetException(task.Exception.InnerExceptions);
        else if (task.IsCanceled)
          tcs.TrySetCanceled();
        else
          tcs.TrySetResult(null);

      }, TaskContinuationOptions.ExecuteSynchronously);

      return tcs.Task;
    }

    /// <summary>
    /// Registers action executed disregarding task state
    /// </summary>
    public static Task OnOkOrError(this Task task,  Action<Task> handler)
    {
      var tcs = new TaskCompletionSource<object>();

      task.ContinueWith(_ => {

        try
        {
          handler(task);
        }
        catch (Exception ex)
        {
          tcs.TrySetException(ex);
          return;
        }

        if (task.IsFaulted)
          tcs.TrySetException(task.Exception.InnerExceptions);
        else if (task.IsCanceled)
          tcs.TrySetCanceled();
        else
          tcs.TrySetResult(null);

      }, TaskContinuationOptions.ExecuteSynchronously);

      return tcs.Task;
    }

    /// <summary>
    /// Registers action executed disregarding task state
    /// </summary>
    public static Task<T> OnOkOrError<T>(this Task<T> task, Action<Task> handler)
    {
      var tcs = new TaskCompletionSource<T>();

      task.ContinueWith(_ => {

        try
        {
          handler(task);
        }
        catch (Exception ex)
        {
          tcs.TrySetException(ex);
          return;
        }

        if (task.IsFaulted)
          tcs.TrySetException(task.Exception.InnerExceptions);
        else if (task.IsCanceled)
          tcs.TrySetCanceled();
        else
          tcs.TrySetResult(task.Result);

      }, TaskContinuationOptions.ExecuteSynchronously);

      return tcs.Task;
    }

    /// <summary>
    /// Non-generic version of <see cref="AsCompletedTask{T}(Func{T})"/>
    /// </summary>
    /// <remarks>
    /// Because there is no non-generic <see cref="System.Threading.Tasks.TaskCompletionSource{T}"/> version
    /// generic version typed by <see cref="System.Object"/> is used (<see cref="System.Threading.Tasks.Task{T}"/> inherits from <see cref="System.Threading.Tasks.Task"/>)
    /// </remarks>
    public static Task AsCompletedTask(this Action act)
    {
      return AsCompletedTask<object>(() =>
      {
        act();
        return null;
      });
    }

    /// <summary>
    /// Returns task completed from a synchronous functor
    /// </summary>
    public static Task<T> AsCompletedTask<T>(this Func<T> func)
    {
      var tcs = new TaskCompletionSource<T>();
      try
      {
        tcs.SetResult(func());
      }
      catch (Exception ex)
      {
        tcs.SetException(ex);
      }

      return tcs.Task;
    }

    /// <summary>
    /// Returns the count of items in work segment along with the start index of the first item to be processed
    /// by a particular worker in the worker set
    /// </summary>
    /// <param name="totalItemCount">Total item count in the set processed by all workers</param>
    /// <param name="totalWorkerCount">Total number of workers int the set operating over the totalItemCount</param>
    /// <param name="thisWorkerIndex">The index of THIS worker in the whole worker set</param>
    /// <param name="startIndex">Returns the index of the first item in the assigned segment</param>
    /// <returns>The count of items in the assigned segment</returns>
    public static int AssignWorkSegment(int totalItemCount,
                                        int totalWorkerCount,
                                        int thisWorkerIndex,
                                        out int startIndex)
    {
      if (totalItemCount  <= 0 ||
          totalWorkerCount<= 0 ||
          thisWorkerIndex <  0 ||
          thisWorkerIndex >= totalWorkerCount)
      {
        startIndex = -1;
        return 0;
      }

      var div = totalItemCount / totalWorkerCount;
      var mod = totalItemCount % totalWorkerCount;

      if (thisWorkerIndex < mod)
      {
        var count = div + 1;
        startIndex = thisWorkerIndex * count;
        return count;
      }
      else
      {
        var count = div;
        startIndex = (mod * (div + 1))  +  ((thisWorkerIndex - mod) * count);
        return count;
      }
    }

    private static readonly FiniteSetLookup<Type, PropertyInfo> s_TaskResultPropertyCache = new FiniteSetLookup<Type, PropertyInfo>( t => {
      var pi = t.GetProperty(nameof(Task<object>.Result));
      if (pi == null || pi.PropertyType.FullName == "System.Threading.Tasks.VoidTaskResult") return null;
      return pi;
    });

    /// <summary>
    /// If the passed task is a completed instance of Task&lt;TResult&gt;
    /// returns TResult.Result polymorphically as object
    /// </summary>
    public static (bool ok, object result) TryGetCompletedTaskResultAsObject(this Task task)
    {
      if (task==null || task == Task.CompletedTask) return (false, null);
      if (!task.IsCompletedSuccessfully) return (false, null);

      var pi = s_TaskResultPropertyCache[task.GetType()];

      if (pi == null) return (false, null);

      var result = pi.GetValue(task);
      return (ok: true, result);
    }


    /// <summary>
    /// Loads all CPU cores on this machine with CPU work for the specified number of milliseconds.
    /// This method is typically used to create a fake load which upsets the current thread pool by
    /// taxing all available thread via TPL. This is used to ensure the "different scheduler state" upon completion.
    /// </summary>
    public static void LoadAllCoresFor(int msSpan)
    {
      var sw = Stopwatch.StartNew();
      while (sw.ElapsedMilliseconds < msSpan)
        Parallel.For(1, 1000, i => Text.NaturalTextGenerator.GenerateFullName());
    }

    /// <summary>
    /// SYNCHRONOUSLY awaits a task with result completion: a shortcut to task(t).GetAwaiter().GetResult(t)()
    /// </summary>
    public static T AwaitResult<T>(this Task<T> task) => task.GetAwaiter().GetResult();

    /// <summary>
    /// SYNCHRONOUSLY awaits a task completion: a shortcut to task(void).GetAwaiter().GetResult()
    /// </summary>
    public static void Await(this Task task) => task.GetAwaiter().GetResult();
  }

  /// <summary>
  /// Facilitates execution of actions/functions with parallel asynchronous timeout notification.
  /// This is very useful for logging suspicious activities, e.g. a shutdown code block takes
  /// longer than X to finalize, the timeout is used to report the SLA violation
  /// </summary>
  public static class TimedCall
  {
    /// <summary>
    /// Synchronously runs an action along with asynchronous timeout mechanism.
    /// If the action completes before the timeout expires, then timeout hook is never called.
    /// If synchronous execution of action takes longer than timeout, then timeout is asynchronously fired,
    /// but this does not affect the execution of synchronous action body which continues regardless of timeout
    /// </summary>
    /// <param name="body">Non null synchronous action taking cancellation token</param>
    /// <param name="msTimeout">The timeout in ms, must be greater than 0</param>
    /// <param name="timeout">
    /// The required notification action asynchronously called when the main body is lagging.
    /// All exceptions are handled, so make sure that you handle whatever errors may arise in caller code
    /// </param>
    /// <param name="cancel">Optional cancel token for body and timeout cancellation</param>
    public static void Run(Action<CancellationToken> body, int msTimeout, Action timeout, CancellationToken? cancel = null)
    {
      body.NonNull(nameof(body));
      timeout.NonNull(nameof(timeout));
      msTimeout.IsTrue( a => a > 0, nameof(msTimeout));

      using (var cts = cancel.HasValue ? CancellationTokenSource.CreateLinkedTokenSource(cancel.Value) : new CancellationTokenSource())
      {
        Task.Delay(msTimeout, cts.Token)
            .ContinueWith(d => { try{ if (!d.IsCanceled) timeout(); }catch{ } });

        try
        {
          body(cts.Token);
        }
        finally
        {
          cts.Cancel();
        }
      }
    }

    /// <summary>
    /// Synchronously runs a func along with asynchronous timeout mechanism.
    /// If the function completes before the timeout expires, then timeout hook is never called.
    /// If synchronous execution of function takes longer than timeout, then timeout is asynchronously fired,
    /// but this does not affect the execution of synchronous body which continues regardless of timeout
    /// </summary>
    /// <param name="body">Non null synchronous action taking cancellation token</param>
    /// <param name="msTimeout">The timeout in ms, must be greater than 0</param>
    /// <param name="timeout">
    /// The required notification action asynchronously called when the main body is lagging.
    /// All exceptions are handled, so make sure that you handle whatever errors may arise in caller code
    /// </param>
    /// <param name="cancel">Optional cancel token for body and timeout cancellation</param>
    public static TResult Run<TResult>(Func<CancellationToken, TResult> body, int msTimeout, Action timeout, CancellationToken? cancel = null)
    {
      body.NonNull(nameof(body));
      timeout.NonNull(nameof(timeout));
      msTimeout.IsTrue(a => a > 0, nameof(msTimeout));

      using (var cts = cancel.HasValue ? CancellationTokenSource.CreateLinkedTokenSource(cancel.Value) : new CancellationTokenSource())
      {
        Task.Delay(msTimeout, cts.Token)
            .ContinueWith(d => { try { if (!d.IsCanceled) timeout(); } catch { } });

        try
        {
          return body(cts.Token);
        }
        finally
        {
          cts.Cancel();
        }
      }
    }

  }

}
