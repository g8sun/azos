﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using Azos.Apps;
using Azos.Conf;

namespace Azos.Security.Services
{
  /// <summary>
  /// Describes a module which manages IAM/IDP services, such as token rings and underlying data stores.
  /// OAuth is a module because not all applications need it, consequently it is not a hard-coded dependency, rather
  /// a mix-in module mounted by app chassis when needed
  /// </summary>
  public interface IOAuthModule : IModule
  {
    /// <summary>
    /// Variable name for gating bad OAuth request (such as bad client ID, invalid redirect Uri or backchannel IP, etc.)
    /// </summary>
    string GateVarErrors { get; set; }

    /// <summary>
    /// Variable name for gating invalid user credentials
    /// </summary>
    string GateVarInvalidUser { get; set; }

    /// <summary>
    /// Imposes a maximum age of roundtrip state which is generated on flow start and checked at user credentials POST.
    /// Value is in seconds
    /// </summary>
    int MaxAuthorizeRoundtripAgeSec { get; set; }

    /// <summary>
    /// Imposes a maximum lifespan for access tokens. If the value is less or equal to zero, then access token defaults are used (e.g. 10 hrs)
    /// </summary>
    int AccessTokenLifespanSec { get; set; }

    /// <summary>
    /// Imposes a maximum lifespan for refresh tokens. If the value is less or equal to zero, the refresh tokens are NOT
    /// being issued
    /// </summary>
    int RefreshTokenLifespanSec { get; set; }

    /// <summary>
    /// Turns-on the Single Sign-on (SSO) session persistence when set. SSO "remembers" the user identity
    /// between authorization calls. This way, multiple OAuth clients do not need to re-enter their
    /// credentials. Depending on implementation this name may be used as a cookie name for storing session identifier.
    /// If this setting is not set, then all SSO-related activities are inactivated.
    /// </summary>
    string SsoSessionName { get; set; }

    /// <summary>
    /// Returns security manager responsible for authentication and authorization of clients(applications) which
    /// request access to the system on behalf of the user. This security manager is expected to understand the
    /// `EntityUriCredentials` used for pseudo-authentication/lookup and `IDPasswordCredentials`.
    /// The returned `User` object represents a requesting client party/application along with its rights, such as:
    /// allowed redirect URIs, back-channel IPs, ability to execute "implicit" OAuth flows etc.
    /// </summary>
    ISecurityManager ClientSecurity { get; }

    /// <summary>
    /// Returns a token ring used to store tokens/temp keys issued at different stages of various
    /// flows such as OAuth token grant, refresh tokens etc.
    /// </summary>
    Tokens.ITokenRing TokenRing { get; }


    /// <summary>
    /// Returns true if the specified scope specification is supported. The string may contain multiple
    /// scopes delimited by spaces or commas
    /// </summary>
    bool CheckScope(string scope);

    /// <summary>
    /// Provides config options related to OAuth.
    /// The node is copied from config 'options' sub section.
    /// For example, this section is used to get OAuth controller config options,
    /// such as e.g. SSO cookie HTTPS policy, expiration etc.
    /// </summary>
    IConfigSectionNode Options { get; }
  }

  /// <summary>
  /// Denotes an entity implementing IOAuthModule
  /// </summary>
  public interface IOAuthModuleImplementation : IOAuthModule, IModuleImplementation
  {
  }

}
