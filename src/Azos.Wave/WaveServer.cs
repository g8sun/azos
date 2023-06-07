/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Azos.Apps;
using Azos.Data;
using Azos.Conf;
using Azos.Collections;
using Azos.Log;
using Azos.IO.Net.Gate;
using Azos.Instrumentation;
using Azos.Serialization.JSON;

using Azos.Wave.Filters;

namespace Azos.Wave
{
  /// <summary>
  /// Web Application server provides AspNetCore middleware
  ///</summary>
  public class WaveServer : DaemonWithInstrumentation<IApplicationComponent>
  {
    #region CONSTS
    public const string CONFIG_SERVER_SECTION = "server";
    public const string CONFIG_MATCH_SECTION = "match";
    public const string CONFIG_GATE_SECTION = "gate";
    public const string CONFIG_ROOT_HANDLER_SECTION = "root-handler";
    public const string CONFIG_JSON_OPTS_SECTION = "json-options";

    public const string CONFIG_DEFAULT_ERROR_HANDLER_SECTION = "default-error-handler";
    public const int    ACCEPT_THREAD_GRANULARITY_MS = 250;
    public const int    INSTRUMENTATION_DUMP_PERIOD_MS = 3377;

    public const int    MAX_ASYNC_READ_CONTENT_LENGTH_THRESHOLD = 1 * 1024 * 1024;

    public const string HTTP_STATUS_TEXT_HDR_DEFAULT = "wv-http-status";
    public const string HTTP_BODY_ERROR_HDR_DEFAULT = "wv-body-error";
    #endregion

    #region Static

    /// <summary>
    /// Exposes active <see cref="WaveServer"/> instances in the <see cref="IApplication"/> context
    /// </summary>
    public sealed class Pool
    {
      /// <summary>
      /// Returns an app-singleton instance of WaveServer pool
      /// </summary>
      public static Pool Get(IApplication app)
       => app.NonNull(nameof(app)).Singletons.GetOrCreate(() => new Pool()).instance;

      private Pool() { m_Servers = new Registry<WaveServer>(); }

      private readonly Registry<WaveServer> m_Servers;

      internal bool RegisterActiveServer(WaveServer server) => m_Servers.Register(server);
      internal bool UnregisterActiveServer(WaveServer server) => m_Servers.Unregister(server);

      public IRegistry<WaveServer> Servers => m_Servers;

      /// <summary>
      /// Called by ASP.Net middleware to dispatch request into WaveServer pool where
      /// a specific server gets pattern match on request and handles it.
      /// Returns a server that handled the request OR NULL if the request was not handled by ANY server in the app
      /// </summary>
      public async Task<WaveServer> DispatchAsync(HttpContext httpContext)
      {
        foreach(var server in m_Servers)
        {
          var handled = await server.HandleRequestAsync(httpContext).ConfigureAwait(false);
          if (handled) return server;
        }
        return null;
      }
    }
    #endregion

    #region Match
    /// <summary>
    /// Matches incoming traffic with HttpContext. You can extend this class with custom matching logic
    /// </summary>
    public class Match : INamed, IOrdered
    {
      public Match(IConfigSectionNode node)
      {
        ConfigAttribute.Apply(this, node.NonEmpty(nameof(node)));
        if (Name.IsNullOrWhiteSpace()) Name = Guid.NewGuid().ToString();

        var host = node.Of("host", "host-name").Value;
        if (host.IsNotNullOrWhiteSpace()) m_HostName = new HostString(host);

        var bports = node.Of("ports").Value;
        if (bports.IsNotNullOrWhiteSpace())
        {
          m_BoundPorts = bports.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                               .Select(one => one.AsInt(0))
                               .Where(one => one > 0 && one <= 0xffff).ToArray();
        }

        var hsegs = node.Of("host-segments").Value;
        if (hsegs.IsNotNullOrWhiteSpace())
        {
          m_HostSegments = hsegs.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(one => one.Trim())
                                .Where(one => one.IsNotNullOrWhiteSpace()).ToArray();
        }
      }

      private HostString  m_HostName;
      private int[] m_BoundPorts;
      private string[] m_HostSegments;

      [Config] public string Name { get; private set; }
      [Config] public int Order { get; private set; }

      /// <summary>
      /// When set matches the host name and optionally a single port number
      /// </summary>
      public HostString HostName => m_HostName;

      /// <summary>
      /// When set matches the incoming traffic with any of the listed port numbers.
      /// The check is performed after the HostName check
      /// </summary>
      public IEnumerable<int> BoundPorts => m_BoundPorts;

      /// <summary>
      /// When set matches the incoming traffic with any of the listed host segments.
      /// The check is performed after the HostName check
      /// </summary>
      public IEnumerable<string> HostSegments => m_HostSegments;

      public bool Make(HttpContext httpContext)
      {
        if (m_HostName.HasValue)
        {
          if (!m_HostName.Host.EqualsIgnoreCase(httpContext.Request.Host.Host)) return false;
          if (m_HostName.Port.HasValue && httpContext.Request.Host.Port != m_HostName.Port) return false;
        }

        //match just on ports
        if (m_BoundPorts != null)
        {
          var any = false;
          var iport = httpContext.Request.Host.Port ?? 0;
          if (iport > 0)
          {
            for (var i=0; i< m_BoundPorts.Length; i++)
            {
              if (iport == m_BoundPorts[i])
              {
                any = true;
                break;
              }
            }
          }
          if (!any) return false;
        }

        //match on partial host segment presence
        if (m_HostSegments != null)
        {
          var any = false;
          var hstr = httpContext.Request.Host.Host;
          if (hstr.IsNotNullOrWhiteSpace())
          {
            for (var i = 0; i < m_HostSegments.Length; i++)
            {
              if (hstr.IndexOf(m_HostSegments[i], StringComparison.InvariantCultureIgnoreCase) > -1)
              {
                any = true;
                break;
              }
            }
          }
          if (!any) return false;
        }

        return true;
      }

      public override string ToString() => "{0}/*".Args(HostName);

      /// <summary>
      /// Registers matches declared in config. Throws error if registry already contains a match with a duplicate name
      /// </summary>
      internal static void MakeAndRegisterFromConfig(OrderedRegistry<Match> registry, IConfigSectionNode confNode, WaveServer server)
      {
        registry.Clear();
        foreach (var cn in confNode.ChildrenNamed(CONFIG_MATCH_SECTION))
        {
          var match = FactoryUtils.Make<Match>(cn, typeof(Match), args: new object[] { cn });
          if (!registry.Register(match))
          {
            throw new WaveException(StringConsts.CONFIG_SERVER_DUPLICATE_MATCH_NAME_ERROR.Args(match.Name, server.Name));
          }
        }
      }

    }

    #endregion

    #region .ctor
    public WaveServer(IApplication app) : base(app) => ctor();
    public WaveServer(IApplicationComponent director) : base(director) => ctor();
    public WaveServer(IApplication app, string name) : this(app) => Name = name;
    public WaveServer(IApplicationComponent director, string name) : this(director) => Name = name;

    private void ctor()
    {
      m_Matches = new OrderedRegistry<Match>();
    }

    protected override void Destructor()
    {
      base.Destructor();
      DisposeIfDisposableAndNull(ref m_Gate);
      DisposeIfDisposableAndNull(ref m_RootHandler);
    }
    #endregion

    #region Fields

    private bool m_LogHandleExceptionErrors;
    private OrderedRegistry<Match> m_Matches;

    private Thread m_InstrumentationThread;
    private AutoResetEvent m_InstrumentationThreadWaiter;


    private INetGate m_Gate;
    private CompositeWorkHandler m_RootHandler;

    private string m_HttpStatusTextHeader;
    private string m_BodyErrorHeader = HTTP_BODY_ERROR_HDR_DEFAULT;
    private int m_AsyncReadContentLengthThreshold;

    private OrderedRegistry<WorkMatch> m_ErrorShowDumpMatches = new OrderedRegistry<WorkMatch>();
    private OrderedRegistry<WorkMatch> m_ErrorLogMatches = new OrderedRegistry<WorkMatch>();


    //*Instrumentation Statistics*//
    internal bool m_InstrumentationEnabled;

    internal long m_stat_ServerRequest;
    internal long m_stat_ServerGateDenial;
    internal long m_stat_ServerHandleException;
    internal long m_stat_FilterHandleException;

    internal long m_stat_WorkContextWrittenResponse;
    internal long m_stat_WorkContextBufferedResponse;
    internal long m_stat_WorkContextBufferedResponseBytes;
    internal long m_stat_WorkContextCtor;
    internal long m_stat_WorkContextDctor;
    internal long m_stat_WorkContextAborted;
    internal long m_stat_WorkContextHandled;
    internal long m_stat_WorkContextNeedsSession;

    internal long m_stat_SessionNew;
    internal long m_stat_SessionExisting;
    internal long m_stat_SessionEnd;
    internal long m_stat_SessionInvalidID;

    internal long m_stat_GeoLookup;
    internal long m_stat_GeoLookupHit;

    internal NamedInterlocked m_stat_PortalRequest = new NamedInterlocked();

    #endregion

    #region Properties

    public override string ComponentLogTopic => CoreConsts.WAVE_TOPIC;

    public override string ComponentCommonName =>  "ws-" + Name;

    /// <summary>
    /// Provides a list of served endpoints
    /// </summary>
    public override string ServiceDescription => Matches.Aggregate(string.Empty, (s, p) => s + "  " + p);


    /// <summary>
    /// Optional name of header used for disclosure of WorkContext.ID. If set to null, suppresses the header
    /// </summary>
    [Config(Default = CoreConsts.HTTP_HDR_DEFAULT_CALL_FLOW)]
    [ExternalParameter(CoreConsts.EXT_PARAM_GROUP_WEB)]
    public string CallFlowHeader { get; set;} = CoreConsts.HTTP_HDR_DEFAULT_CALL_FLOW;


    /// <summary>
    /// When true, emits instrumentation messages
    /// </summary>
    [Config]
    [ExternalParameter(CoreConsts.EXT_PARAM_GROUP_WEB, CoreConsts.EXT_PARAM_GROUP_INSTRUMENTATION)]
    public override bool InstrumentationEnabled
    {
        get { return m_InstrumentationEnabled;}
        set { m_InstrumentationEnabled = value;}
    }


    /// <summary>
    /// When true writes errors that get thrown in server catch-all HandleException methods
    /// </summary>
    [Config]
    [ExternalParameter(CoreConsts.EXT_PARAM_GROUP_WEB, CoreConsts.EXT_PARAM_GROUP_INSTRUMENTATION)]
    public bool LogHandleExceptionErrors
    {
      get { return m_LogHandleExceptionErrors;}
      set { m_LogHandleExceptionErrors = value;}
    }

    /// <summary>
    /// The name of http status text header where the system reports additional textual description of the HTTP response code
    /// </summary>
    [Config]
    [ExternalParameter(CoreConsts.EXT_PARAM_GROUP_WEB)]
    public string HttpStatusTextHeader
    {
      get => m_HttpStatusTextHeader.Default(HTTP_STATUS_TEXT_HDR_DEFAULT);
      set => m_HttpStatusTextHeader = value;
    }

    /// <summary>
    /// The name of body error header where the system reports additional body processing (e.g. unparsable JSON) details.
    /// Default is HTTP_BODY_ERROR_HDR_DEFAULT, set to null to turn off body error reporting.
    /// </summary>
    [Config(Default = HTTP_BODY_ERROR_HDR_DEFAULT)]
    [ExternalParameter(CoreConsts.EXT_PARAM_GROUP_WEB)]
    public string HttpBodyErrorHeader
    {
      get => m_BodyErrorHeader;
      set => m_BodyErrorHeader = value;
    }

    /// <summary>
    /// True when header is enabled
    /// </summary>
    public bool HttpBodyErrorHeaderEnabled => HttpBodyErrorHeader.IsNotNullOrWhiteSpace();

    /// <summary>
    /// When set to the value above zero, triggers synchronous processing of requests that have `content-length` header specified
    /// with values less than the specified threshold value, otherwise the system uses ASYNC reading.
    /// Zero (default value) - turns off sync reading of small payloads and uses ASYNC exclusively
    /// </summary>
    [Config]
    [ExternalParameter(CoreConsts.EXT_PARAM_GROUP_WEB)]
    public int AsyncReadContentLengthThreshold
    {
      get => m_AsyncReadContentLengthThreshold;
      set => m_AsyncReadContentLengthThreshold = value.KeepBetween(0, MAX_ASYNC_READ_CONTENT_LENGTH_THRESHOLD);
    }

    /// <summary>
    /// Provides server-wide json processing options. Defaults to null
    /// </summary>
    public JsonReadingOptions JsonOptions { get; set; }

    /// <summary>
    /// Returns server match collection
    /// </summary>
    public IOrderedRegistry<Match> Matches => m_Matches;


    /// <summary>
    /// Gets/sets network gate
    /// </summary>
    public INetGate Gate
    {
      get { return m_Gate;}
      set
      {
        CheckDaemonInactive();
        m_Gate = value;
      }
    }

    [Config]
    [ExternalParameter(CoreConsts.EXT_PARAM_GROUP_WEB)]
    public string GateCallerRealIpAddressHeader  {  get; set;  }


    /// <summary>
    /// Gets/sets work dispatcher
    /// </summary>
    public CompositeWorkHandler RootHandler
    {
      get { return m_RootHandler;}
      set
      {
        CheckDaemonInactive();
        if (value!=null && value.ComponentDirector!=this)
          throw new WaveException(StringConsts.DISPATCHER_NOT_THIS_SERVER_ERROR);
        m_RootHandler = value;
      }
    }

    /// <summary>
    /// Returns matches used by the server's default error handler to determine whether exception details should be shown
    /// </summary>
    public OrderedRegistry<WorkMatch> ShowDumpMatches => m_ErrorShowDumpMatches;

    /// <summary>
    /// Returns matches used by the server's default error handler to determine whether exception details should be logged
    /// </summary>
    public OrderedRegistry<WorkMatch> LogMatches => m_ErrorLogMatches;

    #endregion

    #region Public
    /// <summary>
    /// Handles processing exception by calling ErrorFilter.HandleException(work, error).
    /// All parameters except ERROR can be null - which indicates error that happened during WorkContext dispose
    /// </summary>
    public virtual async Task HandleExceptionAsync(WorkContext work, Exception error)
    {
      try
      {
        if (m_InstrumentationEnabled) Interlocked.Increment(ref m_stat_ServerHandleException);

        //work may be null (when WorkContext is already disposed)
        if (work != null)
          await ErrorFilter.HandleExceptionAsync(work, error, m_ErrorShowDumpMatches, m_ErrorLogMatches).ConfigureAwait(false);
        else
          WriteLog(MessageType.Error,
              nameof(HandleExceptionAsync),
              StringConsts.SERVER_DEFAULT_ERROR_WC_NULL_ERROR + error.ToMessageWithType(),
              error);
      }
      catch(Exception error2)
      {
        if (m_LogHandleExceptionErrors)
          try
          {
            WriteLog(MessageType.Error,
                  nameof(HandleExceptionAsync),
                  StringConsts.SERVER_DEFAULT_ERROR_HANDLER_ERROR + error2.ToMessageWithType(),
                  error2,
                  pars: new
                  {
                    OriginalError = error.ToMessageWithType()
                  }.ToJson()
                  );
          }
          catch{}
      }
    }
    #endregion


    #region Protected

      protected override void DoConfigure(IConfigSectionNode node)
      {
        if (node==null || !node.Exists)
        {
          //0 get very root
          node = App.ConfigRoot[SysConsts.CONFIG_WAVE_SECTION];
          if (!node.Exists) return;

          //1 try to find the server with the same name as this instance
          var snode = node.Children.FirstOrDefault(cn=>cn.IsSameName(CONFIG_SERVER_SECTION) && cn.IsSameNameAttr(Name));

          //2 try to find a server without a name
          if (snode==null)
            snode = node.Children.FirstOrDefault(cn=>cn.IsSameName(CONFIG_SERVER_SECTION) && cn.AttrByName(Configuration.CONFIG_NAME_ATTR).Value.IsNullOrWhiteSpace());

          if (snode==null) return;
          node = snode;
        }


        ConfigAttribute.Apply(this, node);


        Match.MakeAndRegisterFromConfig(m_Matches, node, this);

        var nGate = node[CONFIG_GATE_SECTION];
        if (nGate.Exists)
        {
          DisposeIfDisposableAndNull(ref m_Gate);
          m_Gate = FactoryUtils.MakeAndConfigure<INetGateImplementation>(nGate, typeof(NetGate), args: new object[]{this});
        }


        var nRootHandler = node[CONFIG_ROOT_HANDLER_SECTION];
        nRootHandler.NonEmpty("Section `{0}/`".Args(CONFIG_ROOT_HANDLER_SECTION));

        m_RootHandler = FactoryUtils.MakeDirectedComponent<CompositeWorkHandler>(this,
                                           nRootHandler,
                                           typeof(CompositeWorkHandler),
                                           new object[]{ nRootHandler });

        ErrorFilter.ConfigureMatches(node[CONFIG_DEFAULT_ERROR_HANDLER_SECTION], m_ErrorShowDumpMatches, m_ErrorLogMatches, null, GetType().FullName);

        var nJsonOpts = node[CONFIG_JSON_OPTS_SECTION];
        if (nJsonOpts.Exists)
        {
          JsonOptions = FactoryUtils.MakeAndConfigure<JsonReadingOptions>(nJsonOpts, typeof(JsonReadingOptions));
        }

      }

      protected override void DoStart()
      {
        if (m_Matches.Count==0)
          throw new WaveException(StringConsts.SERVER_NO_MATCHES_ERROR.Args(Name));

        var serverPool = Pool.Get(App);

        if (!serverPool.RegisterActiveServer(this))
          throw new WaveException(StringConsts.SERVER_COULD_NOT_GET_REGISTERED_ERROR.Args(Name));

        try
        {
           if (m_Gate!=null)
             if (m_Gate is Daemon)
               ((Daemon)m_Gate).Start();


           m_RootHandler.NonNull(nameof(RootHandler));

           m_InstrumentationThread = new Thread(instrumentationThreadSpin);
           m_InstrumentationThread.Name = "{0}-InstrumentationThread".Args(Name);
           m_InstrumentationThreadWaiter = new AutoResetEvent(false);
        }
        catch
        {

          if (m_Gate!=null && m_Gate is Daemon)
            ((Daemon)m_Gate).WaitForCompleteStop();

          serverPool.UnregisterActiveServer(this);
          throw;
        }

        m_InstrumentationThread.Start();
      }

      protected override void DoSignalStop()
      {
        if (m_InstrumentationThreadWaiter!=null)
              m_InstrumentationThreadWaiter.Set();

        if (m_Gate!=null)
          if (m_Gate is Daemon)
             ((Daemon)m_Gate).SignalStop();
      }

      protected override void DoWaitForCompleteStop()
      {
        Pool.Get(App).UnregisterActiveServer(this);

        if (m_InstrumentationThread != null)
        {
          m_InstrumentationThread.Join();
          m_InstrumentationThread = null;
          m_InstrumentationThreadWaiter.Close();
        }

         if (m_Gate!=null)
            if (m_Gate is Daemon)
                ((Daemon)m_Gate).WaitForCompleteStop();
      }

    /// <summary>
    /// Returns true if the specfied context will be services by this server based on its
    /// listen-on hosts, ports etc...
    /// </summary>
    public virtual bool MatchContext(HttpContext httpContext)
    {
      if (httpContext == null) return false;
      return m_Matches.OrderedValues.Any(match => match.Make(httpContext));
    }

    /// <summary>
    /// Called by the Asp.Net middleware via Pool, an entry point for server request processing.
    /// Return true if request was handled and processing should stop
    /// </summary>
    public async Task<bool> HandleRequestAsync(HttpContext httpContext)
    {
      //warning: match PREFIXES return false
      var matches = MatchContext(httpContext);
      if (!matches) return false;

      WorkContext work = null;
      try
      {
        if (m_InstrumentationEnabled) Interlocked.Increment(ref m_stat_ServerRequest);

        var gate = m_Gate;
        if (gate != null)
        {
          try
          {
            var action = gate.CheckTraffic(new AspHttpIncomingTraffic(httpContext, GateCallerRealIpAddressHeader));
            if (action != GateAction.Allow)
            {
              //access denied
              httpContext.Response.StatusCode = WebConsts.STATUS_429;
              //await httpContext.Response.WriteAsync(WebConsts.STATUS_429_DESCRIPTION).ConfigureAwait(false);
              if (m_InstrumentationEnabled) Interlocked.Increment(ref m_stat_ServerGateDenial);
              return true;
            }
          }
          catch (Exception denyError)
          {
            WriteLog(MessageType.Error, nameof(HandleRequestAsync), denyError.ToMessageWithType(), denyError);
          }
        }

        work = MakeContext(httpContext);

        await m_RootHandler.FilterAndHandleWorkAsync(work).ConfigureAwait(false);
      }
      catch(Exception unhandled)
      {
        await this.HandleExceptionAsync(work, unhandled).ConfigureAwait(false);
      }
      finally
      {
        try
        {
          await DisposeAndNullAsync(ref work).ConfigureAwait(false);
        }
        catch(Exception swallow)
        {
          WriteLogFromHere(MessageType.Error, "work.dctor leaked: " + swallow.Message, swallow);
        }
      }

      return true;
    }

    /// <summary>
    /// Factory method to make WorkContext
    /// </summary>
    protected virtual WorkContext MakeContext(HttpContext httpContext)
      =>  new WorkContext(this, httpContext);

    #endregion

    #region .pvt


    private void instrumentationThreadSpin()
    {
      var pe = m_InstrumentationEnabled;
      while(Running)
      {
        if (pe!=m_InstrumentationEnabled)
        {
          resetStats();
          pe = m_InstrumentationEnabled;
        }

        if (m_InstrumentationEnabled &&
            App.Instrumentation.Enabled)
        {
            dumpStats();
            resetStats();
        }

        m_InstrumentationThreadWaiter.WaitOne(INSTRUMENTATION_DUMP_PERIOD_MS);
      }
    }

     private void resetStats()
     {
        m_stat_ServerRequest                        = 0;
        m_stat_ServerGateDenial                     = 0;
        m_stat_ServerHandleException                = 0;
        m_stat_FilterHandleException                = 0;


        m_stat_WorkContextWrittenResponse           = 0;
        m_stat_WorkContextBufferedResponse          = 0;
        m_stat_WorkContextBufferedResponseBytes     = 0;
        m_stat_WorkContextCtor                      = 0;
        m_stat_WorkContextDctor                     = 0;
        m_stat_WorkContextAborted                   = 0;
        m_stat_WorkContextHandled                   = 0;
        m_stat_WorkContextNeedsSession              = 0;

        m_stat_SessionNew                           = 0;
        m_stat_SessionExisting                      = 0;
        m_stat_SessionEnd                           = 0;
        m_stat_SessionInvalidID                     = 0;

        m_stat_GeoLookup                            = 0;
        m_stat_GeoLookupHit                         = 0;

        m_stat_PortalRequest.Clear();
     }

     private void dumpStats()
     {
        var i = App.Instrumentation;

        i.Record( new Instrumentation.ServerRequest                      (Name, m_stat_ServerRequest                      ));
        i.Record( new Instrumentation.ServerGateDenial                   (Name, m_stat_ServerGateDenial                   ));
        i.Record( new Instrumentation.ServerHandleException              (Name, m_stat_ServerHandleException              ));
        i.Record( new Instrumentation.FilterHandleException              (Name, m_stat_FilterHandleException              ));


        i.Record( new Instrumentation.WorkContextWrittenResponse         (Name, m_stat_WorkContextWrittenResponse         ));
        i.Record( new Instrumentation.WorkContextBufferedResponse        (Name, m_stat_WorkContextBufferedResponse        ));
        i.Record( new Instrumentation.WorkContextBufferedResponseBytes   (Name, m_stat_WorkContextBufferedResponseBytes   ));
        i.Record( new Instrumentation.WorkContextCtor                    (Name, m_stat_WorkContextCtor                    ));
        i.Record( new Instrumentation.WorkContextDctor                   (Name, m_stat_WorkContextDctor                   ));
        i.Record( new Instrumentation.WorkContextAborted                 (Name, m_stat_WorkContextAborted                 ));
        i.Record( new Instrumentation.WorkContextHandled                 (Name, m_stat_WorkContextHandled                 ));
        i.Record( new Instrumentation.WorkContextNeedsSession            (Name, m_stat_WorkContextNeedsSession            ));

        i.Record( new Instrumentation.SessionNew                         (Name, m_stat_SessionNew                         ));
        i.Record( new Instrumentation.SessionExisting                    (Name, m_stat_SessionExisting                    ));
        i.Record( new Instrumentation.SessionEnd                         (Name, m_stat_SessionEnd                         ));
        i.Record( new Instrumentation.SessionInvalidID                   (Name, m_stat_SessionInvalidID                   ));

        i.Record( new Instrumentation.GeoLookup                          (Name, m_stat_GeoLookup                          ));
        i.Record( new Instrumentation.GeoLookupHit                       (Name, m_stat_GeoLookupHit                       ));

        foreach(var kvp in m_stat_PortalRequest.SnapshotAllLongs(0))
            i.Record( new Instrumentation.ServerPortalRequest(Name+"."+kvp.Key, kvp.Value) );

        var sample = (int)m_stat_WorkContextBufferedResponseBytes;
        if (sample!=0) Platform.RandomGenerator.Instance.FeedExternalEntropySample(sample);
     }

    #endregion

  }

}
