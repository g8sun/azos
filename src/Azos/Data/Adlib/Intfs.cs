﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Azos.Data.Business;

namespace Azos.Data.Adlib
{
  /// <summary>
  /// Outlines contract for working with [A]morphous [D]ata [Lib]rary database server(s)
  /// </summary>
  public interface IAdlib
  {
    /// <summary>
    /// Returns a list of all known data spaces on the server
    /// </summary>
    Task<IEnumerable<Atom>> GetSpaceNamesAsync();

    /// <summary>
    /// Returns a list of all known collections within the space on the server.
    /// Note: these are server collections that have any data in them
    /// </summary>
    Task<IEnumerable<Atom>> GetCollectionNamesAsync(Atom space);

    /// <summary>
    /// Returns a list of filtered items out of the specified server collection
    /// </summary>
    Task<IEnumerable<Item>> GetListAsync(ItemFilter filter);

    /// <summary>
    /// Saves an Item into the specified collection
    /// </summary>
    Task<ChangeResult> SaveAsync(Item item);

    /// <summary>
    /// Deletes an item with the specified GDID primary key  from the specified collection
    /// </summary>
    Task<ChangeResult> DeleteAsync(EntityId id, string shardTopic = null);

    /// <summary>
    /// Drops a collection from all known shards/hosts
    /// </summary>
    Task<ChangeResult> DropCollectionAsync(Atom space, Atom collection);
  }

  public interface IAdlibLogic : IAdlib, IBusinessLogic
  {
  }


}
