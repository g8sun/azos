﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using Azos.Conf;
using Azos.Data;

namespace Azos.Sky.FileGateway.Server
{
  /// <summary>
  /// Implements Volume based on a local file system access API
  /// </summary>
  public sealed class LocalFsVolume : Volume
  {
    internal LocalFsVolume(GatewaySystem director, IConfigSectionNode conf) : base(director, conf)
    {
      m_MountPath.NonBlank(nameof(MountPath));
      Directory.Exists(m_MountPath).IsTrue("Existing `MountPath`");
    }

    [Config] private string m_MountPath;

    /// <summary>
    /// Root mount path as of which the external pass is based
    /// </summary>
    public string MountPath => m_MountPath;


    private string getPhysicalPath(string volumePath)
    {
      volumePath.NonBlankMax(Constraints.MAX_PATH_TOTAL_LEN, nameof(volumePath), putExternalDetails: true);
      volumePath = volumePath.Trim();
      (volumePath.IndexOf(':') < 0).IsTrue("No ':'", putExternalDetails: true);
      (volumePath.IndexOf("..") < 0).IsTrue("No '..'", putExternalDetails: true);
      (!volumePath.Contains(@"\\") && !volumePath.Contains(@"//")).IsTrue("No UNC", putExternalDetails: true);

      var fullPath = Path.Join(m_MountPath, volumePath);
      return fullPath;
    }

    private ItemInfo getItemInfo(string fullLocalPath)
    {
      var result = new ItemInfo();
      try
      {
        var volumePath = Path.GetRelativePath(MountPath, fullLocalPath);
        result.Path = new EntityId(ComponentDirector.Name, this.Name, Atom.ZERO, volumePath);

        if (File.GetAttributes(fullLocalPath).HasFlag(FileAttributes.Directory))
        {
          var di = new DirectoryInfo(fullLocalPath);
          result.Type = ItemType.Directory;
          result.CreateUtc = di.CreationTimeUtc;
          result.LastChangeUtc = di.LastWriteTimeUtc;
          result.Size = 0;
          return result;
        }

        //File
        result.Type = ItemType.File;
        var fi = new FileInfo(fullLocalPath);
        result.CreateUtc =  fi.CreationTimeUtc;
        result.LastChangeUtc = fi.LastWriteTimeUtc;
        result.Size = fi.Length;
        return result;
      }
      catch(Exception error)
      {
        var got = new FileGatewayException($"getItemInfo(`{fullLocalPath}`): {error.Message}", error);
        WriteLogFromHere(Azos.Log.MessageType.Error, got.ToMessageWithType(), got);
        throw got;
      }
    }


    public override Task<IEnumerable<ItemInfo>> GetItemListAsync(string volumePath, bool recurse)
    {
      try
      {
        var path = getPhysicalPath(volumePath);
        if (!Directory.Exists(path)) throw Azos.Web.HTTPStatusException.NotFound_404($"Dir: `{volumePath}`");
        var all = Directory.GetFileSystemEntries(path,  "*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        IEnumerable<ItemInfo> result = all.Select(one => getItemInfo(one)).ToArray();
        return Task.FromResult(result);
      }
      catch (Exception error)
      {
        var got = new FileGatewayException($"GetItemListAsync(`{volumePath}`): {error.Message}", error);
        WriteLogFromHere(Azos.Log.MessageType.Error, got.ToMessageWithType(), got);
        throw got;
      }
    }

    public override Task<ItemInfo> GetItemInfoAsync(string volumePath)
    {
      try
      {
        var path = getPhysicalPath(volumePath);
        if (!File.Exists(path) && !Directory.Exists(path)) throw Azos.Web.HTTPStatusException.NotFound_404($"Item: `{volumePath}`");
        var result = getItemInfo(path);
        return Task.FromResult(result);
      }
      catch (Exception error)
      {
        var got = new FileGatewayException($"GetItemInfoAsync(`{volumePath}`): {error.Message}", error);
        WriteLogFromHere(Azos.Log.MessageType.Error, got.ToMessageWithType(), got);
        throw got;
      }
    }

    public override Task<ItemInfo> CreateDirectoryAsync(string volumePath)
    {
      try
      {
        var path = getPhysicalPath(volumePath);
        Directory.CreateDirectory(path);
        var result = getItemInfo(path);
        return Task.FromResult(result);
      }
      catch (Exception error)
      {
        var got = new FileGatewayException($"CreateDirectoryAsync(`{volumePath}`): {error.Message}", error);
        WriteLogFromHere(Azos.Log.MessageType.Error, got.ToMessageWithType(), got);
        throw got;
      }
    }

    public override async Task<ItemInfo> CreateFileAsync(string volumePath, CreateMode mode, long offset, byte[] content)
    {
      try
      {
        var path = getPhysicalPath(volumePath);

        var fmode = mode == CreateMode.Create  ? FileMode.CreateNew :
                    mode == CreateMode.Replace ? FileMode.Create    :
                                                 FileMode.OpenOrCreate;
        using(var fs = new FileStream(path, fmode, FileAccess.Write, FileShare.None))
        {
          fs.Position = offset;
          if (content != null)
          {
            await fs.WriteAsync(content, 0, content.Length).ConfigureAwait(false);
          }
        }

        return getItemInfo(path);
      }
      catch(Exception error)
      {
        var got = new FileGatewayException($"CreateFileAsync(`{volumePath}`, {mode}, {offset}, byte[{content?.Length ?? -1}]): {error.Message}", error);
        WriteLogFromHere(Azos.Log.MessageType.Error, got.ToMessageWithType(), got);
        throw got;
      }
    }

    public override Task<bool> DeleteItemAsync(string volumePath)
    {
      try
      {
        var path = getPhysicalPath(volumePath);

        if (Directory.Exists(path))
        {
          Directory.Delete(path, true);
        }
        else if (File.Exists(path))
        {
          File.Delete(path);
        }
        else
          return Task.FromResult(false);
      }
      catch (Exception error)
      {
        var got = new FileGatewayException($"DeleteItemAsync(`{volumePath}`): {error.Message}", error);
        WriteLogFromHere(Azos.Log.MessageType.Error, got.ToMessageWithType(), got);
        throw got;
      }

      return Task.FromResult(true);
    }

    public override async Task<(byte[] data, bool eof)> DownloadFileChunkAsync(string volumePath, long offset, int size)
    {
      try
      {
        (size < Constraints.MAX_FILE_CHUNK_SIZE).IsTrue($"size < {Constraints.MAX_FILE_CHUNK_SIZE}", putExternalDetails: true);
        (offset >= 0).IsTrue("offset >= 0", putExternalDetails: true);

        var path = getPhysicalPath(volumePath);

        if (!File.Exists(path)) throw Azos.Web.HTTPStatusException.NotFound_404($"File: `{volumePath}`");

        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
          if (offset >= fs.Length) return (null, true);

          fs.Position = offset;
          var buff = new byte[size];
          var eof = false;
          var total = 0;
          while(total < size)
          {
            var got = await fs.ReadAsync(buff, total, buff.Length - total).ConfigureAwait(false);
            if (got == 0)
            {
              eof = true;
              break;
            }
            total += got;
          }

          var result = new byte[total];
          Array.Copy(buff, result, total);
          return (result, eof);
        }
      }
      catch (Exception error)
      {
        var got = new FileGatewayException($"DownloadFileChunkAsync(`{volumePath}`, {offset}, {size}): {error.Message}", error);
        WriteLogFromHere(Azos.Log.MessageType.Error, got.ToMessageWithType(), got);
        throw got;
      }
    }

    public override async Task<ItemInfo> UploadFileChunkAsync(string volumePath, long offset, byte[] content)
    {
      try
      {
        content.NonNull(nameof(content), putExternalDetails: true);
        (content.Length < Constraints.MAX_FILE_CHUNK_SIZE).IsTrue($"size < {Constraints.MAX_FILE_CHUNK_SIZE}", putExternalDetails: true);
        (offset >= 0).IsTrue("offset >= 0", putExternalDetails: true);

        var path = getPhysicalPath(volumePath);

        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None))
        {
          fs.Position = offset;
          await fs.WriteAsync(content, 0, content.Length).ConfigureAwait(false);
        }

        return getItemInfo(path);
      }
      catch (Exception error)
      {
        var got = new FileGatewayException($"UploadFileChunkAsync(`{volumePath}`, {offset}, byte[{content.Length}]): {error.Message}", error);
        WriteLogFromHere(Azos.Log.MessageType.Error, got.ToMessageWithType(), got);
        throw got;
      }
    }

    public override Task<bool> RenameItemAsync(string volumePath, string newVolumePath)
    {
      var oldPath = getPhysicalPath(volumePath);
      var newPath = getPhysicalPath(newVolumePath);

      FileAttributes fatr;
      try
      {
        fatr = File.GetAttributes(oldPath);
      }
      catch(FileNotFoundException)
      {
        return Task.FromResult(false);
      }

      try
      {
        if (fatr.HasFlag(FileAttributes.Directory))
        {
          var di = new DirectoryInfo(oldPath);
          di.MoveTo(newPath);
        }
        else
        {
          var fi = new FileInfo(oldPath);
          fi.MoveTo(newPath);
        }
      }
      catch (Exception error)
      {
        var got = new FileGatewayException($"RenameItemAsync(`{volumePath}`, `{newVolumePath}`): {error.Message}", error);
        WriteLogFromHere(Azos.Log.MessageType.Error, got.ToMessageWithType(), got);
        throw got;
      }

      return Task.FromResult(true);
    }
  }
}
