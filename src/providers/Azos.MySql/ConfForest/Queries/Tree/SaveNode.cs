﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Threading.Tasks;

using Azos;
using Azos.Conf.Forest;
using Azos.Data;
using Azos.Data.Access;
using Azos.Data.Access.MySql;
using Azos.Data.Business;
using Azos.Platform;

using MySqlConnector;

namespace Azos.MySql.ConfForest.Queries.Tree
{
  public sealed class SaveNode : MySqlCrudQueryHandler<TreeNode>
  {
    public SaveNode(MySqlCrudDataStoreBase store, string name) : base(store, name) { }

    protected override async Task<Doc> DoExecuteProcedureParameterizedQueryAsync(MySqlCrudQueryExecutionContext ctx, Query query, TreeNode node)
    {
      var result = new TreeNodeChangeInfo
      {
        Id = node.Id,
        Version = ctx.MakeVersionInfo(Constraints.ID_NS_CONFIG_FOREST_PREFIX + node.Forest.Value,
                                      node.Tree.Value + Constraints.ID_SEQ_TREE_NODE_GVERSION_SUFFIX,
                                      node.FormMode)
      };

      if (node.FormMode == FormMode.Insert)
      {
        await ctx.ExecuteCompoundCommand(CommandTimeoutSec, System.Data.IsolationLevel.ReadCommitted,
          cmd => insertNode(cmd, ctx, node),
          cmd => insertNodeLog(true, cmd, result.Version, node)
        ).ConfigureAwait(false);
      }
      else
      {
        var affected = await ctx.ExecuteCompoundCommand(CommandTimeoutSec, System.Data.IsolationLevel.ReadCommitted,
          cmd => insertNodeLog(false, cmd, result.Version, node)
        ).ConfigureAwait(false);

        if (affected < 1)
        {
          throw new DocValidationException(nameof(TreeNode), "Could not find TreeNode(Gdid=`{0}`) to update".Args(node.Gdid))
          {
            HttpStatusCode = 404,
            HttpStatusDescription = "Tree node not found"
          };
        }
      }

      return result;
    }

    private void insertNode(MySqlCommand cmd, MySqlCrudQueryExecutionContext ctx, TreeNode node)
    {
      cmd.CommandText = GetType().GetText("SaveNode.sql");
      cmd.Parameters.AddWithValue("gdid", node.Gdid);
      cmd.Parameters.AddWithValue("create_utc", ctx.GetUtcNow());
    }

    private void insertNodeLog(bool isInsert, MySqlCommand cmd, VersionInfo version, TreeNode node)
    {
      cmd.CommandText = GetType().GetText(isInsert ? "SaveNodeLogInsert.sql" : "SaveNodeLogUpdate.sql");
      cmd.MapVersionToSqlParameters(version);

      cmd.Parameters.AddWithValue("gnode", node.Gdid);
      cmd.Parameters.AddWithValue("gparent", node.G_Parent.IsZero ? Constraints.G_VERY_ROOT_NODE : node.G_Parent);//GDID.ZERO is NEVER inserted
      cmd.Parameters.AddWithValue("psegment", node.PathSegment);
      cmd.Parameters.AddWithValue("start_utc", node.StartUtc.Value);
      cmd.Parameters.AddWithValue("properties", node.Properties.Content);
      cmd.Parameters.AddWithValue("config", node.Config.Content);
    }

  }
}
