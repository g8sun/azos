/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Azos.Web;
using Azos.Serialization.JSON;
using Azos.Conf;
using Azos.Collections;
using Azos.Data;
using Azos.Wave.Templatization;
using ErrorPage = Azos.Wave.Templatization.StockContent.Error;
using System.Threading.Tasks;

namespace Azos.Wave.Filters
{
  /// <summary>
  /// Intercepts error that arise during processing and displays an error page for exceptions and error codes
  /// </summary>
  public sealed class ErrorFilter : WorkFilter
  {
    #region CONSTS
    public const string CONFIG_SHOW_DUMP_SECTION = "show-dump";
    public const string CONFIG_LOG_SECTION       = "log";
    public const string CONFIG_SECURITY_REDIRECT_SECTION  = "security-redirect";

    public const string VAR_SECURITY_REDIRECT_URL    = "security-redirect-url";
    public const string VAR_SECURITY_REDIRECT_TARGET = "security-redirect-target";
    #endregion

    #region .ctor
    public ErrorFilter(WorkHandler handler, string name, int order) : base(handler, name, order)
    {
    }

    public ErrorFilter(WorkHandler handler, IConfigSectionNode confNode): base(handler, confNode)
    {
      ConfigureMatches(confNode, m_ShowDumpMatches, m_LogMatches, m_SecurityRedirectMatches, GetType().FullName);
      ConfigAttribute.Apply(this, confNode);
    }

    #endregion

    #region Fields
    private OrderedRegistry<WorkMatch> m_ShowDumpMatches = new OrderedRegistry<WorkMatch>();
    private OrderedRegistry<WorkMatch> m_LogMatches = new OrderedRegistry<WorkMatch>();

    private string m_SecurityRedirectURL;
    private string m_SecurityRedirectTarget;
    private OrderedRegistry<WorkMatch> m_SecurityRedirectMatches = new OrderedRegistry<WorkMatch>();

    private Type m_CustomErrorPageType;
    #endregion

    #region Properties

    /// <summary>
    /// Returns matches used by the filter to determine whether exception details should be shown
    /// </summary>
    public OrderedRegistry<WorkMatch> ShowDumpMatches { get{ return m_ShowDumpMatches;}}

    /// <summary>
    /// Returns matches used by the filter to determine whether exception details should be logged
    /// </summary>
    public OrderedRegistry<WorkMatch> LogMatches { get{ return m_LogMatches;}}

    /// <summary>
    /// When set redirects response to the specified URL if security exceptions are thrown
    /// </summary>
    [Config]
    public string SecurityRedirectURL
    {
      get{return m_SecurityRedirectURL ?? string.Empty;}
      set{ m_SecurityRedirectURL = value;}
    }

    /// <summary>
    /// When set redirects response to the specified URL if security exceptions are thrown
    /// </summary>
    [Config]
    public string SecurityRedirectTarget
    {
      get{return m_SecurityRedirectTarget ?? string.Empty;}
      set{ m_SecurityRedirectTarget = value;}
    }

    /// <summary>
    /// Returns matches used by the filter to supply custom redirect urls via redirect-url and redirect-target variables
    /// </summary>
    public OrderedRegistry<WorkMatch> SecurityRedirectMatches { get{ return m_SecurityRedirectMatches; }}

    /// <summary>
    /// Specifies a type for custom error page. Must be WebTemplate-derived type
    /// </summary>
    [Config]
    public string CustomErrorPageType
    {
      get{return m_CustomErrorPageType!=null ? m_CustomErrorPageType.AssemblyQualifiedName : string.Empty ;}
      set
      {
        if (value.IsNullOrWhiteSpace())
        {
          m_CustomErrorPageType = null;
          return;
        }

        try
        {
          var tp = Type.GetType(value, true);
          if (!typeof(WaveTemplate).IsAssignableFrom(tp))
            throw new WaveException("not WaveTemplate");
          m_CustomErrorPageType = tp;
        }
        catch(Exception tErr)
        {
          throw new WaveException(StringConsts.ERROR_PAGE_TEMPLATE_TYPE_ERROR.Args(value, tErr.ToMessageWithType()));
        }

      }
    }
    #endregion


    #region Public

    /// <summary>
    /// Handles the exception by responding appropriately with error page with conditional level of details and logging
    /// </summary>
    public static async Task HandleExceptionAsync(WorkContext work,
                                          Exception error,
                                          OrderedRegistry<WorkMatch> showDumpMatches,
                                          OrderedRegistry<WorkMatch> logMatches,
                                          string securityRedirectURL = null,
                                          string securityRedirectTarget = null,
                                          OrderedRegistry<WorkMatch> securityRedirectMatches = null,
                                          Type customPageType = null
                                        )
    {
      if (work==null || error==null) return;

      var showDump = showDumpMatches != null ?
                     showDumpMatches.OrderedValues.Any(m => m.Make(work)!=null) : false;

      if (work.Response.Buffered)
        work.Response.CancelBuffered();

      var json = work.Request.RequestedJson;

      var actual = error;
      if (actual is FilterPipelineException fpe)
        actual = fpe.RootException;

      if (actual is MvcException mvce)
        actual = mvce.InnerException;

      var securityError = Security.AuthorizationException.IsDenotedBy(error);

      var hsp = error.SearchThisOrInnerExceptionOf<IHttpStatusProvider>();
      if (hsp != null)
      {
        work.Response.StatusCode = hsp.HttpStatusCode;
        work.Response.StatusDescription = hsp.HttpStatusDescription;
      }
      else
      {
        if (securityError)
        {
          work.Response.StatusCode = WebConsts.STATUS_403;
          work.Response.StatusDescription = WebConsts.STATUS_403_DESCRIPTION;
        }
        else
        {
          work.Response.StatusCode = WebConsts.STATUS_500;
          work.Response.StatusDescription = WebConsts.STATUS_500_DESCRIPTION;
        }
      }


      if (json)
      {
        await work.Response.WriteJsonAsync(error.ToClientResponseJsonMap(showDump), JsonWritingOptions.PrettyPrintRowsAsMap).ConfigureAwait(false);
      }
      else
      {
        if (securityRedirectMatches != null && securityRedirectMatches.Count > 0)
        {
          JsonDataMap matched = null;
          foreach(var match in securityRedirectMatches.OrderedValues)
          {
            matched = match.Make(work, actual);
            if (matched!=null) break;
          }
          if (matched!=null)
          {
            var url = matched[VAR_SECURITY_REDIRECT_URL].AsString();
            var target = matched[VAR_SECURITY_REDIRECT_TARGET].AsString();

            if (url.IsNotNullOrWhiteSpace())
              securityRedirectURL = url;
            if (target.IsNotNullOrWhiteSpace())
              securityRedirectTarget = target;
          }
        }

        if (securityRedirectURL.IsNotNullOrWhiteSpace() && securityError && !work.IsAuthenticated)
        {
          var url = securityRedirectURL;
          var target = securityRedirectTarget;
          if (target.IsNotNullOrWhiteSpace())
          {
            var partsA = url.Split('#');
            var parts = partsA[0].Split('?');
            var query = parts.Length > 1 ? parts[0] + "&" : string.Empty;
            url = "{0}?{1}{2}={3}{4}".Args(parts[0], query,
              target, Uri.EscapeDataString(work.Request.Url),
              partsA.Length > 1 ? "#" + partsA[1] : string.Empty);
          }
          work.Response.RedirectAndAbort(url);
        }
        else
        {
          WaveTemplate errorPage = null;

          if (customPageType != null)
          {
            try
            {
              //20201130 DKh fix #376
              var simpleCtor = customPageType.GetConstructor(new Type[]{typeof(Exception), typeof(bool)}) == null;
              errorPage = (simpleCtor ? Activator.CreateInstance(customPageType) :
                                        Activator.CreateInstance(customPageType, error, showDump)
                          ) as WaveTemplate;//fix #376

              if (errorPage == null) throw new WaveException("not a {0}".Args(nameof(WaveTemplate)));
            }
            catch(Exception actErr)
            {
              work.Log(Log.MessageType.Error,
                        StringConsts.ERROR_PAGE_TEMPLATE_TYPE_ERROR.Args(customPageType.FullName, actErr.ToMessageWithType()),
                        typeof(ErrorFilter).FullName+".ctor(customPageType)",
                        actErr);
            }
          }

          if (errorPage == null)
            errorPage =  new ErrorPage(error, showDump);

          errorPage.Render(work, error);
        }
      }

      if (logMatches != null && logMatches.Count > 0)
      {
        JsonDataMap matched = null;
        foreach(var match in logMatches.OrderedValues)
        {
          matched = match.Make(work, error);
          if (matched!=null) break;
        }
        if (matched!=null)
        {
          matched["$ip"] = work.EffectiveCallerIPEndPoint.ToString();
          matched["$ua"] = work.Request.UserAgent.TakeFirstChars(78, "..");
          matched["$mtd"] = work.Request.Method;
          matched["$uri"] = work.Request.Url.ToString().TakeFirstChars(78, "..");
          matched["$ref"] = work.Request.Referer?.TakeFirstChars(78, "..");
          matched["$stat"] = "{0}/{1}".Args(work.Response.StatusCode, work.Response.StatusDescription);

          if (work.Portal != null)
            matched["$portal"] = work.Portal.Name;

          if (work.GeoEntity != null)
            matched["$geo"] = work.GeoEntity.LocalityName;

          //20230905 DKh #893
          var cf = Ambient.CurrentCallFlow;
          if (cf != null)
          {
            matched["$call"] = cf.RepresentAsJson();
          }

          work.Log(Log.MessageType.Error, error.ToMessageWithType(), typeof(ErrorFilter).FullName, pars: matched.ToJson(JsonWritingOptions.CompactASCII));
        }
      }
    }

    #endregion

    #region Protected

    internal static void ConfigureMatches(IConfigSectionNode confNode,
                                          OrderedRegistry<WorkMatch> showDumpMatches,
                                          OrderedRegistry<WorkMatch> logMatches,
                                          OrderedRegistry<WorkMatch> securityRedirectMatches,
                                          string from)
    {
      if (showDumpMatches != null)
        foreach(var cn in confNode[CONFIG_SHOW_DUMP_SECTION].Children.Where(cn=>cn.IsSameName(WorkMatch.CONFIG_MATCH_SECTION)))
          if(!showDumpMatches.Register( FactoryUtils.Make<WorkMatch>(cn, typeof(WorkMatch), args: new object[]{ cn })) )
            throw new WaveException(StringConsts.CONFIG_OTHER_DUPLICATE_MATCH_NAME_ERROR.Args(cn.AttrByName(Configuration.CONFIG_NAME_ATTR).Value, "{0}.ShowDump".Args(from)));

      if (logMatches != null)
        foreach(var cn in confNode[CONFIG_LOG_SECTION].Children.Where(cn=>cn.IsSameName(WorkMatch.CONFIG_MATCH_SECTION)))
          if(!logMatches.Register( FactoryUtils.Make<WorkMatch>(cn, typeof(WorkMatch), args: new object[]{ cn })) )
            throw new WaveException(StringConsts.CONFIG_OTHER_DUPLICATE_MATCH_NAME_ERROR.Args(cn.AttrByName(Configuration.CONFIG_NAME_ATTR).Value, "{0}.Log".Args(from)));

      if (securityRedirectMatches != null)
        foreach(var cn in confNode[CONFIG_SECURITY_REDIRECT_SECTION].Children.Where(cn=>cn.IsSameName(WorkMatch.CONFIG_MATCH_SECTION)))
          if(!securityRedirectMatches.Register( FactoryUtils.Make<WorkMatch>(cn, typeof(WorkMatch), args: new object[]{ cn })) )
            throw new WaveException(StringConsts.CONFIG_OTHER_DUPLICATE_MATCH_NAME_ERROR.Args(cn.AttrByName(Configuration.CONFIG_NAME_ATTR).Value, "{0}.SecurityRedirect".Args(from)));
    }

    protected override async Task DoFilterWorkAsync(WorkContext work, CallChain callChain)
    {
      try
      {
        await this.InvokeNextWorkerAsync(work, callChain).ConfigureAwait(false);
      }
      catch(Exception error)
      {
        await HandleExceptionAsync(work,
                                   error,
                                   m_ShowDumpMatches,
                                   m_LogMatches,
                                   m_SecurityRedirectURL,
                                   m_SecurityRedirectTarget,
                                   m_SecurityRedirectMatches,
                                   m_CustomErrorPageType).ConfigureAwait(false);

        if (Server.m_InstrumentationEnabled)
        {
          Interlocked.Increment(ref Server.m_stat_FilterHandleException);
        }
      }
    }

    #endregion
  }

}
