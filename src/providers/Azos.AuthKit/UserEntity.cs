﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azos.Conf;
using Azos.Data;
using Azos.Data.Business;
using Azos.Security;
using Azos.Serialization.Bix;
using Azos.Time;

namespace Azos.AuthKit
{
  /// <summary>
  /// Provides persisted model for user account data
  /// </summary>
  [Bix("ac75cd1f-75d4-471b-879d-17235c1241e5")]
  [Schema(Description = "Provides persisted model for user account data")]
  [UniqueSequence(Constraints.ID_NS_AUTHKIT, Constraints.ID_SEQ_USER)]
  public sealed class UserEntity : EntityBase<IIdpUserCoreLogic, ChangeResult>
  {
    public override EntityId Id => new EntityId(Constraints.SYS_AUTHKIT,
                                                Constraints.ETP_USER,
                                                Constraints.SCH_GDID, Gdid.ToString());
    /// <summary>
    /// Name/(Screen Name)/Uri//Title of user account unique per realm
    /// </summary>
    [Field(required: true,
           minLength: Constraints.USER_NAME_MIN_LEN,
           maxLength: Constraints.USER_NAME_MAX_LEN,
           Description = "Name/(Screen Name)/Uri//Title of user account unique per realm")]
    public string Name { get; set; }

    /// <summary>
    /// User access level
    /// </summary>
    [Field(required: true, Description = "User access level")]
    public UserStatus Level   { get; set; }

    /// <summary>
    /// User description
    /// </summary>
    [Field(required: true,
           minLength: Constraints.USER_DESCR_MIN_LEN,
           maxLength: Constraints.USER_DESCR_MAX_LEN,
           Description = "User description")]
    public string Description { get; set; }

    /// <summary>
    /// User account start/end date UTC time range
    /// </summary>
    [Field(required: true, description: "User account start/end date UTC time range")]
    public DateRange? ValidSpanUtc {  get; set; }

    /// <summary>
    /// User organization unit
    /// </summary>
    [Field(description: @"User organization unit")]
    public EntityId? OrgUnit { get; set; }

    /// <summary>
    /// Properties such as tree connections (e.g. roles) and claims
    /// </summary>
    [Field(required: true,
           minLength: Constraints.PROPS_MIN_LEN,
           maxLength: Constraints.PROPS_MAX_LEN,
           Description = "Properties such as tree connections (e.g. roles) and claims")]
    public ConfigVector Props  { get; set; }

    /// <summary>
    /// User-specific Rights override or null for default rights
    /// </summary>
    [Field(minLength: Constraints.RIGHTS_MIN_LEN,
           maxLength: Constraints.RIGHTS_MAX_LEN,
           Description = "User-specific Rights override or null for default rights")]
    public ConfigVector Rights { get; set; }

    /// <summary>
    /// Free form text notes associated with the account
    /// </summary>
    [Field(maxLength:Constraints.NOTE_MAX_LEN,
           Description = "Free form text notes associated with the account")]
    public string Note { get; set; }

    /// <inheritdoc/>
    public override ValidState Validate(ValidState state, string scope = null)
    {
      state = base.Validate(state, scope);

      if (state.ShouldContinue)
      {
        if (ValidSpanUtc.HasValue && (!ValidSpanUtc.Value.Start.HasValue || !ValidSpanUtc.Value.End.HasValue))
        {
          state = new ValidState(state, new FieldValidationException(nameof(ValidSpanUtc), "Either Start/End unassigned"));
        }

        if (Props != null)
        {
          IConfigSectionNode nProps = null;
          try{ nProps = Props.Node; } catch{ /* Double guard is needed in case of batch validation mode */ }
          if (nProps != null && nProps.Exists && !nProps.IsSameName(Constraints.CONFIG_PROP_ROOT_SECTION))
          {
            state = new ValidState(state, new FieldValidationException(nameof(Props), $"UserEntity.Props root node should be called `{Constraints.CONFIG_PROP_ROOT_SECTION}`"));
          }
        }

        if (Rights != null)
        {
          IConfigSectionNode nRights = null;
          try { nRights = Rights.Node; } catch { /* Double guard is needed in case of batch validation mode */ }
          if (nRights != null && nRights.Exists && !nRights.IsSameName(Azos.Security.Rights.CONFIG_ROOT_SECTION))
          {
            state = new ValidState(state, new FieldValidationException(nameof(Rights), $"UserEntity.Rights root node should be called `{Security.Rights.CONFIG_ROOT_SECTION}`"));
          }
        }
      }

      return state;
    }

    /// <inheritdoc/>
    protected override async Task<ValidState> DoAfterValidateOnSaveAsync(ValidState state)
    {
      var result = await base.DoAfterValidateOnSaveAsync(state).ConfigureAwait(false);
      if (!result.ShouldContinue) return result;

      state = await m_SaveLogic.ValidateUserAsync(this, state).ConfigureAwait(false);

      return result;
    }

    /// <inheritdoc/>
    protected override async Task<ChangeResult> SaveBody(IIdpUserCoreLogic logic)
     => await logic.SaveUserAsync(this).ConfigureAwait(false);
  }
}
