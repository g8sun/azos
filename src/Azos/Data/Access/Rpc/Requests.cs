﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Azos.Apps.Injection;
using Azos.Data.Business;
using Azos.Serialization.Bix;
using Azos.Serialization.JSON;

namespace Azos.Data.Access.Rpc
{
  /// <summary>
  /// Represents a data fetch RPC request
  /// </summary>
  [Bix("ae669802-bfb9-4acb-9b86-ec43248c909c")]
  [Schema(Description = "Represents a data fetch RPC request")]
  public class ReadRequest : FilterModel<JsonDataMap>
  {
    [Inject] IRpcHandler m_Rpc;

    /// <summary>
    /// Provides optional header map used for whole read request execution
    /// </summary>
    [Field(description: "Provides optional header map used for whole read request execution")]
    public JsonDataMap RequestHeaders { get; set; }

    [Field(required: true, Description = "Command object to execute")]
    public Command Command { get; set; }

    public override ValidState Validate(ValidState state, string scope = null)
    {
      state = base.Validate(state, scope);

      if (state.ShouldContinue)
      {
        state = m_Rpc.ValidateReadRequest(state, this);
      }

      return state;
    }

    protected override async Task<SaveResult<JsonDataMap>> DoSaveAsync()
     => new SaveResult<JsonDataMap>(await m_Rpc.ReadAsync(this).ConfigureAwait(false));
  }

  /// <summary>
  /// Represents a data change transactional (batch) RPC request
  /// </summary>
  [Bix("ab1d19a2-80d7-448c-9dfa-b319fb998fe9")]
  [Schema(Description = "Represents a data change transactional (batch) RPC request")]
  public class TransactRequest : PersistedModel<ChangeResult>
  {
    [Inject] IRpcHandler m_Rpc;

    /// <summary>
    /// Provides optional header map used for transaction execution
    /// </summary>
    [Field(description: "Provides optional header map used for transaction execution")]
    public JsonDataMap RequestHeaders { get; set; }

    /// <summary>
    /// A list of commands to execute under the same logical transaction
    /// </summary>
    [Field(required: true,
           minLength: 1,
           Description = "A list of commands to execute under the same logical transaction")]
    public List<Command> Commands { get; set; }

    public override ValidState Validate(ValidState state, string scope = null)
    {
      state = base.Validate(state, scope);

      if (state.ShouldContinue)
      {
        state = m_Rpc.ValidateTransactRequest(state, this);
      }

      return state;
    }

    protected override async Task<SaveResult<ChangeResult>> DoSaveAsync()
     => new SaveResult<ChangeResult>(await m_Rpc.TransactAsync(this).ConfigureAwait(false));
  }
}
