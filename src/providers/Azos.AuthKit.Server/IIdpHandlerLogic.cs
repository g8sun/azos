﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azos.Collections;
using Azos.Conf;
using Azos.Data;
using Azos.Data.Business;
using Azos.Security;
using Azos.Security.MinIdp;
using Azos.Serialization.JSON;

namespace Azos.AuthKit.Server
{
  /// <summary>
  /// Handles the core functionality of identity provider implementation
  /// </summary>
  public interface IIdpHandlerLogic : IBusinessLogic
  {
    /// <summary>
    /// Provides the identity provider config forest. The system uses this value
    /// as the system segment of EntityId for forest navigation. The requestor
    /// realm is then used as Tree ID within this forest
    /// </summary>
    Atom IdpConfigForestId { get; }

    /// <summary>
    /// Provides the system global configuration node value from config forest
    /// </summary>
    IConfigSectionNode SysConfigNode { get; }

    /// <summary>
    /// Forms a full EntityId for the tree path of the config in the forest
    /// </summary>
    EntityId GetIdpConfigTreeNodePath(Atom realm, string address);

    /// <summary>
    /// Registry of login providers
    /// </summary>
    IRegistry<LoginProvider> Providers {  get; }


    /// <summary>
    /// Used if the login ID does not specify the provider
    /// </summary>
    Atom DefaultLoginProvider {  get; }

    /// <summary>
    /// Parses the supplied login string expressed in EntityId format.
    /// The string has to be formatted as EntityId or plain string which then assumes defaults.
    /// The EntityId.System is Provider.Name, and EntityId.Type is login type.
    /// Throws `DataValidationException/400` on wrong ID
    /// </summary>
    /// <example>
    /// "provider::id", "type@provider::id", "[default-type]@[default-provider]::id"
    /// </example>
    (LoginProvider provider, EntityId id) ParseId(string id);

    /// <summary>
    /// Parses the supplied uri expressed in EntityId format.
    /// The string has to be formatted as EntityId or plain string which then assumes defaults.
    /// The EntityId.System is Provider.Name, and EntityId.Type is login type.
    /// Throws `DataValidationException/400` on wrong ID
    /// </summary>
    /// <example>
    /// "uri@provider::uri_address", "uri@[default-provider]::uri_address"
    /// </example>
    (LoginProvider provider, EntityId id) ParseUri(string uri);

    /// <summary>
    /// The name of cryptographic message protection algorithm used to protect raw token data.
    /// Typically this would be configured to use <see cref="Azos.Security.HMACAESCryptoMessageAlgorithm"/>
    /// </summary>
    string SysTokenCryptoAlgorithmName { get; }

    /// <summary>
    /// How long system token lasts for. However, some flows such as OAuth may request
    /// tokens with longer life (by passing a corresponding authentication request intent)
    /// </summary>
    double SysTokenLifespanHours       { get; }

    /// <summary>
    /// Factory pattern:
    /// Create an instance of user authentication DTO object used during login.
    /// The underlying store/handler implementation use this object as DTO passing relevant fields between methods
    /// of handler and query during user authentication. The object is being made in the context of the implicit ambient realm.
    /// </summary>
    AuthContext MakeNewUserAuthenticationContext(AuthenticationRequestContext ctx);

    /// <summary>
    /// Makes new SYSTEMAUTH token content for the `AuthContext`-derived concretion
    /// </summary>
    void MakeSystemTokenData(AuthContext context);

    /// <summary>
    /// Tries to decode the raw token data, returning null if it is compromised/tampered-with or simply malformed.
    /// If OK, returns the login provider used and populates relevant fields in context
    /// </summary>
    LoginProvider TryDecodeSystemTokenData(string token, AuthContext context);

    /// <summary>
    /// Calculates effective Rights, and Props applying policies as necessary as defined
    /// by the specific implementation of `IIdpHandlerLogic`.
    /// The data is supplied in the `AuthContext`-derived concretion
    /// </summary>
    Task ApplyEffectivePoliciesAsync(AuthContext context);
  }
}
