﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.Threading.Tasks;
using Azos.Apps;

namespace Azos.Security
{
  /// <summary>
  /// Checks that user is authenticated and does not care about access level to any specific permission beyond that,
  /// consequently this permission skips authorization altogether.
  /// This is typically used to inject session/security scope and to assert user identity validity without checking
  /// for any specific permissions/ACLs
  /// </summary>
  public sealed class AuthenticatedUserPermission : TypedPermission
  {
    public AuthenticatedUserPermission() : base(0){ }

    public override bool Check(ISecurityManager secman, ISession sessionInstance = null)
    {
      var session = sessionInstance ?? ExecutionContext.Session ?? NOPSession.Instance;
      var user = session.User;

      return user.Status > UserStatus.Invalid;
    }

    public override Task<bool> CheckAsync(ISecurityManager secman, ISession sessionInstance = null)
    {
      var session = sessionInstance ?? ExecutionContext.Session ?? NOPSession.Instance;
      var user = session.User;

      return Task.FromResult(user.Status > UserStatus.Invalid);
    }
  }

}
