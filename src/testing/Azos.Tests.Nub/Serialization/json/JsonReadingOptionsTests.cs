﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.Threading.Tasks;
using Azos.CodeAnalysis.Source;
using Azos.Conf;
using Azos.IO;
using Azos.Scripting;
using Azos.Serialization.JSON;
using Azos.Text;

namespace Azos.Tests.Nub.Serialization
{
  [Runnable]
  public class JsonReadingOptionsTests
  {
    [Run("depth1", @"
      json='{a0:{a1:{a2:{a3: 567}}}}'
      ecode='eGraphDepthLimit'
      pass{ max-depth=4 }
      fail{ max-depth=3 }")]

    [Run("depth2", @"
      json='{a0:[{a2:{a3: 567}}]}'
      ecode='eGraphDepthLimit'
      pass{ max-depth=4 }
      fail{ max-depth=3 }")]

    [Run("maxlen", @"
      json='{a: 1, b: 2, c: 3, ""long"": ""value"", arr: [null, null,null, null, null, null,null, null,null, null,null, null,null, null]}'
      pass{ max-char-length=150 }
      fail{ max-char-length=100 }")]

    [Run("maxobjects", @"
      json='{a: 1, b: {}, c: { d: {}}}'
      pass{ max-objects=20 }
      fail{ max-objects=2 }")]

    [Run("maxarrays", @"
      json='{a: 1, b: [1,2,3], c: [ [ ] ]}'
      pass{ max-arrays=20 }
      fail{ max-arrays=2 }")]

    [Run("maxobjitems", @"
      json='{a: 1, b: 2, c: 3, d: 4, e: 5}'
      pass{ max-object-items=20 }
      fail{ max-object-items=2 }")]

    [Run("maxarrayitems", @"
      json='{a: 1, b: [1,2,null,4,5,6]}'
      pass{ max-array-items=8 }
      fail{ max-array-items=5 }")]

    [Run("maxkeylength", @"
      json='{a: 1, ThisKeyNameIsVeryLong: 2}'
      pass{ max-key-length=25 }
      fail{ max-key-length=20 }")]

    [Run("maxstringlength", @"
      json='{a: 1, b: ""This is a very very long and nasty string!""}'
      pass{ max-string-length=45 }
      fail{ max-string-length=40 }")]

    [Run("maxcommentlength", @"
      json='{a: 1, /* This is a very very long and nasty comment! */ b: 2}'
      pass{ max-comment-length=48 }
      fail{ max-comment-length=40 }")]

    [Run("!timeout-long", @"
      json=$'
        {
          a: 1, b: 2, c: 3, d: 4, v: [1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0, null, null, true, false],
          array: [ //================================================================================================================================
            [1, {a: -1, b: -2e-8, c: -3},{},1,2,3,4,5,6,7,8,9,10,1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1.3,2,3,4,-5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0],
            [2, {a: -2, b: -2e-8, c: -3},{},1,2,3,4,5,6,7,8,9,20,1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1,-2,-3,-4,-5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0],
            [3, {a: -3, b: -2e-8, c: -3},{},1,2,3,4,5,6,7,8,9,30,1,2,3,4,50,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1,2,3,4,-5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0],
            [4, {a: -4, b: -2e-8, c: -3},{},1,2,3,4,5,6,7,8,9,40,1,2,3,4,50,6,7,38,9,0,1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1,2.5647,3,4,5,6,7,8,9,0],
            [5, {a: -5, b: -2e-8, c: -3},{},1,2,3,4,5,6,7,8,9,50,1,2,3,4,50,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1,2,3,-4,5,6,7,8,9,0,1,2,3,4.677,5,6,7,8000,9,0],
            [6, {a: -6, b: -2e-8, c: -3},{},1,2,3,4,5,6,7,8,9,60,1,2,3,4,50,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0],
            [7, {a: -7, b: -2e-8, c: -3},{},1,2,3,4,5,6,7,8,9,70,1,2,3,4,50,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0],
            [8, {a: -8, b: -2e-8, c: -3},{},1,2,3,4,5,6,7,8,9,80,1,2,3,4,50,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1,2,-3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0]
          ]//================================================================================================================================
        }
      '
      msDelayFrom=15 msDelayTo=50
      chunkSizeFrom=1 chunkSizeTo=16
      pass{ timeout-ms=20000 buffer-size=100}
      fail{ timeout-ms=300 buffer-size=100}")]

    [Run("timeout", @"
      json=$'
        {
          a: 1, b: 2, c: 3, d: 4, v: [1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0, null, null, true, false],
          array: [ //================================================================================================================================
            [1, {a: -1, b: -2e-8, c: -3},{},1,2,3,4,5,6,7,8,9,10,1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1.3,2,3,4,-5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0],
            [2, {a: -2, b: -2e-8, c: -3},{},1,2,3,4,5,6,7,8,9,20,1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,1,-2,-3,-4,-5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0]
          ]//================================================================================================================================
        }
      '
      msDelayFrom=10 msDelayTo=10
      chunkSizeFrom=8 chunkSizeTo=8
      pass{ timeout-ms=8000 Xbuffer-size=100}
      fail{ timeout-ms=300 Xbuffer-size=100}")]
    public async Task TestCase(string json, IConfigSectionNode pass, IConfigSectionNode fail, string ecode = "eLimitExceeded", int msDelayFrom = 0, int msDelayTo = 0, int chunkSizeFrom = 0, int chunkSizeTo = 0)
    {
      using var lazyStream = StreamHookUse.CaseOfRandomAsyncStringReading(json, msDelayFrom, msDelayTo, chunkSizeFrom, chunkSizeTo);

      JsonDataMap got;
      #region Part 1 - Sync test
      lazyStream.Position = 0;
      got = JsonReader.Deserialize(lazyStream, ropt: null) as JsonDataMap;//pases with default/null options
      Aver.IsNotNull(got);
     // got.See();

      var optPass = new JsonReadingOptions(pass);
      lazyStream.Position = 0;
      got = JsonReader.Deserialize(lazyStream, ropt: optPass) as JsonDataMap;
      Aver.IsNotNull(got);

      var optFail = new JsonReadingOptions(fail){};
      try
      {
        lazyStream.Position = 0;
        got = JsonReader.Deserialize(lazyStream, ropt: optFail) as JsonDataMap;
        Aver.Fail("SYNC Cant be here");
      }
      catch (JSONDeserializationException jde)
      {
        "Sync Expected and got: {0}".SeeArgs(jde.ToMessageWithType());
        Aver.IsTrue(jde.Message.Contains(ecode), "Expected error code: " + ecode);
      }
      #endregion

      #region Part 2 - Async test
      lazyStream.Position = 0;
      got = await JsonReader.DeserializeAsync(lazyStream, ropt: null) as JsonDataMap;
      Aver.IsNotNull(got);
      // got.See();
      lazyStream.Position = 0;
      got = await JsonReader.DeserializeAsync(lazyStream, ropt: optPass) as JsonDataMap;
      Aver.IsNotNull(got);
      //  got.See();
      try
      {
        lazyStream.Position = 0;
        got = await JsonReader.DeserializeAsync(lazyStream, ropt: optFail) as JsonDataMap;
        Aver.Fail("ASYNC Cant be here");
      }
      catch (JSONDeserializationException jde)
      {
        "Async Expected and got: {0}".SeeArgs(jde.ToMessageWithType());
        Aver.IsTrue(jde.Message.Contains(ecode), "Expected error code: " + ecode);
      }
      #endregion
    }
  }
}
