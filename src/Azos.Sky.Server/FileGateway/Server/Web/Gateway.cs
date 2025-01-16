﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Linq;
using System.Threading.Tasks;

using Azos.Apps.Injection;
using Azos.Data;
using Azos.Security.FileGateway;
using Azos.Wave.Mvc;

namespace Azos.Sky.FileGateway.Server.Web
{
  [NoCache]
  [FileGatewayPermission]
  [ApiControllerDoc(
    BaseUri = "/file/gateway",
    Connection = "default/keep alive",
    Title = "File Gateway",
    Authentication = "Token/Default",
    Description = "Provides REST API for working with remote files",
    TypeSchemas = new[]{typeof(FileGatewayPermission) }
  )]
  [Release(ReleaseType.Preview, 2023, 07, 07, "Initial Release", Description = "First release of API")]
  public class Gateway : ApiProtocolController
  {
    [Inject]
    IFileGatewayLogic m_Logic;

    [ApiEndpointDoc(Title = "Systems",
                    Uri = "systems",
                    Description = "Gets list of systems",
                    Methods = new[] { "GET = gets list of systems" },
                    RequestHeaders = new[] { API_DOC_HDR_ACCEPT_JSON },
                    ResponseHeaders = new[] { API_DOC_HDR_NO_CACHE },
                    ResponseContent = "JSON enumerable of `{@Atom}`",
                    TypeSchemas = new[] { typeof(Atom) })]
    [ActionOnGet(Name = "systems"), AcceptsJson]
    public async Task<object> GetSystems() => GetLogicResult(await m_Logic.GetSystemsAsync().ConfigureAwait(false));

    [ApiEndpointDoc(Title = "Volumes",
                    Uri = "volumes",
                    Description = "Gets list of volumes per system",
                    Methods = new[] { "GET = gets list of volumes" },
                    RequestQueryParameters = new[] { "system = Atom system identifier" },
                    RequestHeaders = new[] { API_DOC_HDR_ACCEPT_JSON },
                    ResponseHeaders = new[] { API_DOC_HDR_NO_CACHE },
                    ResponseContent = "JSON enumerable of `{@Atom}`",
                    TypeSchemas = new[] { typeof(Atom) })]
    [ActionOnGet(Name = "volumes"), AcceptsJson]
    public async Task<object> GetVolumes(Atom system) => GetLogicResult(await m_Logic.GetVolumesAsync(system).ConfigureAwait(false));


    [ApiEndpointDoc(Title = "Get Item List",
                    Uri = "item-list",
                    Description = "Gets a list of `{@ItemInfo}`",
                    Methods = new[] { "GET = {path: EntityId, recurse: bool}" },
                    RequestHeaders = new[] { API_DOC_HDR_ACCEPT_JSON },
                    ResponseHeaders = new[] { API_DOC_HDR_NO_CACHE },
                    RequestBody = "JSON map {path: EntityId, recurse: bool}`",
                    ResponseContent = "JSON enumerable of `{@ItemInfo}`",
                    TypeSchemas = new[] { typeof(EntityId), typeof(ItemInfo) })]
    [ActionOnGet(Name = "item-list"), AcceptsJson]
    public async Task<object> GetItemList(EntityId path, bool recurse = false)
      => GetLogicResult(await m_Logic.GetItemListAsync(path, recurse).ConfigureAwait(false));


    [ApiEndpointDoc(Title = "Get ItemInfo",
                    Uri = "item",
                    Description = "Gets `{@ItemInfo}` for the specified path",
                    Methods = new[] { "GET = gets item info by path" },
                    RequestQueryParameters = new[] { "path = EntityId path" },
                    RequestHeaders = new[] { API_DOC_HDR_ACCEPT_JSON },
                    ResponseHeaders = new[] { API_DOC_HDR_NO_CACHE },
                    ResponseContent = "JSON enumerable of `{@ItemInfo}`",
                    TypeSchemas = new[] { typeof(EntityId), typeof(ItemInfo) })]
    [ActionOnGet(Name = "item"), AcceptsJson]
    public async Task<object> GetItemInfo(EntityId path) => GetLogicResult(await m_Logic.GetItemInfoAsync(path).ConfigureAwait(false));


    [ApiEndpointDoc(Title = "Create Directory",
                    Uri = "directory",
                    Description = "Creates directory `{@ItemInfo}`",
                    Methods = new[] { "POST = {path: EntityId}" },
                    RequestHeaders = new[] { API_DOC_HDR_ACCEPT_JSON },
                    ResponseHeaders = new[] { API_DOC_HDR_NO_CACHE },
                    RequestBody = "JSON map {path: EntityId}`",
                    ResponseContent = "JSON for created directory `{@ItemInfo}`",
                    TypeSchemas = new[] { typeof(EntityId), typeof(ItemInfo) })]
    [ActionOnPost(Name = "directory"), AcceptsJson]
    public async Task<object>CreateDirectory(EntityId path)
      => GetLogicResult(await m_Logic.CreateDirectoryAsync(path).ConfigureAwait(false));

    [ApiEndpointDoc(Title = "Create File",
                  Uri = "file",
                  Description = "Creates file `{@ItemInfo}`",
                  Methods = new[] { "POST = {path: EntityId, mode: CreateMode, offset: long, content: byte[]}" },
                  RequestHeaders = new[] { API_DOC_HDR_ACCEPT_JSON },
                  ResponseHeaders = new[] { API_DOC_HDR_NO_CACHE },
                  RequestBody = "JSON map {path: EntityId, mode: CreateMode, offset: long, content: byte[]}`",
                  ResponseContent = "JSON for created file `{@ItemInfo}`",
                  TypeSchemas = new[] { typeof(EntityId), typeof(ItemInfo) })]
    [ActionOnPost(Name = "file"), AcceptsJson]
    public async Task<object> CreateFile(EntityId path, CreateMode mode, long offset, byte[] content)
      => GetLogicResult(await m_Logic.CreateFileAsync(path, mode, offset, content).ConfigureAwait(false));

    [ApiEndpointDoc(Title = "Upload File Chunk",
                  Uri = "file",
                  Description = "Upload file chunk `{@ItemInfo}`",
                  Methods = new[] { "PUT = {path: EntityId, offset: long, content: byte[]}" },
                  RequestHeaders = new[] { API_DOC_HDR_ACCEPT_JSON },
                  ResponseHeaders = new[] { API_DOC_HDR_NO_CACHE },
                  RequestBody = "JSON map {path: EntityId, offset: long, content: byte[]}`",
                  ResponseContent = "JSON for created file `{@ItemInfo}`",
                  TypeSchemas = new[] { typeof(EntityId), typeof(ItemInfo) })]
    [ActionOnPut(Name = "file"), AcceptsJson]
    public async Task<object> UploadChunk(EntityId path, long offset, byte[] content)
      => GetLogicResult(await m_Logic.UploadFileChunkAsync(path, offset, content).ConfigureAwait(false));


    [ApiEndpointDoc(Title = "Download file chunk",
                    Uri = "file",
                    Description = "Gets file chunk for the specified path",
                    Methods = new[] { "GET = gets file chunk" },
                    RequestQueryParameters = new[] { "path = EntityId path", "offset = Long offset", "size = int size" },
                    RequestHeaders = new[] { API_DOC_HDR_ACCEPT_JSON },
                    ResponseHeaders = new[] { API_DOC_HDR_NO_CACHE },
                    ResponseContent = "JSON map {data: byte[], eof: bool}",
                    TypeSchemas = new[] { typeof(EntityId), typeof(ItemInfo) })]
    [ActionOnGet(Name = "file"), AcceptsJson]
    public async Task<object> DownloadFileChunk(EntityId path, long offset, int size)
    {
      var (data, eof) = await m_Logic.DownloadFileChunkAsync(path, offset, size).ConfigureAwait(false);

     return GetLogicResult(new{ data, eof });
    }

    [ApiEndpointDoc(Title = "Delete Item",
                  Uri = "item",
                  Description = "Delete item",
                  Methods = new[] { "DELETE = {path: EntityId}" },
                  RequestHeaders = new[] { API_DOC_HDR_ACCEPT_JSON },
                  ResponseHeaders = new[] { API_DOC_HDR_NO_CACHE },
                  RequestBody = "JSON map {path: EntityId}`",
                  ResponseContent = "JSON map {deleted: bool}",
                  TypeSchemas = new[] { typeof(EntityId) })]
    [ActionOnDelete(Name = "item"), AcceptsJson]
    public async Task<object> Delete(EntityId path)
      => GetLogicResult(new {deleted = await m_Logic.DeleteItemAsync(path).ConfigureAwait(false)});

    [ApiEndpointDoc(Title = "Rename Item",
                  Uri = "item-name",
                  Description = "Rename item",
                  Methods = new[] { "POST = {path: EntityId, newPath: string}" },
                  RequestHeaders = new[] { API_DOC_HDR_ACCEPT_JSON },
                  ResponseHeaders = new[] { API_DOC_HDR_NO_CACHE },
                  RequestBody = "JSON map {path: EntityId, newPath: string}`",
                  ResponseContent = "JSON map {renamed: bool}",
                  TypeSchemas = new[] { typeof(EntityId) })]
    [ActionOnPost(Name = "item-name"), AcceptsJson]
    public async Task<object> Rename(EntityId path, string newPath)
      => GetLogicResult(new { renamed = await m_Logic.RenameItemAsync(path, newPath).ConfigureAwait(false) });

  }
}
