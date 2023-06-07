﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Azos.Apps;
using Azos.Log;
using Azos.Serialization.JSON;

using Azos.Conf;
using Azos.Data.Access.MongoDb.Connector;

namespace Azos.Sky.Chronicle.Server
{
  /// <summary>
  /// Implements ILogChronicleStoreLogic and IInstrumentationChronicleStoreLogic using MongoDb
  /// </summary>
  public sealed class MongoChronicleStore : Daemon, ILogChronicleStoreImplementation, IInstrumentationChronicleStoreImplementation
  {
    public const string DEFAULT_DB = "sky_chron";

    public const string COLLECTION_LOG = "sky_log";
    public const string COLLECTION_INSTR = "sky_ins";

    public const int MAX_FETCH_DOC_COUNT = 8 * 1024;
    public const int MAX_INSERT_DOC_COUNT = 8 * 1024;
    public const int FETCH_BY_LOG = 128;

    public MongoChronicleStore(IApplication application) : base(application) { }
    public MongoChronicleStore(IApplicationComponent parent) : base(parent) { }

    private Database m_LogDb;
    private Database m_InstrDb;


    [Config]
    private string m_CsLogDatabase;

    [Config]
    private string m_CsInstrDatabase;

    public override string ComponentLogTopic => CoreConsts.INSTRUMENTATION_TOPIC;


    protected override void DoStart()
    {
      base.DoStart();

      m_LogDb = App.GetMongoDatabaseFromConnectString(m_CsLogDatabase, DEFAULT_DB);
      m_InstrDb = App.GetMongoDatabaseFromConnectString(m_CsInstrDatabase, DEFAULT_DB);
    }


    public Task<IEnumerable<Message>> GetAsync(LogChronicleFilter filter) => Task.FromResult(get(filter));
    private IEnumerable<Message> get(LogChronicleFilter filter)
    {
      filter.NonNull(nameof(filter));
      var totalCount = Math.Min(filter.PagingCount <= 0 ? FETCH_BY_LOG : filter.PagingCount, MAX_FETCH_DOC_COUNT);

      if (!Running) return Enumerable.Empty<Message>();

      var result = new List<Message>(totalCount);
      var cLog = m_LogDb[COLLECTION_LOG];

      var query = LogFilterQueryBuilder.BuildLogFilterQuery(filter);
      using (var cursor = cLog.Find(query, filter.PagingStartIndex, FETCH_BY_LOG))
      {
        int i = 0;
        foreach (var bdoc in cursor)
        {

          var msg = this.DontLeak(() => BsonConvert.FromBson(bdoc), "Bson conv error `BsonConvert.FromBson(doc)`: ", nameof(GetAsync));
          if (msg == null) continue;
          result.Add(msg);

          if (++i > totalCount || !Running) break;
        }
      }

      return result;
    }

    public Task WriteAsync(LogBatch data)
    {
      if (!Running) return Task.CompletedTask;

      var toSend = data.NonNull(nameof(data))
                       .Data.NonNull(nameof(data))
                       .Where(m => m != null && !m.Gdid.IsZero);


      var cLog = m_LogDb[COLLECTION_LOG];

      int i = 0;
      using(var errors = new ErrorLogBatcher(App.Log){Type = MessageType.Critical, From = this.ComponentLogFromPrefix + nameof(WriteAsync), Topic = ComponentLogTopic})
      foreach (var batch in toSend.BatchBy(0xf))
      {
        if (!Running) break;

        try
        {
          //#872 05302023 DKh Refactoring
          var bsons = batch.Select(msg =>
                                  {
                                    try
                                    {
                                      return BsonConvert.ToBson(msg);
                                    }
                                    catch(Exception conversionError)
                                    {
                                      errors.Add(conversionError);
                                    }
                                    return null;
                                  })
                           .Where(bson => bson != null)
                           .ToArray();

          i += bsons.Length;
          if (i > MAX_INSERT_DOC_COUNT)
          {
            WriteLog(MessageType.Critical, nameof(WriteAsync), "LogBatch exceeds max allowed count of {0}. The rest discarded".Args(MAX_INSERT_DOC_COUNT));
            break;
          }

          var result = cLog.Insert(bsons);
          if (result.WriteErrors != null)
          {
            result.WriteErrors.ForEach(we => errors.Add(new MongoDbConnectorServerException(we.Message)));
          }
        }
        catch(Exception genError)
        {
          errors.Add(genError);
        }
      }
      return Task.CompletedTask;
    }

    public Task<IEnumerable<JsonDataMap>> GetAsync(InstrumentationChronicleFilter filter)
    {
      throw new NotImplementedException();
    }

    public Task WriteAsync(InstrumentationBatch data)
    {
      throw new NotImplementedException();
    }
  }
}
