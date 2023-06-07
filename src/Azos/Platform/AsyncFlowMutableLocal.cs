﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.Threading;

namespace Azos.Platform
{
  /// <summary>
  /// Represents a mutable analog of AsyncLocal(T). The mutability is achieved by
  /// wrapping the mutable value in a fake class instance which internal AsyncLocal stores, however changes are done to the
  /// wrapped value, not the class instance which is immutable.
  /// ATTENTION: This structure is designed to work with single linear async strand(flow) like a "logical thread"
  /// as it uses the mutable flow-global value, it should not be transacted from multi-threaded child callers, such as
  /// forking sub-tasks
  /// </summary>
  /// <remarks>
  /// See https://devblogs.microsoft.com/pfxteam/executioncontext-vs-synchronizationcontext/
  /// </remarks>
  public sealed class AsyncFlowMutableLocal<T>
  {
    private class wrap<TData>{  public TData Data; }

    private readonly AsyncLocal<wrap<T>> m_Local;

    public AsyncFlowMutableLocal() => m_Local = new AsyncLocal<wrap<T>>();

    /// <summary>
    /// Provides access to mutable wrapped async local value
    /// </summary>
    public T Value
    {
      get
      {
        var v = m_Local.Value;
        if (v==null) return default(T);
        return v.Data;
      }

      set
      {
        __EnsureInit();
        m_Local.Value.Data = value;
      }
    }

    /// <summary>
    /// Low-level system code - app developers do not call, see AZ#676.
    /// Ensures that local mutable slot is initialized.
    /// This is called by framework at app entry point.
    /// </summary>
    public void __EnsureInit()
    {
      if (m_Local.Value == null) m_Local.Value = new wrap<T>();
    }
  }
}
