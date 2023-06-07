/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Azos.Apps;

namespace Azos.Sky.Locking
{
  public sealed class NOPLockManager : ApplicationComponent, ILockManagerImplementation
  {

    public NOPLockManager(IApplication app) : base(app) { }

    public override string ComponentLogTopic => SysConsts.LOG_TOPIC_LOCKING;

    public LockTransactionResult ExecuteLockTransaction(LockSession session, LockTransaction transaction)
    {
      return  new LockTransactionResult(Guid.Empty, null, LockStatus.TransactionOK, LockErrorCause.Unspecified, null, 0, 0d, null);
    }

    public Task<LockTransactionResult> ExecuteLockTransactionAsync(LockSession session, LockTransaction transaction)
    {
      return Task.FromResult( ExecuteLockTransaction(session, transaction) );
    }

    public bool EndLockSession(LockSession session)
    {
      return true;
    }

    public Task<bool> EndLockSessionAsync(LockSession session)
    {
      return Task.FromResult( EndLockSession(session) );
    }

    public LockSession MakeSession(string path, object shardingID, string description = null, int? maxAgeSec = null)
    {
      return new LockSession(this, path, shardingID, description, maxAgeSec);
    }


    public LockSession this[LockSessionID sid]
    {
      get { return null; }
    }



    public void Configure(Conf.IConfigSectionNode node)
    {

    }

    public bool InstrumentationEnabled
    {
      get
      {
         return false;
      }
      set
      {

      }
    }

    public bool ExternalGetParameter(string name, out object value, params string[] groups)
    {
      value = null;
      return false;
    }

    public IEnumerable<KeyValuePair<string, Type>> ExternalParameters
    {
      get { return Enumerable.Empty<KeyValuePair<string, Type>>(); }
    }

    public IEnumerable<KeyValuePair<string, Type>> ExternalParametersForGroups(params string[] groups)
    {
      return Enumerable.Empty<KeyValuePair<string, Type>>();
    }

    public bool ExternalSetParameter(string name, object value, params string[] groups)
    {
      return false;
    }


  }
}
