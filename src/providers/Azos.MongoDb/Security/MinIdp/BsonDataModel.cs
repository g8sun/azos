﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using Azos.Serialization.BSON;
using Azos.Data.Access.MongoDb.Connector;

namespace Azos.Security.MinIdp
{
  /// <summary>
  /// Provides helpers to read MinIdpUserData from MongoDB BSON
  /// </summary>
  internal static class BsonDataModel
  {
    public const int FETCH_LIMIT_LIST = 1024;
    public const string _ID = Query._ID;

    public const int MAX_ID_LEN = 64;
    public const int MAX_NOTE_LEN = 4 * 1024;
    public const int MAX_PROPS_LEN = 2 * 1024;
    public const int MAX_DESCR_LEN = 256;
    public const int MAX_NAME_LEN  = 64;
    public const int MAX_PWD_LEN = 4 * 1024;

    public const string COLLECTION_LOGIN = "lin";
    public const string COLLECTION_USER = "usr";
    public const string COLLECTION_ROLE = "rol";

    public const string FLD_SYSID = "sysid";
    public const string FLD_STATUS = "status";
    public const string FLD_RIGHTS = "rights";
    public const string FLD_PROPS = "props";
    public const string FLD_PASSWORD = "pwd";

    public const string FLD_CREATEUTC = "cutc";
    public const string FLD_STARTUTC = "sutc";
    public const string FLD_ENDUTC = "eutc";

    public const string FLD_ROLE = "role";
    public const string FLD_NAME = "name";
    public const string FLD_DESCRIPTION = "descr";
    public const string FLD_NOTE = "note";


    public static string GetCollectionName(Atom realm, string name) => $"midp_{name}_{realm.Value}";


    public static void ReadLogin(BSONDocument bson, MinIdpUserData data)
    {
      if (bson[_ID] is BSONStringElement id)
      {
        data.LoginId = id.Value;
        data.ScreenName = id.Value;
      }

      if (bson[FLD_SYSID] is BSONInt64Element sysid) data.SysId = ((ulong)sysid.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
      if (bson[FLD_PASSWORD] is BSONStringElement pwd) data.LoginPassword = pwd.Value;

      if (bson[FLD_STARTUTC] is BSONDateTimeElement sdt) data.LoginStartUtc = sdt.Value;
      if (bson[FLD_ENDUTC] is BSONDateTimeElement edt) data.LoginEndUtc = edt.Value;
    }

    public static void ReadUser(BSONDocument bson, MinIdpUserData data)
    {
      if (bson[_ID] is BSONInt64Element sysid) data.SysId = ((ulong)sysid.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
      if (bson[FLD_STATUS] is BSONInt32Element status) data.Status = (UserStatus)status.Value;
      if (bson[FLD_ROLE] is BSONStringElement role) data.Role = role.Value;

      if (bson[FLD_CREATEUTC] is BSONDateTimeElement cdt) data.CreateUtc = cdt.Value;
      if (bson[FLD_STARTUTC] is BSONDateTimeElement sdt) data.StartUtc = sdt.Value;
      if (bson[FLD_ENDUTC] is BSONDateTimeElement edt) data.EndUtc = edt.Value;

      if (bson[FLD_NAME] is BSONStringElement name) data.Name = name.Value;
      if (bson[FLD_DESCRIPTION] is BSONStringElement descr) data.Description = descr.Value;
      if (bson[FLD_NOTE] is BSONStringElement note) data.Note = note.Value;

      if (bson[FLD_PROPS] is BSONStringElement props)
      {
        data.Props = new Azos.Data.ConfigVector(props.Value);
      }
    }

    public static void ReadRole(BSONDocument bson, MinIdpUserData data)
    {
      if (bson[_ID] is BSONStringElement role) data.Role = role.Value;
      if (bson[FLD_RIGHTS] is BSONStringElement rights) data.Rights = rights.Value;
    }
  }
}
