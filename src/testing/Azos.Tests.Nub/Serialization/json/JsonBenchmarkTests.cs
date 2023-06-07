﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Threading.Tasks;

using Azos.Scripting;
using Azos.Time;
using Azos.IO;
using Azos.Serialization.JSON;
using System.IO;
using System.Linq;

namespace Azos.Tests.Nub.Serialization
{
  [Runnable]
  public class JsonBenchmarkTests : IRunnableHook
  {
    static JsonBenchmarkTests()
    {
      JsonReader.____SetReaderBackend(new Azos.Serialization.JSON.Backends.JazonReaderBackend());
    }

    public void Prologue(Runner runner, FID id)
    {
      JsonReader.____SetErrorSourceDisclosureLevel(1000);
    }

    public bool Epilogue(Runner runner, FID id, Exception error)
    {
      JsonReader.____SetErrorSourceDisclosureLevel(0);
      return false;
    }


    // [Run("cnt=250000 par=false")]
    // [Run("cnt=250000 par=true")]
    [Run("cnt=25000 par=false")]
    [Run("cnt=25000 par=true")]
    public void Test_Primitives(int cnt, bool par)
    {
      var json = @"{ a: 1, b: 2, c: true, d: null, e: false, f: false, g: true, i1: 3, i4: 2, i5: 125, i6: 18, f1: true, f2: true, f3: false,
       i10: 1,i11: 21,i12: 1,i13: 143,i14: 343, i15: 89, i16: 23,
       f10: true, f11: true, f12: false, f13: false, f14: false, f15: true, f16: true, f17: false, f18: true
      }";

      void body()
      {
        var got = JsonReader.DeserializeDataObject(json);
        Aver.IsNotNull(got);
      }

      var time = Timeter.StartNew();

      if (par)
        Parallel.For(0, cnt, i => body());
      else
        for (var i = 0; i < cnt; i++) body();

      time.Stop();

      "Did {0:n0} in {1:n1} sec at {2:n0} ops/sec".SeeArgs(cnt, time.ElapsedSec, cnt / time.ElapsedSec);
    }

    //[Run("cnt=250000 par=false")]
    //[Run("cnt=250000 par=true")]
    [Run("cnt=25000 par=false")]
    [Run("cnt=25000 par=true")]
    public void Test_SimpleObject(int cnt, bool par)
    {
      var json=@"{ a: 1, b: ""something"", c: null, d: {}, e: 23.7}";

      void body()
      {
        var got= JsonReader.DeserializeDataObject(json);
        Aver.IsNotNull(got);
      }

      var time = Timeter.StartNew();

      if (par)
       Parallel.For(0, cnt, i => body());
      else
       for(var i=0; i<cnt; i++) body();

      time.Stop();

      "Did {0:n0} in {1:n1} sec at {2:n0} ops/sec".SeeArgs(cnt, time.ElapsedSec, cnt / time.ElapsedSec);
    }

    //[Run("cnt=150000 par=false")]
    //[Run("cnt=150000 par=true")]
    [Run("cnt=15000 par=false")]
    [Run("cnt=15000 par=true")]
    public void Test_ModerateObject(int cnt, bool par)
    {
      var json = @"{ a: 1, b: true, c: 3, d: { a: ""qweqweqwewqeqw"", b: ""werwerwrwrwe6778687"" }, e: [ 1, 2, null, null, 3, 4, {a: 1}, {a: 2}] }";

      void body()
      {
        var got = JsonReader.DeserializeDataObject(json);
        Aver.IsNotNull(got);
      }

      var time = Timeter.StartNew();

      if (par)
        Parallel.For(0, cnt, i => body());
      else
        for (var i = 0; i < cnt; i++) body();

      time.Stop();

      "Did {0:n0} in {1:n1} sec at {2:n0} ops/sec".SeeArgs(cnt, time.ElapsedSec, cnt / time.ElapsedSec);
    }


    private const string COMPLEX_OBJECT_JSON = @"
      {
        ""start-marker"": ""My-magic-string-value-123456789"",
        a: 1,
        b: true,
        c: 3.79, csci: -3e-4,
        d: { a: ""qweqweqwewqeqw"", b: ""werwerwrwrwe6778687"" },
        e: [ 1, 2, null, null, 3, 4, {a: 1}, {a: 2}],
        ""person"": {""first-name"": ""oleg"", ""last-name"": ""ogurzov"", age: 127},
        ""doctor"": {""first-name"": ""Venni"", ""last-name"": ""Smekhov"", age: 27},
        ""patient"": {""first-name"": ""oleg"", ""last-name"": ""popov"", age: 98, prokofiev: { influence: ""shostakovich"", when: ""12 January 1980 3:47 pm""}},
        ""singer"": {""first-name"": ""Alla"", ""last-name"": ""Pugacheva"", age: 127},
        flag1: true,
        flag2: true,
        flag3: null,
        data: {array: [

                     { a: 1, b: true, c: 3, d: { a: ""qweqweqwewqeqw"", b: ""werwerwrwrwe6778687"" }, e: [ 1, 2, null, null, 3, 4, {a: 1}, {a: 2}] },
                     { a: -5, b: false, c: 23, d: { a: ""34 34 5343 34 qweqweqwewqeqw"", b: ""w687"" }, e: [ null, 2, null, null, 3, 4, {a: 1}, {a: 2}] }

                      ]},
        flag4: true,
        flag5: false,
        flag6: true,
        notes: [""aaa"", ""bbb"",""ddd"", null, null, [{a: 1},{},{},{},{},{},[],[2, true, true],[1,0],[]]],
        ""stop-marker"": ""My-magic-string-value-987654321"",
       }";





//[Run("cnt=95000 par=false")]
//[Run("cnt=95000 par=true")]
    [Run("cnt=9500 par=false")]
    [Run("cnt=9500 par=true")]
    public void Test_ComplexObject(int cnt, bool par)
    {
      var json = COMPLEX_OBJECT_JSON;

      void body()
      {
        var got = JsonReader.DeserializeDataObject(json) as JsonDataMap;
        Aver.IsNotNull(got);
        Aver.AreEqual("My-magic-string-value-123456789", got["start-marker"].ToString());
        Aver.AreEqual("My-magic-string-value-987654321", got["stop-marker"].ToString());
        Aver.AreEqual(-3e-4d, (double)got["csci"]);
      }

      var time = Timeter.StartNew();

      if (par)
        Parallel.For(0, cnt, i => body());
      else
        for (var i = 0; i < cnt; i++) body();

      time.Stop();

      "Did {0:n0} in {1:n1} sec at {2:n0} ops/sec".SeeArgs(cnt, time.ElapsedSec, cnt / time.ElapsedSec);
    }


//    [Run("cnt=95000 par=false")]
//    [Run("cnt=95000 par=true")]
    [Run("cnt=9500 par=false")]
    [Run("cnt=9500 par=true")]

    [Run("cnt=2000 par=true szf=1 szt=1000")]
    [Run("cnt=2000 par=true szf=1 szt=2")]
    [Run("cnt=2000 par=true szf=1 szt=3")]
    [Run("cnt=2000 par=true szf=1 szt=4")]
    [Run("cnt=2000 par=true szf=1 szt=5")]
    [Run("cnt=2000 par=true szf=1 szt=6")]
    [Run("cnt=2000 par=true szf=1 szt=7")]
    [Run("cnt=2000 par=true szf=1 szt=8")]
    [Run("cnt=2000 par=true szf=1 szt=9")]
    [Run("cnt=2000 par=true szf=1 szt=10")]
    [Run("cnt=2000 par=true szf=1 szt=11")]

    [Run("cnt=5000 par=true szf=1 szt=32")]
    [Run("cnt=5000 par=true szf=1 szt=64")]
    [Run("cnt=5000 par=true szf=1 szt=128")]

    [Run("cnt=5000 par=true szf=8 szt=32")]
    [Run("cnt=5000 par=true szf=16 szt=64")]
    [Run("cnt=5000 par=true szf=32 szt=128")]

    [Run("cnt=2000 par=true szf=1  szt=1")]
    [Run("cnt=2000 par=true szf=2  szt=2")]
    [Run("cnt=2000 par=true szf=3  szt=3")]
    [Run("cnt=2000 par=true szf=4  szt=4")]
    [Run("cnt=2000 par=true szf=5  szt=5")]
    [Run("cnt=2000 par=true szf=6  szt=6")]
    [Run("cnt=2000 par=true szf=7  szt=7")]
    [Run("cnt=2000 par=true szf=8  szt=8")]
    [Run("cnt=2000 par=true szf=9  szt=9")]
    [Run("cnt=2000 par=true szf=10 szt=10")]
    public async Task Test_ComplexObject_Async(int cnt, bool par, int szf=0, int szt=0)
    {
      var jsonBytes = StreamHookUse.EncodeStringToBufferNoBom(COMPLEX_OBJECT_JSON);

      async ValueTask body()
      {
        using var mock = StreamHookUse.CaseOfRandomAsyncBufferReading(jsonBytes, 0, 0, szf, szt);
        var got = await JsonReader.DeserializeAsync(mock).ConfigureAwait(false) as JsonDataMap;
        Aver.IsNotNull(got);
        Aver.AreEqual("My-magic-string-value-123456789", got["start-marker"].ToString());
        Aver.AreEqual("My-magic-string-value-987654321", got["stop-marker"].ToString());
        Aver.AreEqual(-3e-4d, (double)got["csci"]);
      }

      var time = Timeter.StartNew();

      if (par)
        await Parallel.ForEachAsync(Enumerable.Range(0, cnt), async (i,_) => await body().ConfigureAwait(false)).ConfigureAwait(false);
      else
        for (var i = 0; i < cnt; i++) await body().ConfigureAwait(false);

      time.Stop();

      "Did {0:n0} in {1:n1} sec at {2:n0} ops/sec".SeeArgs(cnt, time.ElapsedSec, cnt / time.ElapsedSec);
    }


  }
}

/*

RLEASE MODE  <================================================

Started 03/10/2020 22:19:10
Starting Azos.Tests.Nub::Azos.Tests.Nub.Serialization.JsonBenchmarkTests ...
  - Test_Primitives  {cnt=250000 par=false} Did 250,000 in 3.6 sec at 69,981 ops/sec
[OK]
  - Test_Primitives  {cnt=250000 par=true} [1] Did 250,000 in 0.5 sec at 544,563 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=false} Did 250,000 in 0.6 sec at 395,329 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=true} [1] Did 250,000 in 0.1 sec at 2,895,378 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=false} Did 150,000 in 0.8 sec at 184,504 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=true} [1] Did 150,000 in 0.1 sec at 1,278,717 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=false} Did 95,000 in 3.7 sec at 26,792 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=true} [1] Did 95,000 in 0.5 sec at 200,237 ops/sec
[OK]

 Started 03/08/2020 18:13:59
Starting Azos.Tests.Nub::Azos.Tests.Nub.Serialization.JsonBenchmarkTests ...
  - Test_Primitives  {cnt=250000 par=false} Did 250,000 in 4.7 sec at 53,997 ops/sec
[OK]
  - Test_Primitives  {cnt=250000 par=true} [1] Did 250,000 in 0.6 sec at 425,205 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=false} Did 250,000 in 0.9 sec at 315,253 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=true} [1] Did 250,000 in 0.1 sec at 2,143,198 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=false} Did 150,000 in 1.0 sec at 148,212 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=true} [1] Did 150,000 in 0.1 sec at 1,039,787 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=false} Did 95,000 in 4.6 sec at 21,120 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=true} [1] Did 95,000 in 0.6 sec at 167,312 ops/sec
[OK]

 Started 02/26/2020 21:09:57
Starting Azos.Tests.Nub::Azos.Tests.Nub.Serialization.JsonBenchmarkTests ...
  - Test_SimpleObject  {cnt=250000 par=false} Did 250,000 in 2.0 sec at 126,621 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=true} [1] Did 250,000 in 0.3 sec at 967,133 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=false} Did 150,000 in 2.4 sec at 61,476 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=true} [1] Did 150,000 in 0.3 sec at 487,722 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=false} Did 95,000 in 9.8 sec at 9,694 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=true} [1] Did 95,000 in 1.2 sec at 77,224 ops/sec
[OK]


DEBUG MODE  <================================================
Started 02/26/2020 21:09:04
Starting Azos.Tests.Nub::Azos.Tests.Nub.Serialization.JsonBenchmarkTests ...
  - Test_SimpleObject  {cnt=250000 par=false} Did 250,000 in 4.1 sec at 60,947 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=true} [1] Did 250,000 in 0.5 sec at 520,612 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=false} Did 150,000 in 5.4 sec at 27,704 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=true} [1] Did 150,000 in 0.6 sec at 232,450 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=false} Did 95,000 in 24.7 sec at 3,847 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=true} [1] Did 95,000 in 2.9 sec at 32,917 ops/sec
[OK]

DEBUG .NET 6 runtime debug (notice the 2x performance), ERROR SNIPPETS DISABLED
Started 02/27/2023 15:41:45
Starting Azos.Tests.Nub::Azos.Tests.Nub.Serialization.JsonBenchmarkTests ...
  - Test_Primitives  {cnt=250000 par=false} Did 250,000 in 9.8 sec at 25,541 ops/sec
[OK]
  - Test_Primitives  {cnt=250000 par=true} [1] Did 250,000 in 1.1 sec at 226,439 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=false} Did 250,000 in 1.6 sec at 156,741 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=true} [1] Did 250,000 in 0.2 sec at 1,152,178 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=false} Did 150,000 in 2.3 sec at 65,186 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=true} [1] Did 150,000 in 0.3 sec at 567,352 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=false} Did 95,000 in 10.3 sec at 9,217 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=true} [1] Did 95,000 in 1.2 sec at 81,919 ops/sec
[OK]

DEBUG .NET 6 runtime debug (notice the 2x performance), ERROR SNIPPETS ENABLED
Started 02/27/2023 15:45:55
Starting Azos.Tests.Nub::Azos.Tests.Nub.Serialization.JsonBenchmarkTests ...
  - Test_Primitives  {cnt=250000 par=false} Did 250,000 in 10.0 sec at 24,918 ops/sec
[OK]
  - Test_Primitives  {cnt=250000 par=true} [1] Did 250,000 in 1.1 sec at 218,764 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=false} Did 250,000 in 1.7 sec at 149,251 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=true} [1] Did 250,000 in 0.2 sec at 1,230,104 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=false} Did 150,000 in 2.4 sec at 61,543 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=true} [1] Did 150,000 in 0.3 sec at 520,135 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=false} Did 95,000 in 11.0 sec at 8,610 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=true} [1] Did 95,000 in 1.3 sec at 74,457 ops/sec
[OK]

RELEASE .NET 6 runtime debug (notice the 2x+ performance over .Net 4 Fx), ERROR SNIPPETS ENABLED
Started 02/27/2023 15:48:09
Starting Azos.Tests.Nub::Azos.Tests.Nub.Serialization.JsonBenchmarkTests ...
  - Test_Primitives  {cnt=250000 par=false} Did 250,000 in 3.8 sec at 66,041 ops/sec
[OK]
  - Test_Primitives  {cnt=250000 par=true} [1] Did 250,000 in 0.5 sec at 532,190 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=false} Did 250,000 in 0.6 sec at 400,614 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=true} [1] Did 250,000 in 0.1 sec at 3,139,903 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=false} Did 150,000 in 1.0 sec at 150,023 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=true} [1] Did 150,000 in 0.1 sec at 1,222,514 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=false} Did 95,000 in 4.3 sec at 21,949 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=true} [1] Did 95,000 in 0.5 sec at 181,708 ops/sec
[OK]
*/

/* ------------- 3/21/2023 Testing new Async StreamSource performance ----------------------------
DEBUG .NET 6
Started 03/21/2023 16:21:08
Starting Azos.Tests.Nub::Azos.Tests.Nub.Serialization.JsonBenchmarkTests ...
  - Test_Primitives  {cnt=250000 par=false} Did 250,000 in 10.2 sec at 24,417 ops/sec
[OK]
  - Test_Primitives  {cnt=250000 par=true} [1] Did 250,000 in 1.2 sec at 208,735 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=false} Did 250,000 in 1.7 sec at 145,241 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=true} [1] Did 250,000 in 0.2 sec at 1,213,153 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=false} Did 150,000 in 2.5 sec at 59,248 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=true} [1] Did 150,000 in 0.3 sec at 500,023 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=false} Did 95,000 in 12.0 sec at 7,902 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=true} [1] Did 95,000 in 1.3 sec at 71,701 ops/sec
[OK]
  - Test_ComplexObject_Async  {cnt=95000 par=false} Did 95,000 in 39.9 sec at 2,381 ops/sec
[OK]
  - Test_ComplexObject_Async  {cnt=95000 par=true} [1] Did 95,000 in 3.1 sec at 30,529 ops/sec
[OK]
... done JsonBenchmarkTests


RELEASE .Net 6
Started 03/21/2023 16:16:02
Starting Azos.Tests.Nub::Azos.Tests.Nub.Serialization.JsonBenchmarkTests ...
  - Test_Primitives  {cnt=250000 par=false} Did 250,000 in 3.3 sec at 76,104 ops/sec
[OK]
  - Test_Primitives  {cnt=250000 par=true} [1] Did 250,000 in 0.4 sec at 588,220 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=false} Did 250,000 in 0.6 sec at 427,322 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=true} [1] Did 250,000 in 0.1 sec at 2,569,999 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=false} Did 150,000 in 0.8 sec at 182,646 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=true} [1] Did 150,000 in 0.1 sec at 1,199,381 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=false} Did 95,000 in 4.1 sec at 23,235 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=true} [1] Did 95,000 in 0.5 sec at 177,183 ops/sec
[OK]
  - Test_ComplexObject_Async  {cnt=95000 par=false} Did 95,000 in 24.7 sec at 3,852 ops/sec
[OK]
  - Test_ComplexObject_Async  {cnt=95000 par=true} [1] Did 95,000 in 1.3 sec at 71,206 ops/sec
[OK]
... done JsonBenchmarkTests


------------------------------------------------------------------------------------------------*/

/* ------------- 3/30/2023 Testing after JsonReadingOptions limits ----------------------------
RELEASE .Net 6
Started 03/30/2023 19:51:23
Starting Azos.Tests.Nub::Azos.Tests.Nub.Serialization.JsonBenchmarkTests ...
  - Test_Primitives  {cnt=250000 par=false} Did 250,000 in 3.3 sec at 76,206 ops/sec
[OK]
  - Test_Primitives  {cnt=250000 par=true} [1] Did 250,000 in 0.4 sec at 575,449 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=false} Did 250,000 in 0.6 sec at 411,943 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=true} [1] Did 250,000 in 0.1 sec at 2,883,131 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=false} Did 150,000 in 0.8 sec at 176,881 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=true} [1] Did 150,000 in 0.1 sec at 1,164,434 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=false} Did 95,000 in 4.1 sec at 23,286 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=true} [1] Did 95,000 in 0.5 sec at 180,411 ops/sec
[OK]
  - Test_ComplexObject_Async  {cnt=95000 par=false} Did 95,000 in 24.9 sec at 3,822 ops/sec
[OK]
  - Test_ComplexObject_Async  {cnt=95000 par=true} [1] Did 95,000 in 1.3 sec at 72,440 ops/sec
... done JsonBenchmarkTests
------------------------------------------------------------------------------------------------*/

/* ------------- 4/06/2023 Testing after JsonReadingOptions limits ----------------------------
Started 04/06/2023 15:31:32
Starting Azos.Tests.Nub::Azos.Tests.Nub.Serialization.JsonBenchmarkTests ...
  - Test_Primitives  {cnt=250000 par=false} Did 250,000 in 3.5 sec at 71,799 ops/sec
[OK]
  - Test_Primitives  {cnt=250000 par=true} [1] Did 250,000 in 0.4 sec at 571,984 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=false} Did 250,000 in 0.6 sec at 412,602 ops/sec
[OK]
  - Test_SimpleObject  {cnt=250000 par=true} [1] Did 250,000 in 0.1 sec at 2,814,862 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=false} Did 150,000 in 0.9 sec at 167,945 ops/sec
[OK]
  - Test_ModerateObject  {cnt=150000 par=true} [1] Did 150,000 in 0.1 sec at 1,157,829 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=false} Did 95,000 in 4.2 sec at 22,715 ops/sec
[OK]
  - Test_ComplexObject  {cnt=95000 par=true} [1] Did 95,000 in 0.6 sec at 172,028 ops/sec
[OK]
  - Test_ComplexObject_Async  {cnt=95000 par=false} Did 95,000 in 25.1 sec at 3,785 ops/sec
[OK]
  - Test_ComplexObject_Async  {cnt=95000 par=true} [1] Did 95,000 in 1.4 sec at 69,523 ops/sec
... done JsonBenchmarkTests
------------------------------------------------------------------------------------------------*/