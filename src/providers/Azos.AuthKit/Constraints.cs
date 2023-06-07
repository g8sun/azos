﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using Azos.Data;
using Azos.Security;

namespace Azos.AuthKit
{
  /// <summary>
  /// Provides global AuthKit constraints and definitions
  /// </summary>
  public static class Constraints
  {
    /// <summary>
    /// Reserving X GDIDS in authority A:  0:A:0..X
    /// </summary>
    public const int GDID_RESERVED_ID_AUTHORITY = 0;

    /// <summary>
    /// Reserving X GDIDS in authority A:  0:A:0..X
    /// </summary>
    public const int GDID_RESERVED_ID_COUNT = 32;


    /// <summary>
    /// Gdid generation namespace
    /// </summary>
    public const string ID_NS_AUTHKIT = "sky.akit";
    public const string ID_SEQ_USER  = "user";
    public const string ID_SEQ_LOGIN = "login";

    // Config Tree related constants
    public static readonly Atom TREE_AUTHKIT = Atom.Encode("sky-akit");
    public const string TREE_AUTHKIT_SYS_PATH = "/sys";

    //EntityIds
    public static readonly Atom SYS_AUTHKIT = Atom.Encode("sky-auth");
    public static readonly Atom SCH_GDID    = Atom.Encode("gdid");
    public static readonly Atom SCH_ID      = Atom.Encode("id");
    public static readonly Atom ETP_USER    = Atom.Encode("user");
    public static readonly Atom ETP_LOGIN   = Atom.Encode("login");
    public static readonly Atom ETP_ORGUNIT = Atom.Encode("orgu");


    //System provider Login types
    public static readonly Atom LTP_SYS_EMAIL = Atom.Encode("email");//detected by searching for @
    public static readonly Atom LTP_SYS_PHONE = Atom.Encode("phone");//detected by searching for digits
    public static readonly Atom LTP_SYS_ID    = Atom.Encode("id");//if not email or phone
    public static readonly Atom LTP_SYS_SCREEN_NAME = Atom.Encode("screenm");//user screen name akin to Id, screen names are more mnemonic than Ids
    public static readonly Atom LTP_SYS_URI   = Atom.Encode("uri");//used by Login by URI

    /// <summary>
    /// AuthKit event namespace
    /// </summary>
    public const string EVT_NS_AUTHKIT = "aukit";

    /// <summary>
    /// Name of the queue for login-related events
    /// </summary>
    public const string EVT_QUEUE_LOGIN = "login";


    /// <summary>
    /// The name of root node of props vector
    /// </summary>
    public const string CONFIG_PROP_ROOT_SECTION = "prop";

    public const string CONFIG_CLAIMS_SECTION = "claims";// prop{  claims{ pub{...} } }
    public const string CONFIG_PUBLIC_SECTION = "pub";

    public const string CONFIG_G_USER_ATTR = "g-user";
    public const string CONFIG_ROLE_ATTR = "role";
    public const string CONFIG_ORG_UNIT_ATTR = "org-unit";

    public const int ENTITY_ID_MAX_LEN = 256;

    public const int USER_NAME_MIN_LEN = 3;
    public const int USER_NAME_MAX_LEN = 64;

    public const int USER_DESCR_MIN_LEN = 1;
    public const int USER_DESCR_MAX_LEN = 128;

    public const int LOGIN_ID_MAX_LEN = 700;

    public const int LOGIN_PWD_MIN_LEN = 2;// { }
    public const int LOGIN_PWD_MAX_LEN = 2048;// { }

    public const int PROVIDER_DATA_MAX_LEN = 128 * 1024;

    public const int RIGHTS_MIN_LEN = 6; // {r:{}}
    public const int RIGHTS_MAX_LEN = 256 * 1024;

    public const int PROPS_MIN_LEN = 6; // {r:{}}
    public const int PROPS_MAX_LEN = 128 * 1024;

    public const int DESCRIPTION_MAX_LEN = 128;
    public const int NOTE_SHORT_MAX_LEN = 256;
    public const int NOTE_MAX_LEN = 4 * 1024;

    public const int CALLER_ADDR_MAX_LEN = 64;
    public const int CALLER_AGENT_MAX_LEN = 256;

    /// <summary>
    /// Returns entity GDID if the supplied EntityId points to a valid entity type, using GDID schema
    /// </summary>
    public static GDID AsValidLockEntityId(EntityId id)
    {
      if (
           (!id.IsAssigned) ||
           (id.System != SYS_AUTHKIT) ||
           (!id.Schema.IsZero && id.Schema != SCH_GDID)
         ) return GDID.ZERO;

      if (id.Type != ETP_USER &&
          id.Type != ETP_LOGIN) return GDID.ZERO;

      return id.Address.AsGDID(GDID.ZERO);
    }

    public static UserStatus? MapUserStatus(string v)
    {
      if (v.IsNullOrWhiteSpace()) return null;

      if (v == "u" || v == "U") return UserStatus.User;
      if (v == "a" || v == "A") return UserStatus.Admin;
      if (v == "s" || v == "S") return UserStatus.System;
      return UserStatus.Invalid;
    }

    public static string MapUserStatus(UserStatus? v)
    {
      if (!v.HasValue) return null;

      if (v == UserStatus.User) return "U";
      if (v == UserStatus.Admin) return "A";
      if (v == UserStatus.System) return "S";
      return "I";
    }

  }
}
