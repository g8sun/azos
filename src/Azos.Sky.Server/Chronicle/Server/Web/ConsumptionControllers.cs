﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Threading.Tasks;

using Azos.Log;
using Azos.Platform;
using Azos.Security.Chronicle;
using Azos.Wave.Mvc;
using Azos.Web;

namespace Azos.Sky.Chronicle.Server.Web
{
  [NoCache]
  [ChroniclePermission]
  [ApiControllerDoc(
    BaseUri = "/chronicle/log",
    Connection = "default/keep alive",
    Title = "Log Chronicle",
    Authentication = "Token/Default",
    Description = "Provides REST API for uploading log batches and querying chronicles by posting filter requests",
    TypeSchemas = new[]{typeof(ChroniclePermission) }
  )]
  [Release(ReleaseType.Preview, 2020, 07, 05, "Initial Release", Description = "Preview release of API")]
  public class Log : ApiProtocolController
  {
    [Action(Name = "view")]
    [ChroniclePermission(ChronicleAccessLevel.Browse)]
    public async Task View(Guid? id = null, Guid? rel = null)
    {
      string esc(string s)
        => s.IsNullOrWhiteSpace() ? "" : s.Replace("\"", "'")
                                          .Replace("<", "&lt;")
                                          .Replace(">", "&gt;");

      WorkContext.NeedsSession();
      var html = typeof(Log).GetText("LogView.htm");

      html = html.Replace("[:USER:]", esc(WorkContext.Session.User.Name))
                 .Replace("[:APP:]", esc(App.AppId.Value))
                 .Replace("[:HOST:]", esc(Computer.HostName))
                 .Replace("[:ENV:]", esc(App.EnvironmentName))
                 .Replace("[:FILTER-ID:]", id.HasValue ? id.ToString() : "")
                 .Replace("[:FILTER-REL:]", rel.HasValue ? rel.ToString() : "");


      WorkContext.Response.ContentType = ContentType.HTML;
      await WorkContext.Response.WriteAsync(html, false).ConfigureAwait(false);
    }


    [ApiEndpointDoc(Title = "Filter",
                    Uri = "filter",
                    Description = "Queries log chronicle by applying a structured filter `{@LogChronicleFilter}`",
                    Methods = new []{"POST = post filter object for query execution"},
                    RequestHeaders = new[]{ API_DOC_HDR_ACCEPT_JSON },
                    ResponseHeaders = new[]{ API_DOC_HDR_NO_CACHE },
                    RequestBody = "JSON representation of `{@LogChronicleFilter}`",
                    ResponseContent = "JSON filter result - enumerable of `{@Message}`",
                    TypeSchemas = new[]{typeof(Message)})]
    [ActionOnPost(Name = "filter"), AcceptsJson]
    [ChroniclePermission(ChronicleAccessLevel.Browse)]
    public async Task<object> Filter(LogChronicleFilter filter) => await ApplyFilterAsync(filter).ConfigureAwait(false);

    [ApiEndpointDoc(Title = "Filter Facts",
                    Uri = "filter-facts",
                    Description = "Queries log chronicle by applying a structured filter `{@LogChronicleFactFilter}` and returning facts",
                    Methods = new[] { "POST = post filter object for query execution" },
                    RequestHeaders = new[] { API_DOC_HDR_ACCEPT_JSON },
                    ResponseHeaders = new[] { API_DOC_HDR_NO_CACHE },
                    RequestBody = "JSON representation of `{@LogChronicleFilter}`",
                    ResponseContent = "JSON filter result - enumerable of `{@Fact}`",
                    TypeSchemas = new[] { typeof(Fact) })]
    [ActionOnPost(Name = "filter-facts"), AcceptsJson]
    [ChroniclePermission(ChronicleAccessLevel.Browse)]
    public async Task<object> Filter_Facts(LogChronicleFactFilter filter) => await ApplyFilterAsync(filter).ConfigureAwait(false);

    [ApiEndpointDoc(Title = "Batch",
                    Uri = "batch",
                    Description = "Uploads a batch of log messages to log chronicle using `{@LogBatch}`",
                    Methods = new[] { "POST = post `{@LogBatch}` for insertion into archive" },
                    RequestHeaders = new[] { API_DOC_HDR_ACCEPT_JSON },
                    ResponseHeaders = new[] { API_DOC_HDR_NO_CACHE },
                    RequestBody = "JSON representation of `{@LogBatch}`",
                    ResponseContent = "Api Change Result")]
    [ActionOnPost(Name = "batch"), AcceptsJson]
    [ChroniclePermission(ChronicleAccessLevel.Emit)]
    public async Task<object> PostDataBatch(LogBatch batch) => await SaveNewAsync(batch).ConfigureAwait(false);
  }

  [NoCache]
  [ChroniclePermission]
  [ApiControllerDoc(
    BaseUri = "/chronicle/instrumentation",
    Connection = "default/keep alive",
    Title = "Instrumentation Chronicle",
    Authentication = "Token/Default",
    Description = "Provides REST API for uploading instrumentation batches and querying chronicles by posting filter requests",
    TypeSchemas = new[] { typeof(ChroniclePermission) }
  )]
  [Release(ReleaseType.Preview, 2020, 07, 05, "Initial Release", Description = "Preview release of API")]
  public class Instrumentation : ApiProtocolController
  {
    [ApiEndpointDoc(Title = "Filter",
                    Uri = "filter",
                    Description = "Queries instrumentation chronicle by applying a structured filter `{@InstrumentationChronicleFilter}`",
                    Methods = new[] { "POST = post filter object for query execution" },
                    RequestHeaders = new[] { API_DOC_HDR_ACCEPT_JSON },
                    ResponseHeaders = new[] { API_DOC_HDR_NO_CACHE },
                    RequestBody = "JSON representation of `{@InstrumentationChronicleFilter}`",
                    ResponseContent = "JSON filter result - enumerable of `{@Datum}`",
                    TypeSchemas = new[] { typeof(Message) })]
    [ActionOnPost(Name = "filter"), AcceptsJson]
    [ChroniclePermission(ChronicleAccessLevel.Browse)]
    public async Task<object> Filter(InstrumentationChronicleFilter filter) => await ApplyFilterAsync(filter).ConfigureAwait(false);

    [ApiEndpointDoc(Title = "Batch",
                    Uri = "batch",
                    Description = "Uploads a batch of instrumentation messages to log chronicle using `{@InstrumentationBatch}`",
                    Methods = new[] { "POST = post `{@InstrumentationBatch}` for insertion into archive" },
                    RequestHeaders = new[] { API_DOC_HDR_ACCEPT_JSON },
                    ResponseHeaders = new[] { API_DOC_HDR_NO_CACHE },
                    RequestBody = "JSON representation of `{@InstrumentationBatch}`",
                    ResponseContent = "Api Change Result")]
    [ActionOnPost(Name = "batch"), AcceptsJson]
    [ChroniclePermission(ChronicleAccessLevel.Emit)]
    public async Task<object> PostDataBatch(InstrumentationBatch batch) => await SaveNewAsync(batch).ConfigureAwait(false);
  }
}
