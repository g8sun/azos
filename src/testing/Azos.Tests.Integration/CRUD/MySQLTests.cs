/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/



using System;
using System.Threading.Tasks;

using Azos.Apps;
using Azos.Data;
using Azos.Data.Access.MySql;
using Azos.Scripting;

using MySqlConnector;

namespace Azos.Tests.Integration.CRUD
{
  /// <summary>
  /// To perform tests below MySQL server instance is needed.
  /// Look at CONNECT_STRING constant
  /// </summary>
  [Runnable]
  public class MySQLTests
  {
        [Run("cnt=1")]
        [Run("cnt=10")]
        [Run("cnt=100")]
        [Run("cnt=500")]
        public void Parallel_Fiasco(int cnt)
        {
          var cstr = getConnectString();

          Console.WriteLine(cstr);

          Parallel.For(0, cnt, new ParallelOptions{ MaxDegreeOfParallelism=36}, (i) =>
          {
            using(var cnn = new MySqlConnection(cstr))
            {
             cnn.Open();
            // using(var tx = cnn.BeginTransaction())
               using(var cmd = cnn.CreateCommand())
               {
                 var a = i;
                 var b = i+980;
                 cmd.CommandText = "select SQL_NO_CACHE {0}+{1} as ZZZ, t.* from mysql.help_keyword t".Args(a, b);
               //  Thread.Sleep(Ambient.Random.NextScaledRandomInteger(1,50));
                 using(var reader = cmd.ExecuteReader())
                 {
                    Aver.IsTrue(reader.Read());
                    Aver.AreEqual(a+b, reader[0].AsInt());
                    var rc = 0;
                    while(reader.Read()) rc++;
                    Console.WriteLine("Count: "+rc);
                 }

              //   tx.Commit();
               }
            }
          });
        }



        [Run]
        public void ManualDS_QueryInsertQuery()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.QueryInsertQuery( store );
            }
        }

        [Run]
        public void ManualDS_ASYNC_QueryInsertQuery()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.ASYNC_QueryInsertQuery( store );
            }
        }

        [Run]
        public void ManualDS_QueryInsertQuery_TypedRow()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.QueryInsertQuery_TypedRow( store );
            }
        }

        [Run]
        public void ManualDS_QueryInsertQuery_TypedRowDerived()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.QueryInsertQuery_TypedRowDerived( store );
            }
        }


        [Run]
        public void ManualDS_QueryInsertQuery_DynamicRow()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.QueryInsertQuery_DynamicRow( store );
            }
        }


        [Run]
        public void ManualDS_InsertManyUsingLogChanges_TypedRow()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.InsertManyUsingLogChanges_TypedRow( store );
            }
        }

        [Run]
        public void ManualDS_ASYNC_InsertManyUsingLogChanges_TypedRow()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.ASYNC_InsertManyUsingLogChanges_TypedRow( store );
            }
        }


        [Run]
        public void ManualDS_InsertInTransaction_Commit_TypedRow()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.InsertInTransaction_Commit_TypedRow( store );
            }
        }

        [Run]
        public async Task ManualDS_ASYNC_InsertInTransaction_Commit_TypedRow()
        {
          // 06/23/2021 D
          //https://mysqlconnector.net/troubleshooting/connection-reuse/

          using (var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
          {
              store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
              clearAllTables();
              await TestLogic.ASYNC_InsertInTransaction_Commit_TypedRow( store );
          }
        }

        [Run]
        public void ManualDS_InsertInTransaction_Rollback_TypedRow()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.InsertInTransaction_Rollback_TypedRow( store );
            }
        }

        [Run]
        public void ManualDS_InsertThenUpdate_TypedRow()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.InsertThenUpdate_TypedRow( store );
            }
        }

        [Run]
        public async Task ManualDS_ASYNC_InsertThenUpdate_TypedRow()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                await TestLogic.ASYNC_InsertThenUpdate_TypedRow( store );
            }
        }


        [Run]
        public void ManualDS_InsertThenDelete_TypedRow()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.InsertThenDelete_TypedRow( store );
            }
        }

        [Run]
        public async Task ManualDS_ASYNC_InsertThenDelete_TypedRow()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                await TestLogic.ASYNC_InsertThenDelete_TypedRow( store );
            }
        }

        [Run]
        public void ManualDS_InsertThenUpsert_TypedRow()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.InsertThenUpsert_TypedRow( store );
            }
        }

        [Run]
        public async Task ManualDS_ASYNC_InsertThenUpsert_TypedRow()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                await TestLogic.ASYNC_InsertThenUpsert_TypedRow( store );
            }
        }

        [Run]
        public void ManualDS_GetSchemaAndTestVariousTypes()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.StringBool = false;
                store.FullGDIDS = false;
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.GetSchemaAndTestVariousTypes( store );
            }
        }

        [Run]
        public async Task ManualDS_ASYNC_GetSchemaAndTestVariousTypes()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.StringBool = false;
                store.FullGDIDS = false;
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                await TestLogic.ASYNC_GetSchemaAndTestVariousTypes( store );
            }
        }


        [Run]
        public void ManualDS_TypedRowTestVariousTypes()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.StringBool = false;
                store.FullGDIDS = false;
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.TypedRowTestVariousTypes( store );
            }
        }

        [Run]
        public void ManualDS_TypedRowTestVariousTypes_StrBool()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.StringBool = true; //<-------- NOTICE
                store.StringForTrue = "1";
                store.StringForFalse = "0";

                store.FullGDIDS = false;
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.TypedRowTestVariousTypes( store );
            }
        }



        [Run]
        public void ManualDS_TypedRowTest_FullGDID()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.StringBool = false;
                store.FullGDIDS = true;
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.TypedRowTest_FullGDID( store );
            }
        }


        [Run]
        public void ManualDS_GetSchemaAndTestFullGDID()
        {
            using(var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.StringBool = false;
                store.FullGDIDS = true;
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.GetSchemaAndTestFullGDID( store );
            }
        }

        [Run]
        public void ManualDS_InsertWithPredicate()
        {
            using (var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.InsertWithPredicate(store);
                clearAllTables();
            }
        }

        [Run]
        public void ManualDS_UpdateWithPredicate()
        {
            using (var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.InsertWithPredicate(store);
                clearAllTables();
            }
        }

        [Run]
        public void ManualDS_UpsertWithPredicate()
        {
            using (var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.UpsertWithPredicate(store);
                clearAllTables();
            }
        }


        [Run]
        public void ManualDS_Populate_OpenCursor()
        {
            using (var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                TestLogic.Populate_OpenCursor(store);
                clearAllTables();
            }
        }

        [Run]
        public async Task ManualDS_ASYNC_Populate_OpenCursor()
        {
            using (var store = new MySqlCanonicalDataStore(NOPApplication.Instance, getConnectString()))
            {
                store.QueryResolver.ScriptAssembly = SCRIPT_ASM;
                clearAllTables();
                await TestLogic.Populate_ASYNC_OpenCursor(store);
                clearAllTables();
            }
        }


        //===============================================================================================================================

        private const string CONNECT_STRING = "Server=localhost;Database=AzosTest;Uid=root;Pwd=thejake;";

        private const string SCRIPT_ASM = "Azos.Tests.Integration";


        private string getConnectString()  //todo read form ENV var in future
        {
          return CONNECT_STRING;
        }

        private void clearAllTables()
        {
          using(var cnn = new MySqlConnection(CONNECT_STRING))
          {
              cnn.Open();
              using(var cmd = cnn.CreateCommand())
              {
                cmd.CommandText = "TRUNCATE TBL_TUPLE; TRUNCATE TBL_PATIENT; TRUNCATE TBL_DOCTOR; TRUNCATE TBL_TYPES; TRUNCATE TBL_FULLGDID;";
                cmd.ExecuteNonQuery();
              }
          }

        }
  }
}
