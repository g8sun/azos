/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;

using Azos.Web;
using Azos.Conf;
using Azos.Data;
using Azos.Collections;
using System.Threading.Tasks;

namespace Azos.Wave.Handlers
{
  /// <summary>
  /// Implements handler that serves content from assembly-embedded resources and class actions.
  /// Inherit from this class to implement actual handler that serves from particular assembly/namespace root
  /// </summary>
  public abstract class EmbeddedSiteHandler : WorkHandler
  {
    #region CONSTS
    public const string CONFIG_CACHE_CONTROL_SECTION = "cache-control";

    public const string SITE_ACTION = "site";
    public const string DEFAULT_SITE_PATH = "Home.htm";

    public const string VAR_PATH = "path";
    #endregion

    #region Inner Classes
    /// <summary>
    /// Represents an action that can be dispatched by a EmbeddedSiteHandler.
    /// The instance of this interface implementor is shared between requests (must be thread-safe)
    /// </summary>
    public interface IAction : INamed
    {
      /// <summary>
      /// Performs the action - by performing action work
      /// </summary>
      Task PerformAsync(WorkContext context);
    }
    #endregion


    #region .ctor
    protected EmbeddedSiteHandler(WorkHandler director, string name, int order, WorkMatch match = null)
                                  : base(director, name, order, match)
    {
      ctor();
    }

    protected EmbeddedSiteHandler(WorkHandler dispatcher, IConfigSectionNode confNode) : base(dispatcher, confNode)
    {
      ConfigAttribute.Apply(this, confNode);
      if (confNode != null && confNode.Exists)
        m_CacheControl = ConfigAttribute.Apply(new CacheControl(), confNode[CONFIG_CACHE_CONTROL_SECTION]);

      ctor();
    }

    private void ctor()
    {
      foreach(var action in GetActions())
        m_Actions.Register(action);

      var assembly = this.GetType().Assembly;

      try
      {
        var bi = new BuildInformation(assembly);
        m_LastModifiedDate = WebUtils.DateTimeToHTTPLastModifiedHeaderDateTime(bi.DateStampUTC);
      }
      catch(Exception err)
      {
        //no build info
        WriteLog(Log.MessageType.Warning, "ctor", "Assembly '{0}' has no BUILD_INFO".Args(assembly.FullName), error:  err);
        m_LastModifiedDate = WebUtils.DateTimeToHTTPLastModifiedHeaderDateTime(App.TimeSource.UTCNow);
      }
    }

    #endregion

    #region Fields
    private Registry<IAction> m_Actions = new Registry<IAction>();
    [Config] private string m_VersionSegmentPrefix;
    private CacheControl m_CacheControl = CacheControl.PublicMaxAgeSec();

    private string m_LastModifiedDate;
    #endregion

    #region Properties

    /// <summary>
    /// Returns actions that this site can perform
    /// </summary>
    public IRegistry<IAction> Actions { get { return m_Actions;} }

    /// <summary>
    /// Returns name for action that serves embedded site
    /// </summary>
    public virtual string SiteAction { get { return SITE_ACTION;}}

    /// <summary>
    /// Returns default site path that serves site root
    /// </summary>
    public virtual string DefaultSitePath { get { return DEFAULT_SITE_PATH;}}

    /// <summary>
    /// Returns resource path root, i.e. namespace prefixes where resources reside
    /// </summary>
    public abstract string RootResourcePath{ get; }


    /// <summary>
    /// Override in sites that do not support named environment sub-folders in resource structure.
    /// True by default
    /// </summary>
    public virtual bool SupportsEnvironmentBranching
    {
      get { return true;}
    }

    /// <summary>
    /// When set indicates the case-insensitive prefix of a path segment that should be ignored by the handler path resolver.
    /// Version prefixes are used for attaching a surrogate path "folder" that makes resource differ based on their content.
    /// For example when prefix is "@",  path '/embedded/img/@767868768768/picture.png' resolves to actual '/embedded/img/picture.png'
    /// </summary>
    public string VersionSegmentPrefix
    {
      get { return m_VersionSegmentPrefix;}
      set { m_VersionSegmentPrefix = value;}
    }

    public CacheControl CacheControl
    {
      get { return m_CacheControl; }
      set { m_CacheControl = value; }
    }
    #endregion

    #region Protected
    /// <summary>
    /// Override to declare what actions this site can perform
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerable<IAction> GetActions();

    /// <summary>
    /// Override to set specific cache header set per resource name
    /// </summary>
    protected virtual void SetResourceCacheHeader(WorkContext work, string sitePath, string resName)
    {
      work.Response.SetCacheControlHeaders(CacheControl);
    }


    protected override async Task DoHandleWorkAsync(WorkContext work)
    {
      string path;
      var action = ParseWork(work, out path);

      await DispatchActionAsync(work, action, path);
    }

    /// <summary>
    /// Override to extract action and path form request
    /// </summary>
    protected virtual string ParseWork(WorkContext work, out string path)
    {
      path = string.Empty;
      var fullPath = work.MatchedVars[VAR_PATH].AsString();
      if (fullPath.IsNullOrWhiteSpace()) return SiteAction;

      string action = null;

      var segs = fullPath.Split(DELIMS);

      if(segs.Length>1)
      {
        action = segs[0];
        path = string.Join("/",segs, 1, segs.Length-1);
      }
      else
        if (segs.Length==1) action=segs[0];

      if (action.IsNullOrWhiteSpace())
        action = SiteAction;

      return action;
    }


    /// <summary>
    /// Dispatched appropriate action
    /// </summary>
    protected virtual async Task DispatchActionAsync(WorkContext work, string action, string path)
    {
      if (string.Equals(SiteAction, action, StringComparison.InvariantCultureIgnoreCase))
        await serveSiteAsync(work, path);
      else
        await DispatchNonSiteActionAsync(work, action);
    }

    /// <summary>
    /// Dispatches an action which is not a site-serving one
    /// </summary>
    protected virtual async Task DispatchNonSiteActionAsync(WorkContext context, string action)
    {
      var actionInstance = m_Actions[action];
      if (actionInstance != null)
      {
        await actionInstance.PerformAsync(context).ConfigureAwait(false);
      }
      else
      {
        context.Response.StatusCode = WebConsts.STATUS_500;
        context.Response.StatusDescription = WebConsts.STATUS_500_DESCRIPTION;
        await context.Response.WriteAsync(StringConsts.DONT_KNOW_ACTION_ERROR + action).ConfigureAwait(false);
      }
    }
    #endregion

    #region .pvt
    private char[] DELIMS = new char[]{'/','\\'};

    private async Task serveSiteAsync(WorkContext work, string sitePath)
    {
      if (sitePath.IsNullOrWhiteSpace())
        sitePath = DefaultSitePath;

      var assembly = this.GetType().Assembly;

      //Cut the surrogate out of path, i.e. '/static/img/@@767868768768/picture.png' -> '/static/img/picture.png'
      sitePath = FileDownloadHandler.CutVersionSegment(sitePath, m_VersionSegmentPrefix);

      var resName = getResourcePath(sitePath);

      var ifModifiedSince = work.Request.HeaderAsString(SysConsts.HEADER_IF_MODIFIED_SINCE);
      if (ifModifiedSince.IsNotNullOrWhiteSpace() && m_LastModifiedDate.EqualsOrdIgnoreCase(ifModifiedSince))
      {
        SetResourceCacheHeader(work, sitePath, resName);
        work.Response.Redirect(null, WebConsts.RedirectCode.NotModified_304);
        return;
      }
      using(var stream = assembly.GetManifestResourceStream(resName))
      if (stream != null)
      {
        work.Response.Headers.LastModified = m_LastModifiedDate;
        SetResourceCacheHeader(work, sitePath, resName);
        work.Response.ContentType = mapContentType(resName);

        stream.Seek(0, SeekOrigin.Begin);
        await work.Response.WriteStreamAsync(stream).ConfigureAwait(false);
      }
      else throw new HTTPStatusException(WebConsts.STATUS_404, WebConsts.STATUS_404_DESCRIPTION, resName);
    }

    private string getResourcePath(string sitePath)
    {
      var root = RootResourcePath;
      if (!root.EndsWith("."))
        root += '.';

      if (SupportsEnvironmentBranching)
      {
        var envp = App.EnvironmentName;

        if (envp.IsNotNullOrWhiteSpace())
        {
          root += envp;
          if (!root.EndsWith("."))
          root += '.';
        }
      }

      //adjust namespace names
      string[] segments = sitePath.Split(DELIMS);
      for(var i=0; i<segments.Length-1; i++) //not the resource name
        segments[i] = segments[i].Replace('-','_').Replace(' ','_');


      var result = new StringBuilder();
      var first = true;
      foreach(var seg in segments)
      {
        if (!first)
          result.Append('.');
        else
          first = false;
        result.Append(seg);
      }

      return root + result;
    }

    private string mapContentType(string res)
    {
      if (res==null)
          return ContentType.HTML;

      var i = res.LastIndexOf('.');

      if (i<0 || i>res.Length-1)
          return ContentType.HTML;

      var ext = res.Substring(i+1);

      return App.GetContentTypeMappings().MapFileExtension(ext).ContentType;
    }
    #endregion
  }
}
