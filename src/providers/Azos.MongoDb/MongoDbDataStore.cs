/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Threading.Tasks;

using Azos.Apps;
using Azos.Conf;
using Azos.Serialization.BSON;

namespace Azos.Data.Access.MongoDb
{
  /// <summary>
  /// Implements MongoDB general data store that supports CRUD operations.
  /// This class IS thread-safe load/save/delete operations
  /// </summary>
  public class MongoDbDataStore : MongoDbDataStoreBase, ICrudDataStoreImplementation
  {
    #region CONSTS
        public const string SCRIPT_FILE_SUFFIX = ".mon.json";
    #endregion

    #region STATIC
    public static void CheckCRUDResult(Connector.CRUDResult result, string schema, string operation)
    {
      if (result.WriteErrors == null ||
          result.WriteErrors.Length == 0) return;

      var dump = Azos.Serialization.JSON.JsonWriter.Write(result.WriteErrors, Serialization.JSON.JsonWritingOptions.PrettyPrint);

      string kv = null;
      KeyViolationKind kvKind = KeyViolationKind.Unspecified;

      if (result.WriteErrors[0].Code == 11000)
      {
        kv = result.WriteErrors[0].Message;
        kvKind = kv.IndexOf("_id") > 0 ? KeyViolationKind.Primary : KeyViolationKind.Secondary;
      }


      throw new MongoDbDataAccessException(StringConsts.OP_CRUD_ERROR.Args(operation, schema, dump), kvKind, kv);
    }
    #endregion

    #region .ctor/.dctor

    public MongoDbDataStore(IApplication app) : base(app) => ctor();

    public MongoDbDataStore(IApplicationComponent director) : base(director) => ctor();

    private void ctor()
    {
      m_QueryResolver = new QueryResolver(this);
      m_Converter = new DataDocConverter();
    }

    protected override void Destructor()
    {
      DisposeAndNull(ref m_QueryResolver);
      base.Destructor();
    }

    #endregion

    #region Fields

    private QueryResolver    m_QueryResolver;
        private DataDocConverter m_Converter;

    #endregion


    #region ICRUDDataStore

    //WARNING!!!
    //The ASYNC versions of sync call now call TaskUtils.AsCompletedTask( sync_version )
    // which executes synchronously. Because of it CRUDOperationCallContext does not need to be captured
    // and passed along the async task chain.
    // Keep in mind: In future implementations, the true ASYNC versions of methods would need to capture
    // CRUDOperationCallContext and pass it along the call chain
    //todo: Add async support (socket-level)



    public virtual CrudTransaction BeginTransaction(IsolationLevel iso = IsolationLevel.ReadCommitted,
                                            TransactionDisposeBehavior behavior = TransactionDisposeBehavior.CommitOnDispose)
    {
        throw new MongoDbDataAccessException(StringConsts.OP_NOT_SUPPORTED_ERROR.Args("BeginTransaction", GetType().Name));
    }

    public virtual Task<CrudTransaction> BeginTransactionAsync(IsolationLevel iso = IsolationLevel.ReadCommitted,
                                                        TransactionDisposeBehavior behavior = TransactionDisposeBehavior.CommitOnDispose)
    {
        throw new MongoDbDataAccessException(StringConsts.OP_NOT_SUPPORTED_ERROR.Args("BeginTransactionAsync", GetType().Name));
    }

    public virtual bool SupportsTransactions
    {
        get { return false; }
    }

    public virtual bool SupportsTrueAsynchrony
    {
        get { return false; }
    }

    public virtual string ScriptFileSuffix
    {
        get { return SCRIPT_FILE_SUFFIX;}
    }

    public virtual CrudDataStoreType StoreType
    {
        get { return CrudDataStoreType.Document; }
    }


    public virtual Schema GetSchema(Query query)
    {
        if (query==null) return null;

        var db = GetDatabase();

        var handler = QueryResolver.Resolve(query);
        return handler.GetSchema( new MongoDbCRUDQueryExecutionContext(this, db), query);
    }

    public virtual Task<Schema> GetSchemaAsync(Query query)
    {
        return TaskUtils.AsCompletedTask( () => this.GetSchema(query) );
    }

    public virtual List<RowsetBase> Load(params Query[] queries)
    {
        var db = GetDatabase();

        var result = new List<RowsetBase>();
        if (queries==null) return result;

        foreach(var query in queries)
        {
            var handler = QueryResolver.Resolve(query);
            var rowset = handler.Execute( new MongoDbCRUDQueryExecutionContext(this, db), query, false);
            result.Add( rowset );
        }

        return result;
    }

    public virtual Task<List<RowsetBase>> LoadAsync(params Query[] queries)
    {
        return TaskUtils.AsCompletedTask( () => this.Load(queries) );
    }

    public virtual RowsetBase LoadOneRowset(Query query)
    {
        return Load(query).FirstOrDefault();
    }

    public virtual Task<RowsetBase> LoadOneRowsetAsync(Query query)
    {
        return this.LoadAsync(query)
                    .ContinueWith( antecedent => antecedent.Result.FirstOrDefault());
    }

    public virtual Doc LoadOneDoc(Query query)
    {
        RowsetBase rset = null;
        rset = Load(query).FirstOrDefault();

        if (rset!=null) return rset.FirstOrDefault();
        return null;
    }

    public virtual Task<Doc> LoadOneDocAsync(Query query)
    {
        return this.LoadAsync(query)
                    .ContinueWith( antecedent =>
                    {
                      RowsetBase rset = antecedent.Result.FirstOrDefault();
                      if (rset!=null) return rset.FirstOrDefault();
                      return null;
                    });
    }

    public virtual Cursor OpenCursor(Query query)
    {
        var db = GetDatabase();

        var handler = QueryResolver.Resolve(query);
        var context = new MongoDbCRUDQueryExecutionContext(this, db);
        var result = handler.OpenCursor( context, query);
        return result;
    }

    public virtual Task<Cursor> OpenCursorAsync(Query query)
    {
      return TaskUtils.AsCompletedTask( () => this.OpenCursor(query) );
    }


    public virtual int Save(params RowsetBase[] rowsets)
    {
        if (rowsets==null) return 0;

        var db = GetDatabase();

        var affected = 0;

        foreach(var rset in rowsets)
        {
            foreach(var change in rset.Changes)
            {
                switch(change.ChangeType)
                {
                    case DocChangeType.Insert: affected += DoInsert(db, change.Doc); break;
                    case DocChangeType.Update: affected += DoUpdate(db, change.Doc, change.Key); break;
                    case DocChangeType.Upsert: affected += DoUpsert(db, change.Doc); break;
                    case DocChangeType.Delete: affected += DoDelete(db, change.Doc, change.Key); break;
                }
            }
        }

        return affected;
    }

    public virtual Task<int> SaveAsync(params RowsetBase[] rowsets)
    {
        return TaskUtils.AsCompletedTask( () => this.Save(rowsets) );
    }

    public virtual int Insert(Doc row, FieldFilterFunc filter = null)
    {
        var db = GetDatabase();
        return DoInsert(db, row, filter);
    }

    public virtual Task<int> InsertAsync(Doc row, FieldFilterFunc filter = null)
    {
        return TaskUtils.AsCompletedTask( () => this.Insert(row, filter) );
    }

    public virtual int Upsert(Doc row, FieldFilterFunc filter = null)
    {
        var db = GetDatabase();
        return DoUpsert(db, row, filter);
    }

    public virtual Task<int> UpsertAsync(Doc row, FieldFilterFunc filter = null)
    {
        return TaskUtils.AsCompletedTask( () => this.Upsert(row, filter) );
    }

    public virtual int Update(Doc row, IDataStoreKey key = null, FieldFilterFunc filter = null)
    {
        var db = GetDatabase();
        return DoUpdate(db, row, key, filter);
    }

    public virtual Task<int> UpdateAsync(Doc row, IDataStoreKey key = null, FieldFilterFunc filter = null)
    {
        return TaskUtils.AsCompletedTask( () => this.Update(row, key, filter) );
    }

    public int Delete(Doc row, IDataStoreKey key = null)
    {
        var db = GetDatabase();
        return DoDelete(db, row, key);
    }

    public virtual Task<int> DeleteAsync(Doc row, IDataStoreKey key = null)
    {
        return TaskUtils.AsCompletedTask( () => this.Delete(row, key) );
    }

    public virtual Doc Execute(Query query)
    {
        if (query==null) return null;
        var db = GetDatabase();
        var handler = QueryResolver.Resolve(query);
        return handler.ExecuteProcedure( new MongoDbCRUDQueryExecutionContext(this, db), query);
    }

    public virtual Task<Doc> ExecuteAsync(Query query)
    {
        return TaskUtils.AsCompletedTask(() => this.Execute(query));
    }

    public CrudQueryHandler MakeScriptQueryHandler(QuerySource querySource)
    {
        return new MongoDbCRUDScriptQueryHandler(this, querySource);
    }

    public ICrudQueryResolver QueryResolver
    {
        get { return m_QueryResolver; }
    }
    #endregion


    #region Protected

    public DataDocConverter Converter{ get{return m_Converter;} }

    public override void Configure(IConfigSectionNode node)
    {
        m_QueryResolver.Configure(node);
        m_Converter.Configure(node);
        base.Configure(node);
    }

    protected internal string GetCollectionName(Schema schema)
    {
      string tableName = schema.Name;

      if (schema.TypedDocType!=null)
        tableName = schema.TypedDocType.Name;//without namespace

      var tableAttr = schema.GetSchemaAttrForTarget(TargetName);
      if (tableAttr!=null && tableAttr.Name.IsNotNullOrWhiteSpace()) tableName = tableAttr.Name;
      return tableName;
    }


    protected virtual int DoInsert(Connector.Database db, Doc row, FieldFilterFunc filter = null)
    {
      var doc = convertDocToBSONDocumentWith_ID(row, "insert", filter);

      var tname = GetCollectionName(row.Schema);

      var collection = db[tname];

      var result = collection.Insert(doc);

      CheckCRUDResult(result, row.Schema.Name, "insert");

      return result.TotalDocumentsAffected;
    }

    protected virtual int DoUpsert(Connector.Database db, Doc row, FieldFilterFunc filter = null)
    {
      var doc = convertDocToBSONDocumentWith_ID(row, "upsert", filter);

      var tname = GetCollectionName(row.Schema);

      var collection = db[tname];

      var result = collection.Save(doc);

      CheckCRUDResult(result, row.Schema.Name, "upsert");

      return result.TotalDocumentsAffected;
    }

    protected virtual int DoUpdate(Connector.Database db, Doc row, IDataStoreKey key, FieldFilterFunc filter = null)
    {
      var doc = convertDocToBSONDocumentWith_ID(row, "update", filter);
      var _id = doc[Connector.Protocol._ID];

      doc.Delete(Connector.Protocol._ID);
      if (doc.Count == 0) return 0; // nothing to update

      //20160212 spol
      if (filter != null)
      {
        var wrapDoc = new BSONDocument();
        wrapDoc.Set(new BSONDocumentElement(Connector.Protocol.SET, doc));
        doc = wrapDoc;
      }

      var tname = GetCollectionName(row.Schema);

      var collection = db[tname];

      var qry = new Connector.Query();
      qry.Set( _id );
      var upd = new Connector.UpdateEntry(qry, doc, false, false);

      var result = collection.Update( upd );

      CheckCRUDResult(result, row.Schema.Name, "update");

      return result.TotalDocumentsAffected;
    }

    protected virtual int DoDelete(Connector.Database db, Doc row, IDataStoreKey key)
    {
      var doc = convertDocToBSONDocumentWith_ID(row, "delete");

      var tname = GetCollectionName(row.Schema);

      var collection = db[tname];

      var qry = new Connector.Query();
      qry.Set( doc[Connector.Protocol._ID] );

      var result = collection.Delete( new Connector.DeleteEntry( qry, Connector.DeleteLimit.OnlyFirstMatch) );

      CheckCRUDResult(result, row.Schema.Name, "delete");

      return result.TotalDocumentsAffected;
    }
    #endregion

    #region .pvt
    private BSONDocument convertDocToBSONDocumentWith_ID(Doc doc, string operation, FieldFilterFunc filter = null)
    {
      var result = m_Converter.DataDocToBSONDocument(doc, this.TargetName, filter: filter);

      if (result[Connector.Protocol._ID]==null)
        throw new MongoDbDataAccessException(StringConsts.OP_ROW_NO_PK_ID_ERROR.Args(doc.Schema.Name, Connector.Protocol._ID, operation));

      return result;
    }
    #endregion
  }
}
