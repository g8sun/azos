﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Azos.Apps;
using Azos.Conf;
using Azos.Serialization.JSON;
using Azos.Web;

namespace Azos.Client
{
  /// <summary>
  /// Defines endpoints of Http/s service
  /// </summary>
  public class HttpEndpoint : EndpointBase<HttpService>, IHttpEndpoint
  {
    /// <summary>
    /// Implements internal HttpClient wrapper which supports various aspects (e.g. IAuthImpersonationAspect, IDistributedCallFlowAspect etc.)
    /// </summary>
    internal class ClientWithAspects : HttpClient,
                                       WebCallExtensions.IDistributedCallFlowAspect,
                                       WebCallExtensions.IAuthImpersonationAspect,
                                       WebCallExtensions.IRequestBodyErrorAspect
    {
      public ClientWithAspects(HttpEndpoint endpoint, HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler)
       => Endpoint = endpoint;

      public readonly HttpEndpoint Endpoint;
      public string DistributedCallFlowHeader =>  Endpoint.DistributedCallFlowHeader;
      public string AuthImpersonationHeader => Endpoint.AuthImpersonateHeader;
      public string BodyErrorHeader => Endpoint.BodyErrorAspect?.BodyErrorHeader;

      public DistributedCallFlow GetDistributedCallFlow()
        => Endpoint.EnableDistributedCallFlow ? ExecutionContext.CallFlow as DistributedCallFlow : null;

      public async Task<string> GetAuthImpersonationHeaderAsync(Func<object> fGetIdentityContext)
      {
        if (!Endpoint.AuthImpersonate) return null;//turned off

        var aspectName = Endpoint.AuthAspectName;

        if (aspectName.IsNullOrWhiteSpace())//Use SYS TOKEN
        {
          return Ambient.CurrentCallUser.MakeSysTokenAuthHeader().Value;
        }

        //Use ASPECT
        var aspect = Endpoint.TryGetAspect<IHttpAuthAspect>(aspectName);
        aspect.NonNull("IHttpAuthApsect: " + aspectName);
        var identityContext = fGetIdentityContext != null ? fGetIdentityContext() : null;
        var result = await aspect.ObtainAuthorizationHeaderAsync(Endpoint, identityContext).ConfigureAwait(false);
        return result;
      }

      public async Task ProcessBodyErrorAsync(string uri,
                                              HttpMethod method,
                                              object body,
                                              string contentType,
                                              JsonWritingOptions options,
                                              HttpRequestMessage request,
                                              HttpResponseMessage response,
                                              bool isSuccess,
                                              string rawResponseContent,
                                              IEnumerable<string> bodyErrorValues)
      {
        var aspect = Endpoint.BodyErrorAspect;
        if (aspect == null) return;
        await aspect.ProcessBodyErrorAsync(uri,
                                           method,
                                           body,
                                           contentType,
                                           options,
                                           request,
                                           response,
                                           isSuccess,
                                           rawResponseContent,
                                           bodyErrorValues).ConfigureAwait(false);
      }
    }


    public const int DEFAULT_TIMEOUT_MS = 10_000;

    public HttpEndpoint(HttpService service, IConfigSectionNode conf) : base(service, conf)
    {
    }

    protected override void Destructor()
    {
      DisposeAndNull(ref m_Client);
      DisposeAndNull(ref m_ClientHandler);
      base.Destructor();
    }

    private object m_Lock = new object();
    private HttpClientHandler m_ClientHandler;
    private HttpClient m_Client;

    /// <summary>
    /// Physical URI of the endpoint (the physical address of Http endpoint) base address.
    /// The Contract gets appended to this address when making actual calls
    /// </summary>
    [Config]
    public Uri Uri { get; private set; }

    /// <summary>
    /// When true, enables attaching an HTTP header containing DistributedCallFlow object (if available) to outgoing calls
    /// </summary>
    [Config]
    public bool EnableDistributedCallFlow { get; private set; }

    /// <summary>
    /// When set, overrides the HTTP_HDR_DEFAULT_CALL_FLOW header name
    /// </summary>
    [Config]
    public string DistributedCallFlowHeader { get; private set; }

    /// <summary> If True, automatically follows HTTP redirect </summary>
    [Config(Default = true)]
    public bool AutoRedirect { get; private set; } = true;

    /// <summary> When set imposes maximum on the redirect count </summary>
    [Config]
    public int? AutoRedirectMax { get; private set; }

    /// <summary> If True, automatically decompresses traffic </summary>
    [Config(Default = true)]
    public bool AutoDecompress { get; private set; } = true;

    [Config(Default = "Basic")]
    public string AuthScheme { get; internal set; } = "Basic";

    [Config]
    public string AuthHeader { get; internal set; }

    /// <summary>
    /// When set to true, attaches Authorization header either with `SysToken` scheme and sysAuthToken content, overriding
    /// the AuthHeader value (if any); OR if HttpAuthAspectName is set delegates header value acquisition to that named aspect instance
    /// </summary>
    [Config]
    public bool AuthImpersonate { get; internal set; }

    /// <summary>
    /// When set, enables acquisition of AUTH header value from IHttpAuthAspect with the specified name
    /// </summary>
    [Config]
    public string AuthAspectName { get; internal set; }

    /// <summary>
    /// When set, enables body error processing via a named aspect
    /// </summary>
    [Config]
    public string BodyErrorAspectName { get; internal set; }

    /// <summary>
    /// Returns the effective aspect for this endpoint or null if not configured
    /// </summary>
    public IHttpBodyErrorAspect BodyErrorAspect
    {
      get
      {
        var aname = BodyErrorAspectName;
        if (aname.IsNullOrWhiteSpace()) return null;
        return this.TryGetAspect<IHttpBodyErrorAspect>(aname);
      }
    }


    /// <summary>
    /// When set, overrides the standard HTTP `Authorization` header name when impersonation is used
    /// </summary>
    [Config]
    public string AuthImpersonateHeader { get; internal set; }

    [Config(Default = true)]
    public bool AcceptJson { get; internal set; } = true;

    [Config(Default = true)]
    public bool AcceptBixon { get; internal set; } = true;

    [Config]
    public bool UseCookies { get; internal set; }

    /// <summary> When set imposes maximum content buffer size limit in bytes </summary>
    [Config]
    public int? MaxRequestContentBufferSize { get; private set; }

    /// <summary> When set imposes maximum on the response headers length sent back from server </summary>
    [Config]
    public int? MaxResponseHeadersLength { get; private set; }

    /// <summary> When set imposes maximum on connection count </summary>
    [Config]
    public int? MaxConnections { get; private set; }

    /// <summary>
    /// Returns Http Client which is used to make calls to the remote http endpoint
    /// </summary>
    public HttpClient Client
    {
      get
      {
        EnsureObjectNotDisposed();
        Uri.NonNull("`{0}` is not configured".Args(nameof(Uri)));
        lock (m_Lock)
        {
          if (m_Client == null)
          {
            m_ClientHandler = MakeHttpClientHandler();
            m_Client = MakeHttpClient();
          }
          return m_Client;
        }
      }
    }

    /// <summary>
    /// Override factory to make and configure/build HttpClientHandler instance.
    /// The default implementation allocates HttpClientHandler and sets AllowAutoRedirect.
    /// Attention: This method is called under lock and must not create any blocking conditions
    /// </summary>
    protected virtual HttpClientHandler MakeHttpClientHandler()
    {
      var result = new HttpClientHandler();
      result.AllowAutoRedirect = AutoRedirect;

      if (AutoRedirectMax.HasValue) result.MaxAutomaticRedirections = AutoRedirectMax.Value;
      if (MaxRequestContentBufferSize.HasValue) result.MaxRequestContentBufferSize = MaxRequestContentBufferSize.Value;
      if (MaxResponseHeadersLength.HasValue) result.MaxResponseHeadersLength = MaxResponseHeadersLength.Value;
      if (MaxConnections.HasValue) result.MaxConnectionsPerServer = MaxConnections.Value;

      result.UseCookies = UseCookies;

      result.AutomaticDecompression = AutoDecompress ?
               result.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate :
               result.AutomaticDecompression = DecompressionMethods.None;


      return result;
    }

    /// <summary>
    /// Override factory to make and configure/build HttpClient instance.
    /// The default implementation allocates HttpClient(m_ClientHandler) and conditionally sets Accept: json.; and Authorization header if they are set.
    /// The HttpClient.BaseAddress is set to Uri property.
    /// Attention: This method is called under lock and must not create any blocking conditions
    /// </summary>
    protected virtual HttpClient MakeHttpClient()
    {
      var result = new ClientWithAspects(this, m_ClientHandler, disposeHandler: false);
      result.Timeout = TimeSpan.FromMilliseconds(TimeoutMs > 0 ? TimeoutMs : this.Service.DefaultTimeoutMs > 0 ? Service.DefaultTimeoutMs : DEFAULT_TIMEOUT_MS);
      result.BaseAddress = this.Uri.NonNull("`{0}` is not configured".Args(nameof(Uri)));

      if (AcceptJson)
        result.DefaultRequestHeaders.Accept.ParseAdd(ContentType.JSON);

      if (AcceptBixon)//#874 20230604 DKh
        result.DefaultRequestHeaders.Accept.ParseAdd(ContentType.BIXON);

      //If impersonation is used, it attaches headers per call obtained from Ambient security context
      //https://stackoverflow.com/questions/50399003/send-httpclient-request-without-defaultrequestheaders
      if (!AuthImpersonate && AuthHeader.IsNotNullOrWhiteSpace())
        result.DefaultRequestHeaders.Authorization =
          new AuthenticationHeaderValue(AuthScheme, AuthHeader);

      return result;
    }

    public override CallErrorClass NotifyCallError(ITransport transport, Exception cause)
    {
    //delegate this into the Aspect/Extension, so we can classify things like 500 -> logic error via pattern match on exception etc...

      var result = CallErrorClass.MakingCall;

      if (cause != null)
      {

        var isCallProblem = cause is HttpRequestException ||
                            cause is TaskCanceledException; //timeout

        if (isCallProblem)
        {
          //mutate circuit breaker state machine
         // this.m_CircuitBreakerTimeStampUtc = now;//trip
        }
        else result = CallErrorClass.ServiceLogic;
      }

      if (ComponentEffectiveLogLevel <= Log.MessageType.Error)
      {
        WriteLog(Log.MessageType.Error, nameof(NotifyCallError),
                     "HttpEndpoint `{0}` -> `{1}` ({2}) error: {3}".Args(ServiceDescription,
                                                                       Uri,
                                                                       result,
                                                                       (cause?.ToMessageWithType()).Default("<none>")),
                     error: cause,
                     related: Ambient.CurrentCallFlow?.ID);
      }

      return result;
    }

    public override void NotifyCallSuccess(ITransport transport)
    {
      //throw new NotImplementedException();
      //reset circuit breaker etc...
    }

  }
}
