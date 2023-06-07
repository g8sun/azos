﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Azos.Conf;
using Azos.Data;
using Azos.Data.Business;
using Azos.Scripting.Dsl;
using Azos.Serialization.JSON;

namespace Azos.AuthKit.Dsl
{
  /// <summary>
  /// Saves the whole user entity graph including logins
  /// </summary>
  public class LoadUsers : Step
  {
    public class UserData : FragmentModel
    {
      [Field] public UserEntity User { get; set; }
      [Field] public List<LoginEntity> Logins { get; set;}
    }


    public LoadUsers(StepRunner runner, IConfigSectionNode cfg, int order) : base(runner, cfg, order) { }

    [Config] public string Data { get; set; }

    protected override async Task<string> DoRunAsync(JsonDataMap state)
    {
      var logic = LoadModule.Get<IIdpUserAdminLogic>();

      var manyUsersData = GetUserData(state);

      var results = new List<JsonDataMap>();

      foreach(var oneUser in manyUsersData)
      {
        var result = new JsonDataMap();
        results.Add(result);
        var loginResults = new List<ChangeResult>();
        result["logins"] = loginResults;

        oneUser.User.NonNull("[{User....},{...},...]");
        oneUser.Logins.NonNull("[{Logins....},{...},...]");

        oneUser.User.FormMode = oneUser.User.Gdid.IsZero ? FormMode.Insert : FormMode.Update;
        var userChange = await logic.SaveUserAsync(oneUser.User).ConfigureAwait(false);
        result["user"] = userChange;
        var uci =  EntityChangeInfo.FromChangeAs<IdpEntityChangeInfo>(userChange);


        foreach (var login in oneUser.Logins)
        {
          login.FormMode = login.Gdid.IsZero ? FormMode.Insert : FormMode.Update;
          login.G_User = uci.Id_Gdid;
          loginResults.Add(await logic.SaveLoginAsync(login).ConfigureAwait(false));
        }
      }

      Runner.SetResult(results);

      return null;
    }

    protected virtual IEnumerable<UserData> GetUserData(JsonDataMap state)
    {
      var json = Eval(Data, state);

      if (json.IsNullOrWhiteSpace()) return Enumerable.Empty<UserData>();

      var dobj = JsonReader.DeserializeDataObject(json);

      if (dobj == null) return Enumerable.Empty<UserData>();

      if (dobj is JsonDataMap jmap)
      {
        return JsonReader.ToDoc<UserData>(jmap).ToEnumerable();
      }
      else if (dobj is JsonDataArray jarr)
      {
        return jarr.OfType<JsonDataMap>()
                   .Select(one => JsonReader.ToDoc<UserData>(one));
      }
      else throw new Scripting.ScriptingException($"{nameof(LoadUsers)} does not support data of type: " + dobj.GetType().DisplayNameWithExpandedGenericArgs());
    }
  }

}
