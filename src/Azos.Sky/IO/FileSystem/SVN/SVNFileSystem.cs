/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;

using Azos.Apps;
using Azos.Conf;
using Azos.Web;

namespace Azos.IO.FileSystem.SVN
{
  /// <summary>
  /// Implements FileSystem for SVN.
  /// WebDAV protocol is used so reading methods only are implemented
  /// </summary>
  public class SVNFileSystem : FileSystem
  {
    #region Inner Types

      public class SVNFSH : IFileSystemHandle
      {
        public SVNFSH(WebDAV.Item item)
        {
          Item = item;
        }

        public readonly WebDAV.Item Item;
      }

    #endregion

    #region .ctor

    public SVNFileSystem(IApplication app) : base(app)
    {
      app.RequireInitilizedServicePointManager();
    }

    public SVNFileSystem(IApplicationComponent director) : base(director)
    {
      App.RequireInitilizedServicePointManager();
    }

    #endregion

    #region Public

      public override string ComponentCommonName { get { return "fssvn"; }}

      public override IFileSystemCapabilities GeneralCapabilities
      {
        get { return SVNFileSystemCapabilities.Instance; }
      }

      public override IFileSystemCapabilities InstanceCapabilities
      {
        get { return SVNFileSystemCapabilities.Instance; }
      }

      public SVNFileSystemSession StartSession(SVNFileSystemSessionConnectParams cParams)
      {
        var svnCParams = cParams ?? (DefaultSessionConnectParams as SVNFileSystemSessionConnectParams);
        if (svnCParams == null)
          throw new AzosIOException(Azos.Sky.StringConsts.FS_SESSION_BAD_PARAMS_ERROR + this.GetType() + ".StartSession");

        return new SVNFileSystemSession(this, null, svnCParams);
      }

      public override FileSystemSession StartSession(FileSystemSessionConnectParams cParams = null)
      {
        return this.StartSession(cParams as SVNFileSystemSessionConnectParams);
      }

    #endregion

    #region Protected

      public override string GetPathRoot(string fullPath)
      {
        return "/";
      }

      public override string[] SplitPathSegments(string fullPath)
      {
        if (fullPath.IsNullOrWhiteSpace()) return new string[0];

        var idx = fullPath.IndexOf('/');
        if (idx >= 0) fullPath = (idx + 1 == fullPath.Length) ? string.Empty : fullPath.Substring(idx + 1);

        var segs = fullPath.Split('/')
                           .Where(s => s.IsNotNullOrWhiteSpace());

        return segs.ToArray();
      }

      public override string ExtractFileName(string fullPath)
       => System.IO.Path.GetFileName(fullPath);


      protected internal override IEnumerable<string> DoGetSubDirectoryNames(FileSystemDirectory directory, bool recursive)
      {
        SVNFileSystemSession session = directory.Session as SVNFileSystemSession;

        WebDAV.Directory wdDirectory = GetSVNItem(directory) as WebDAV.Directory;

        WebDAV.Directory pathDirectory = session.WebDAV.Root.NavigatePath(wdDirectory.Path) as WebDAV.Directory;

        if (pathDirectory == null) return null;

        return pathDirectory.Directories.Select(c => c.Name);
      }

      protected internal override IEnumerable<string> DoGetFileNames(FileSystemDirectory directory, bool recursive)
      {
        SVNFileSystemSession session = directory.Session as SVNFileSystemSession;

        WebDAV.Directory wdDirectory = GetSVNItem(directory) as WebDAV.Directory;

        WebDAV.Directory pathDirectory = session.WebDAV.Root.NavigatePath(wdDirectory.Path) as WebDAV.Directory;

        if (pathDirectory == null) return null;

        return pathDirectory.Files.Select(c => c.Name);
      }

      protected internal override FileSystemSessionItem DoNavigate(FileSystemSession session, string path)
      {
        SVNFileSystemSession wdSession = session as SVNFileSystemSession;

        WebDAV.Item item = wdSession.WebDAV.Root.NavigatePath(path);

        WebDAV.Directory dir = item as WebDAV.Directory;
        if (dir != null)
          return new FileSystemDirectory(session, dir.Parent != null ? dir.Parent.Path : string.Empty, dir.Name, new SVNFSH(dir));

        WebDAV.File file = item as WebDAV.File;
        if (file != null)
          return new FileSystemFile(session, file.Parent.Path, file.Name, new SVNFSH(file));

        return null;
      }

      protected internal override bool DoRenameItem(FileSystemSessionItem item, string newName)
      {
        throw new NotImplementedException();
      }

      protected internal override void DoDeleteItem(FileSystemSessionItem item)
      {
        throw new NotImplementedException();
      }

      protected internal override FileSystemFile DoCreateFile(FileSystemDirectory dir, string name, int size)
      {
        throw new NotImplementedException();
      }

      protected internal override FileSystemFile DoCreateFile(FileSystemDirectory dir, string name, string localFile, bool readOnly)
      {
        throw new NotImplementedException();
      }

      protected internal override FileSystemDirectory DoCreateDirectory(FileSystemDirectory dir, string name)
      {
        throw new NotImplementedException();
      }

      protected internal override ulong DoGetItemSize(FileSystemSessionItem item)
      {
        var wdItem = GetSVNItem(item);

        var file = wdItem as WebDAV.File;
        if (file != null) return file.Size;

        var dir = wdItem as WebDAV.Directory;
        if (dir != null) return GetDirectorySize(dir);

        throw new AzosIOException(Azos.Sky.StringConsts.ARGUMENT_ERROR + this.GetType().Name + ".DoGetItemSize(item is FileSystemFile or FileSystemDirectory)");
      }

      private ulong GetDirectorySize(WebDAV.Directory dir)
      {
        if (dir == null) return 0;

        ulong size = 0;

        foreach (var child in dir.Children)
        {
          var childDir = child as WebDAV.Directory;
          if (childDir != null)
            size += GetDirectorySize(childDir);

          var childFile = child as WebDAV.File;
          if (childFile != null)
            size += childFile.Size;
        }

        return size;
      }

      protected internal override FileSystemStream DoGetPermissionsStream(FileSystemSessionItem item, Action<FileSystemStream> disposeAction)
      {
        throw new NotImplementedException();
      }

      protected internal override FileSystemStream DoGetMetadataStream(FileSystemSessionItem item, Action<FileSystemStream> disposeAction)
      {
        throw new NotImplementedException();
      }

      protected internal override FileSystemStream DoGetFileStream(FileSystemFile file, Action<FileSystemStream> disposeAction)
      {
        return new SVNFileSystemStream(file, (fs) => {});
      }

      protected internal override DateTime? DoGetCreationTimestamp(FileSystemSessionItem item)
      {
        WebDAV.Item wdItem = GetSVNItem(item);
        return wdItem.CreationDate;
      }

      protected internal override DateTime? DoGetModificationTimestamp(FileSystemSessionItem item)
      {
        WebDAV.Item wdItem = GetSVNItem(item);
        return wdItem.LastModificationDate;
      }

      protected internal override DateTime? DoGetLastAccessTimestamp(FileSystemSessionItem item)
      {
        throw new NotImplementedException();
      }

      protected internal override void DoSetCreationTimestamp(FileSystemSessionItem item, DateTime timestamp)
      {
        throw new NotImplementedException();
      }

      protected internal override void DoSetModificationTimestamp(FileSystemSessionItem item, DateTime timestamp)
      {
        throw new NotImplementedException();
      }

      protected internal override void DoSetLastAccessTimestamp(FileSystemSessionItem item, DateTime timestamp)
      {
        throw new NotImplementedException();
      }

      protected internal override bool DoGetReadOnly(FileSystemSessionItem item)
      {
        return true;
      }

      protected internal override void DoSetReadOnly(FileSystemSessionItem item, bool readOnly)
      {
        throw new NotImplementedException();
      }

      protected override FileSystemSessionConnectParams MakeSessionConfigParams(IConfigSectionNode node)
      {
        return FileSystemSessionConnectParams.Make<SVNFileSystemSessionConnectParams>(node);
      }

    #endregion

    #region .pvt. impl.

      private WebDAV.Item GetSVNItem(FileSystemSessionItem item)
      {
        return GetSVNFSH(item).Item;
      }

      private SVNFSH GetSVNFSH(FileSystemSessionItem item)
      {
        return item.Handle as SVNFSH;
      }

    #endregion


  } //SVNFileSystem

}
