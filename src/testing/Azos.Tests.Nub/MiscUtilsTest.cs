/*<FILE_LICENSE>
* Azos (A to Z Application Operating System) Framework
* The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
* See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.IO;

using Azos.Scripting;
using static Azos.Aver.ThrowsAttribute;
using MatchType = Azos.Aver.ThrowsAttribute.MatchType;

namespace Azos.Tests.Nub
{
  [Runnable]
  public class MiscUtilsTest
  {
    [Run]
    public void ReadWriteBEShortTestStream()
    {
      var ms = new MemoryStream();
      ms.WriteBEShort(789);
      Aver.AreEqual(2, ms.Position);
      Aver.IsTrue((new byte[] { 0x03, 0x15 }).MemBufferEquals(ms.ToArray()));

      ms.Position = 0;
      Aver.AreEqual(789, ms.ReadBEShort());
    }

    [Run]
    public void ReadWriteLEShortTestStream()
    {
      var ms = new MemoryStream();
      ms.WriteLEShort(789);
      Aver.AreEqual(2, ms.Position);
      Aver.IsTrue((new byte[] { 21, 3 }).MemBufferEquals(ms.ToArray()));

      ms.Position = 0;
      Aver.AreEqual(789, ms.ReadLEShort());

      ms.Position = 0;
      ms.WriteLEUShort(789);
      Aver.AreEqual(2, ms.Position);
      Aver.IsTrue((new byte[] { 21, 3 }).MemBufferEquals(ms.ToArray()));
      ms.Position = 0;
      Aver.AreEqual(789, ms.ReadLEUShort());
    }

    [Run]
    public void ReadWriteBEShortTestArray()
    {
      var buf = new byte[2];
      buf.WriteBEShort(0, 789);
      Aver.IsTrue((new byte[] { 0x03, 0x15 }).MemBufferEquals(buf));

      var idx = 0;
      Aver.AreEqual(789, buf.ReadBEShort(ref idx));
      Aver.AreEqual(2, idx);
    }

    [Run]
    public void ReadWriteLEShortTestArray()
    {
      var buf = new byte[2];
      buf.WriteLEShort(0, 770);
      Aver.IsTrue((new byte[] { 2, 3 }).MemBufferEquals(buf));

      var idx = 0;
      Aver.AreEqual(770, buf.ReadLEShort(ref idx));
      Aver.AreEqual(2, idx);
    }

    [Run]
    public void ReadWriteBEInt32TestStream()
    {
      var ms = new MemoryStream();
      ms.WriteBEInt32(16909060);
      Aver.AreEqual(4, ms.Position);
      Aver.IsTrue((new byte[] { 1, 2, 3, 4 }).MemBufferEquals(ms.ToArray()));

      ms.Position = 0;
      Aver.AreEqual(16909060, ms.ReadBEInt32());
    }

    [Run]
    public void ReadWriteLEInt32TestStream()
    {
      var ms = new MemoryStream();
      ms.WriteLEInt32(16909060);
      Aver.AreEqual(4, ms.Position);
      Aver.IsTrue((new byte[] { 4, 3, 2, 1 }).MemBufferEquals(ms.ToArray()));

      ms.Position = 0;
      Aver.AreEqual(16909060, ms.ReadLEInt32());
    }

    [Run]
    public void ReadWriteBEInt32TestArray()
    {
      var buf = new byte[4];
      buf.WriteBEInt32(16909060);

      Aver.IsTrue((new byte[] { 1, 2, 3, 4 }).MemBufferEquals(buf));

      Aver.AreEqual(16909060, buf.ReadBEInt32());

      var idx = 0;
      Aver.AreEqual(16909060, buf.ReadBEInt32(ref idx));
      Aver.AreEqual(4, idx);
    }

    [Run]
    public unsafe void ReadWriteBEInt32TestBuffer()
    {
      var ptr = stackalloc byte[4];
      IOUtils.WriteBEUInt32(ptr, 0, 0xFACACA07);

      var idx = 0;
      Aver.AreEqual(0xFACACA07, IOUtils.ReadBEUInt32(ptr, ref idx));
      Aver.AreEqual(4, idx);
    }

    [Run]
    public void ReadWriteLEInt32TestArray()
    {
      var buf = new byte[4];
      buf.WriteLEInt32(67305985);

      Aver.IsTrue((new byte[] { 1, 2, 3, 4 }).MemBufferEquals(buf));

      Aver.AreEqual(67305985, buf.ReadLEInt32());

      var idx = 0;
      Aver.AreEqual(67305985, buf.ReadLEInt32(ref idx));
      Aver.AreEqual(4, idx);
    }

    [Run]
    public void ReadWriteBEInt64TestStream()
    {
      var ms = new MemoryStream();
      ms.WriteBEUInt64(0xFACACA07EBEDDAFE);
      Aver.AreEqual(8, ms.Position);
      Aver.IsTrue((new byte[] { 0xFA, 0xCA, 0xCA, 0x07, 0xEB, 0xED, 0xDA, 0xFE }).MemBufferEquals(ms.ToArray()));

      ms.Position = 0;
      Aver.AreEqual(0xFACACA07EBEDDAFE, ms.ReadBEUInt64());
      ms.Position = 0;
      Aver.AreEqual(0xFEDAEDEB07CACAFA, ms.ReadLEUInt64());
    }

    [Run]
    public void ReadWriteLEInt64TestStream()
    {
      var ms = new MemoryStream();
      ms.WriteLEUInt64(0xFACACA07EBEDDAFE);
      Aver.AreEqual(8, ms.Position);
      Aver.IsTrue((new byte[] { 0xFE, 0xDA, 0xED, 0xEB, 0x07, 0xCA, 0xCA, 0xFA }).MemBufferEquals(ms.ToArray()));

      ms.Position = 0;
      Aver.AreEqual(0xFACACA07EBEDDAFE, ms.ReadLEUInt64());
    }

    [Run]
    public void ReadWriteBEInt64TestArray()
    {
      var buf = new byte[8];
      buf.WriteBEUInt64(0xFACACA07EBEDDAFE);

      Aver.IsTrue((new byte[] { 0xFA, 0xCA, 0xCA, 0x07, 0xEB, 0xED, 0xDA, 0xFE }).MemBufferEquals(buf));
      Aver.AreEqual(0xFACACA07EBEDDAFE, buf.ReadBEUInt64());
      var idx = 0;
      Aver.AreEqual(0xFACACA07EBEDDAFE, buf.ReadBEUInt64(ref idx));
      Aver.AreEqual(8, idx);
    }

    [Run]
    public unsafe void ReadWriteBEInt64TestBuffer()
    {
      var ptr = stackalloc byte[8];
      IOUtils.WriteBEUInt64(ptr, 0, 0xFACACA07EBEDDAFE);

      var idx = 0;
      Aver.AreEqual(0xFACACA07EBEDDAFE, IOUtils.ReadBEUInt64(ptr, ref idx));
      Aver.AreEqual(8, idx);
    }

    [Run]
    public void ReadWriteLEInt64TestArray()
    {
      var buf = new byte[8];
      buf.WriteLEUInt64(72057615580070401);

      Aver.IsTrue((new byte[] { 1, 2, 3, 4, 5, 0, 0, 1 }).MemBufferEquals(buf));
      Aver.IsTrue(72057615580070401 == buf.ReadLEUInt64());
      var idx = 0;
      Aver.IsTrue(72057615580070401 == buf.ReadLEUInt64(ref idx));
      Aver.AreEqual(8, idx);
    }

    [Run]
    public void StringLines()
    {
      var txt =
@"A,b,
c,d,e
f
";
      Aver.AreEqual("A,b,", txt.ReadLine());

      var lines = txt.SplitLines();

      Aver.AreEqual(4, lines.Length);
      Aver.AreEqual("A,b,", lines[0]);
      Aver.AreEqual("c,d,e", lines[1]);
      Aver.AreEqual("f", lines[2]);
      Aver.AreEqual("", lines[3]);
    }


    [Run]
    public void Type_FullNameWithExpandedGenericArgs1()
    {
      var t = typeof(List<string>);

      Aver.AreEqual("System.Collections.Generic.List<System.String>", t.FullNameWithExpandedGenericArgs(false));
      Aver.AreEqual("@System.@Collections.@Generic.@List<@System.@String>", t.FullNameWithExpandedGenericArgs(true));
      Aver.AreEqual("@System.@Collections.@Generic.@List<@System.@String>", t.FullNameWithExpandedGenericArgs());
    }

    [Run]
    public void Type_FullNameWithExpandedGenericArgs2()
    {
      var t = typeof(int?);

      Aver.AreEqual("System.Nullable<System.Int32>", t.FullNameWithExpandedGenericArgs(false));
      Aver.AreEqual("@System.@Nullable<@System.@Int32>", t.FullNameWithExpandedGenericArgs(true));
      Aver.AreEqual("@System.@Nullable<@System.@Int32>", t.FullNameWithExpandedGenericArgs());
    }

    [Run]
    public void Type_FullNameWithExpandedGenericArgs3()
    {
      var t = typeof(Dictionary<DateTime?, List<bool?>>);
      Aver.AreEqual("System.Collections.Generic.Dictionary<System.Nullable<System.DateTime>, System.Collections.Generic.List<System.Nullable<System.Boolean>>>", t.FullNameWithExpandedGenericArgs(false));
      Aver.AreEqual("@System.@Collections.@Generic.@Dictionary<@System.@Nullable<@System.@DateTime>, @System.@Collections.@Generic.@List<@System.@Nullable<@System.@Boolean>>>", t.FullNameWithExpandedGenericArgs(true));
      Aver.AreEqual("@System.@Collections.@Generic.@Dictionary<@System.@Nullable<@System.@DateTime>, @System.@Collections.@Generic.@List<@System.@Nullable<@System.@Boolean>>>", t.FullNameWithExpandedGenericArgs());
    }

    [Run]
    public void Type_FullNameWithExpandedGenericArgs4()
    {
      var t = typeof(DateTime);
      Aver.AreEqual("System.DateTime", t.FullNameWithExpandedGenericArgs(false));
      Aver.AreEqual("@System.@DateTime", t.FullNameWithExpandedGenericArgs(true));
      Aver.AreEqual("@System.@DateTime", t.FullNameWithExpandedGenericArgs());
    }


    internal class ClazzA
    {
      public struct StructB { }
      public class ClB { }
    }


    [Run]
    public void Type_FullNameWithExpandedGenericArgs5()
    {
      Aver.AreEqual("Azos.Tests.Nub.MiscUtilsTest.ClazzA.StructB", typeof(ClazzA.StructB).FullNameWithExpandedGenericArgs(false));
      Aver.AreEqual("@Azos.@Tests.@Nub.@MiscUtilsTest.@ClazzA.@StructB", typeof(ClazzA.StructB).FullNameWithExpandedGenericArgs(true));

      Aver.AreEqual("Azos.Tests.Nub.MiscUtilsTest.ClazzA.ClB", typeof(ClazzA.ClB).FullNameWithExpandedGenericArgs(false));
      Aver.AreEqual("@Azos.@Tests.@Nub.@MiscUtilsTest.@ClazzA.@ClB", typeof(ClazzA.ClB).FullNameWithExpandedGenericArgs(true));
    }

    [Run]
    public void Type_FullNestedTypeName()
    {
      Aver.AreEqual("MiscUtilsTest", typeof(MiscUtilsTest).FullNestedTypeName(false));
      Aver.AreEqual("@MiscUtilsTest", typeof(MiscUtilsTest).FullNestedTypeName(true));
      Aver.AreEqual("MiscUtilsTest.ClazzA", typeof(ClazzA).FullNestedTypeName(false));
      Aver.AreEqual("@MiscUtilsTest.@ClazzA", typeof(ClazzA).FullNestedTypeName(true));
      Aver.AreEqual("MiscUtilsTest.ClazzA.StructB", typeof(ClazzA.StructB).FullNestedTypeName(false));
      Aver.AreEqual("@MiscUtilsTest.@ClazzA.@StructB", typeof(ClazzA.StructB).FullNestedTypeName(true));
    }

    [Run]
    public void Type_DisplayNameWithExpandedGenericArgs1()
    {
      var t = typeof(List<string>);

      Aver.AreEqual("List<String>", t.DisplayNameWithExpandedGenericArgs());
    }

    [Run]
    public void Type_DisplayNameWithExpandedGenericArgs2()
    {
      var t = typeof(List<Dictionary<string, List<DateTime?>>>);

      Aver.AreEqual("List<Dictionary<String, List<Nullable<DateTime>>>>", t.DisplayNameWithExpandedGenericArgs());
    }

    [Run]
    public void Burmatographize1()
    {
      Aver.AreEqual("aDmi", "Dima".Burmatographize());
      Aver.AreEqual("aDmi", "Dima".Burmatographize(false));
      Aver.AreEqual("Daim", "Dima".Burmatographize(true));
    }

    [Run]
    public void Burmatographize2()
    {
      Aver.AreEqual("mDi", "Dim".Burmatographize());
      Aver.AreEqual("mDi", "Dim".Burmatographize(false));
      Aver.AreEqual("Dmi", "Dim".Burmatographize(true));
    }

    [Run]
    public void Burmatographize3()
    {
      Aver.AreEqual("iD", "Di".Burmatographize());
      Aver.AreEqual("iD", "Di".Burmatographize(false));
      Aver.AreEqual("Di", "Di".Burmatographize(true));
    }


    [Run]
    public void Burmatographize4()
    {
      Aver.AreEqual("D", "D".Burmatographize());
      Aver.AreEqual("D", "D".Burmatographize(false));
      Aver.AreEqual("D", "D".Burmatographize(true));
    }

    [Run]
    public void Burmatographize5()
    {
      var b = "Some.Assembly.Namespace1234.MyClassName".Burmatographize();
      b.See();
      Aver.AreEqual("eSmoamNes.sAaslsCeymMb.l4y3.2N1aemceasp", b);
    }

    [Run]
    public void ArgsTpl1()
    {
      var s = "My first name is {@FirstName@} and last name is {@LastName@}"
              .ArgsTpl(new { FirstName = "Alex", LastName = "Borisov" });
      s.See();
      Aver.AreEqual("My first name is Alex and last name is Borisov", s);
    }

    [Run]
    public void ArgsTpl2()
    {
      var s = "My name is {@Name@} and i make {@Salary@,10} an hour"
              .ArgsTpl(new { Name = "Someone", Salary = 125 });
      s.See();
      Aver.AreEqual("My name is Someone and i make        125 an hour", s);
    }

    [Run]
    public void MemBufferEquals_1()
    {
      var b1 = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1 };
      var b2 = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1 };

      Aver.IsTrue(b1.MemBufferEquals(b2));
    }

    [Run]
    public void MemBufferEquals_2()
    {
      var b1 = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1 };
      var b2 = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 2, 1 };

      Aver.IsFalse(b1.MemBufferEquals(b2));
    }

    [Run]
    public void MemBufferEquals_3()
    {
      var b1 = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1 };
      var b2 = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 1 };

      Aver.IsFalse(b1.MemBufferEquals(b2));
    }

    [Run]
    public void MemBufferEquals_4()
    {
      var b1 = new byte[] { };
      var b2 = new byte[] { };

      Aver.IsTrue(b1.MemBufferEquals(b2));
    }

    [Run]
    public void MemBufferEquals_5()
    {
      var b1 = new byte[] { };
      byte[] b2 = null;

      Aver.IsFalse(b1.MemBufferEquals(b2));
    }

    [Run]
    public void MemBufferEquals_6()
    {
      var b1 = new byte[] { 0 };
      var b2 = new byte[] { 0 };

      Aver.IsTrue(b1.MemBufferEquals(b2));

      b1 = new byte[] { 0, 1 };
      b2 = new byte[] { 0, 1 };

      Aver.IsTrue(b1.MemBufferEquals(b2));

      b1 = new byte[] { 0, 1, 88 };
      b2 = new byte[] { 0, 1, 88 };

      Aver.IsTrue(b1.MemBufferEquals(b2));

      b1 = new byte[] { 0, 1, 88, 99 };
      b2 = new byte[] { 0, 1, 88, 99 };

      Aver.IsTrue(b1.MemBufferEquals(b2));

      b1 = new byte[] { 0, 1, 88, 99, 22 };
      b2 = new byte[] { 0, 1, 88, 99, 22 };

      Aver.IsTrue(b1.MemBufferEquals(b2));

      b1 = new byte[] { 0, 1, 88, 99, 22, 3 };
      b2 = new byte[] { 0, 1, 88, 99, 22, 3 };

      Aver.IsTrue(b1.MemBufferEquals(b2));

      b1 = new byte[] { 0, 1, 88, 99, 22, 3, 0 };
      b2 = new byte[] { 0, 1, 88, 99, 22, 3, 0 };

      Aver.IsTrue(b1.MemBufferEquals(b2));

      b1 = new byte[] { 0, 1, 88, 99, 22, 3, 0, 44 };
      b2 = new byte[] { 0, 1, 88, 99, 22, 3, 0, 44 };

      Aver.IsTrue(b1.MemBufferEquals(b2));

      b1 = new byte[] { 0, 1, 88, 99, 22, 3, 0, 44, 122 };
      b2 = new byte[] { 0, 1, 88, 99, 22, 3, 0, 44, 122 };

      Aver.IsTrue(b1.MemBufferEquals(b2));

      b1 = new byte[] { 0, 1, 88, 99, 22, 3, 0, 44, 122, 121 };
      b2 = new byte[] { 0, 1, 88, 99, 22, 3, 0, 44, 122, 121 };

      Aver.IsTrue(b1.MemBufferEquals(b2));

      b1 = new byte[] { 0, 1, 88, 99, 22, 3, 0, 44, 122, 121, 7 };
      b2 = new byte[] { 0, 1, 88, 99, 22, 3, 0, 44, 122, 121, 7 };

      Aver.IsTrue(b1.MemBufferEquals(b2));
    }

    [Run]
    public void MemBufferEquals_Benchmark()
    {
      const int CNT = 10000000;

      var b1 = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 123, 2, 3, 45, 89, 3, 23, 143, 124, 44, 1, 7, 89, 32, 44, 33, 112 };
      var b2 = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 123, 2, 3, 45, 89, 3, 23, 143, 124, 44, 1, 7, 89, 32, 44, 33, 112 };


      var sw = System.Diagnostics.Stopwatch.StartNew();
      for (var i = 0; i < CNT; i++)
        Aver.IsTrue(b1.MemBufferEquals(b2));

      sw.Stop();

      "Fast Compared {0} in {1}ms at {2} ops/sec".SeeArgs(CNT, sw.ElapsedMilliseconds, CNT / (sw.ElapsedMilliseconds / 1000d));

      sw = System.Diagnostics.Stopwatch.StartNew();
      for (var i = 0; i < CNT; i++)
        Aver.IsTrue(compareSlow(b1, b2));
      sw.Stop();

      "Slow Compared {0} in {1}ms at {2} ops/sec".SeeArgs(CNT, sw.ElapsedMilliseconds, CNT / (sw.ElapsedMilliseconds / 1000d));
    }

    private bool compareSlow(byte[] b1, byte[] b2)
    {
      if (b1.Length != b2.Length) return false;
      for (var i = 0; i < b1.Length; i++)
        if (b1[i] != b2[i]) return false;

      return true;
    }

    [Run]
    public void URI_Join()
    {
      Aver.AreEqual("static/site/content", WebUtils.JoinPathSegs("static", "site", "content"));
      Aver.AreEqual("static/site/content", WebUtils.JoinPathSegs(" static", "  site  ", " content"));
      Aver.AreEqual("static/site/content", WebUtils.JoinPathSegs(" static", " \\ site  ", " // content"));
      Aver.AreEqual("static/site/content", WebUtils.JoinPathSegs(" static/", "//site  ", " // content"));
      Aver.AreEqual("static/site/content", WebUtils.JoinPathSegs(" static/", "/", "/site", "// content"));
      Aver.AreEqual("/static/site/content", WebUtils.JoinPathSegs("/static/", "/", "/site", "// content"));
      Aver.AreEqual("/static/site/content", WebUtils.JoinPathSegs("      /static/", "site", "\\content"));
      Aver.AreEqual("/static/site/content", WebUtils.JoinPathSegs(" ", null, "      /static/", "site", "\\content"));
      Aver.AreEqual("static/site/content", WebUtils.JoinPathSegs("static", null, "site", "", "", "\\content"));
    }

    [Run]
    public void ComposeURLQueryString_Empty()
    {
      Dictionary<string, object> pars = null;
      var result = WebUtils.ComposeURLQueryString(pars);
      Aver.AreEqual(string.Empty, result);

      pars = new Dictionary<string, object>();
      result = WebUtils.ComposeURLQueryString(pars);
      Aver.AreEqual(string.Empty, result);
    }

    [Run]
    public void ComposeURLQueryString_NullOrEmptyQueryParts()
    {
      var pars = new Dictionary<string, object>
      {
        { "name", null }
      };
      var result = WebUtils.ComposeURLQueryString(pars);
      Aver.AreEqual("name", result);

      pars = new Dictionary<string, object>
      {
        { "name", string.Empty },
      };
      result = WebUtils.ComposeURLQueryString(pars);
      Aver.AreEqual("name=", result);

      pars = new Dictionary<string, object>
      {
        { string.Empty, "ABBA" }
      };
      result = WebUtils.ComposeURLQueryString(pars);
      Aver.AreEqual(string.Empty, result);

      pars = new Dictionary<string, object>
      {
        { "name1", null },
        { "name2", string.Empty },
        { string.Empty, "ABBA" }
      };
      result = WebUtils.ComposeURLQueryString(pars);
      Aver.AreEqual("name1&name2=", result);

      pars = new Dictionary<string, object>
      {
        { "name1", string.Empty },
        { "name2", null },
        { string.Empty, "ABBA" },
        { "name3", "John" }
      };
      result = WebUtils.ComposeURLQueryString(pars);
      Aver.AreEqual("name1=&name2&name3=John", result);
    }

    [Run]
    public void ComposeURLQueryString_SpecSymbols()
    {
      var pars = new Dictionary<string, object> { { "name", "Petrov" }, { "age", 19 }, { "spec", @" -y~!@#$%^&*()_?><|';:\/=+" } };

      var result = WebUtils.ComposeURLQueryString(pars);
      Aver.AreEqual("name=Petrov&age=19&spec=%20-y%7E%21%40%23%24%25%5E%26%2A%28%29_%3F%3E%3C%7C%27%3B%3A%5C%2F%3D%2B", result);
    }

    [Run]
    public void ComposeURLQueryString_Types()
    {
      var pars = new Dictionary<string, object>
      {
        { "int", -257 },
        { "bool", true },
        { "double", 1.9D },
        { "string", "data&data" },
        { "dec", 23.45M },
        { "float", -12.34F }
      };

      var result = WebUtils.ComposeURLQueryString(pars);
      Aver.AreEqual("int=-257&bool=True&double=1.9&string=data%26data&dec=23.45&float=-12.34", result);
    }

    [Run]
    public void ComposeURLQueryString_UTF8()
    {
      var pars = new Dictionary<string, object>
      {
        { "eng", "Hello!" },
        { "jap", "こんにちは" },
        { "chi", "久有归天愿"},
        { "chi2", "你好" },
        { "fra", "Allô" },
        { "привет", "rus" },
        { "नमस्कार", "hind" }
      };

      var result = WebUtils.ComposeURLQueryString(pars);
      Aver.AreEqual("eng=Hello%21&jap=%E3%81%93%E3%82%93%E3%81%AB%E3%81%A1%E3%81%AF&chi=%E4%B9%85%E6%9C%89%E5%BD%92%E5%A4%A9%E6%84%BF&chi2=%E4%BD%A0%E5%A5%BD&fra=All%C3%B4&%D0%BF%D1%80%D0%B8%D0%B2%D0%B5%D1%82=rus&%E0%A4%A8%E0%A4%AE%E0%A4%B8%E0%A5%8D%E0%A4%95%E0%A4%BE%E0%A4%B0=hind", result);
    }

    [Run]
    public void ComposeURLQueryString_Mixed()
    {
      var pars = new Dictionary<string, object>
      {
        { "eng", "Hello!" },
        { "jap", null },
        { "chi", "久有归天愿"},
        { "chi2", 12 },
        { "", -123456 },
        { "привет", string.Empty },
        { "नमस्कार", null }
      };

      var result = WebUtils.ComposeURLQueryString(pars);
      Aver.AreEqual("eng=Hello%21&jap&chi=%E4%B9%85%E6%9C%89%E5%BD%92%E5%A4%A9%E6%84%BF&chi2=12&%D0%BF%D1%80%D0%B8%D0%B2%D0%B5%D1%82=&%E0%A4%A8%E0%A4%AE%E0%A4%B8%E0%A5%8D%E0%A4%95%E0%A4%BE%E0%A4%B0", result);
    }

    [Run]
    public void ComposeURLQueryString_PlusAndSpaces()
    {
      var pars = new Dictionary<string, object>
      {
        { "eng", "Hello Lenin!" },
        { "rus", "Привет Ленин!" }
      };

      var result = WebUtils.ComposeURLQueryString(pars);
      Aver.AreEqual("eng=Hello%20Lenin%21&rus=%D0%9F%D1%80%D0%B8%D0%B2%D0%B5%D1%82%20%D0%9B%D0%B5%D0%BD%D0%B8%D0%BD%21", result);
    }

    [Run]
    public void RoundToWeekDay()
    {
      var now = DateTime.Now;
      checkDayOfWeek(now, DayOfWeek.Sunday);
      checkDayOfWeek(now, DayOfWeek.Monday);
      checkDayOfWeek(now, DayOfWeek.Tuesday);
      checkDayOfWeek(now, DayOfWeek.Wednesday);
      checkDayOfWeek(now, DayOfWeek.Thursday);
      checkDayOfWeek(now, DayOfWeek.Friday);
      checkDayOfWeek(now, DayOfWeek.Saturday);
    }

    private void checkDayOfWeek(DateTime now, DayOfWeek dayOfWeek)
    {
      var day = now.RoundToWeekDay(dayOfWeek);
      var dt = (day - now.Date);
      Aver.IsTrue(day.DayOfWeek == dayOfWeek && (TimeSpan.FromDays(0) <= dt) && (dt <= TimeSpan.FromDays(6)));
    }

    [Run]
    public void RoundToNextWeekDay()
    {
      var now = DateTime.Now;
      checkNextDayOfWeek(now, DayOfWeek.Sunday);
      checkNextDayOfWeek(now, DayOfWeek.Monday);
      checkNextDayOfWeek(now, DayOfWeek.Tuesday);
      checkNextDayOfWeek(now, DayOfWeek.Wednesday);
      checkNextDayOfWeek(now, DayOfWeek.Thursday);
      checkNextDayOfWeek(now, DayOfWeek.Friday);
      checkNextDayOfWeek(now, DayOfWeek.Saturday);
    }

    [Run]
    public void EscapeURIStringWithPlus()
    {
      var goodURL = "https://shippo-delivery-east.s3.amazonaws.com/fff5ec643c2c44539e5a26940d29e917.pdf?Signature=UUd8Pyuki6EDp8RJ/JtEIcSm524=&Expires=1505468405&AWSAccessKeyId=AKIAJGLCC5MYLLWIG42A";
      var badURL = "https://shippo-delivery-east.s3.amazonaws.com/6dcf1e56f4fe49b892716393de92dd7e.pdf?Signature=/4iTy32xguuMX7Eba+5qc8TFCbs=&Expires=1505468476&AWSAccessKeyId=AKIAJGLCC5MYLLWIG42A";

      var escapedGoodURL = goodURL.EscapeURIStringWithPlus();
      var escapedBadURL = badURL.EscapeURIStringWithPlus();

      Aver.AreEqual(goodURL, escapedGoodURL);
      Aver.AreEqual("https://shippo-delivery-east.s3.amazonaws.com/6dcf1e56f4fe49b892716393de92dd7e.pdf?Signature=/4iTy32xguuMX7Eba%2B5qc8TFCbs=&Expires=1505468476&AWSAccessKeyId=AKIAJGLCC5MYLLWIG42A", escapedBadURL);
    }

    private void checkNextDayOfWeek(DateTime now, DayOfWeek dayOfWeek)
    {
      var day = now.RoundToNextWeekDay(dayOfWeek);
      var dt = (day - now.Date);
      Aver.IsTrue(day.DayOfWeek == dayOfWeek && (TimeSpan.FromDays(0) < dt) && (dt <= TimeSpan.FromDays(7)));
    }

    [Run]
    public void PackISO3CodeToInt()
    {
      var p = IOUtils.PackISO3CodeToInt("abc");
      Aver.AreEqual(0, (p & 0xff000000));
      Aver.AreEqual('C', (p & 0x00ff0000) >> 16);
      Aver.AreEqual('B', (p & 0x0000ff00) >> 8);
      Aver.AreEqual('A', (p & 0x000000ff) >> 0);

      p = IOUtils.PackISO3CodeToInt("us");
      Aver.AreEqual(0, (p & 0xff000000));
      Aver.AreEqual(0, (p & 0x00ff0000));
      Aver.AreEqual('S', (p & 0x0000ff00) >> 8);
      Aver.AreEqual('U', (p & 0x000000ff) >> 0);

      p = IOUtils.PackISO3CodeToInt("z");
      Aver.AreEqual(0, (p & 0xff000000));
      Aver.AreEqual(0, (p & 0x00ff0000));
      Aver.AreEqual(0, (p & 0x0000ff00));
      Aver.AreEqual('Z', (p & 0x000000ff) >> 0);
    }

    [Run]
    public void PackISO3CodeToInt_Null()
    {
      Aver.AreEqual(0, IOUtils.PackISO3CodeToInt(null));
    }

    [Run]
    public void PackISO3CodeToInt_Empty()
    {
      Aver.AreEqual(0, IOUtils.PackISO3CodeToInt(""));
      Aver.AreEqual(0, IOUtils.PackISO3CodeToInt("                    "));
    }

    [Run]
    [Aver.Throws(typeof(AzosException), Message = "iso>3", MsgMatch = MatchType.Contains)]
    public void PackISO3CodeToInt_Bad3()
    {
      IOUtils.PackISO3CodeToInt("1234");
    }

    [Run]
    public void UnpackISO3CodeFromInt()
    {
      var p = 0;
      Aver.IsNull(IOUtils.UnpackISO3CodeFromInt(p));

      p = IOUtils.PackISO3CodeToInt("abc");
      Aver.AreEqual("ABC", IOUtils.UnpackISO3CodeFromInt(p), StringComparison.Ordinal);

      p = IOUtils.PackISO3CodeToInt("Us");
      Aver.AreEqual("US", IOUtils.UnpackISO3CodeFromInt(p), StringComparison.Ordinal);

      p = IOUtils.PackISO3CodeToInt("Z");
      Aver.AreEqual("Z", IOUtils.UnpackISO3CodeFromInt(p), StringComparison.Ordinal);
    }

    [Run]
    public void GuidToNetworkByteOrder()
    {
      var guid = Guid.Parse("AECBF3B2-C90E-4F2D-B51C-4EBABECF4338");
      var std = guid.ToByteArray();

      Aver.AreEqual(0xB2, std[0]); // aver MSFT improper LE byte order
      Aver.AreEqual(0xAE, std[3]); // aver MSFT improper LE byte order

      var azos = guid.ToNetworkByteOrder();

      Aver.AreEqual(0xAE, azos[0]);
      Aver.AreEqual(0xCB, azos[1]);
      Aver.AreEqual(0xF3, azos[2]);
      Aver.AreEqual(0xB2, azos[3]);

      Aver.AreEqual(0xC9, azos[4]);
      Aver.AreEqual(0x0E, azos[5]);

      Aver.AreEqual(0x4F, azos[6]);
      Aver.AreEqual(0x2D, azos[7]);

      Aver.AreEqual(0xB5, azos[8]);
      Aver.AreEqual(0x1C, azos[9]);

      Aver.AreEqual(0x4E, azos[10]);
      Aver.AreEqual(0xBA, azos[11]);
      Aver.AreEqual(0xBE, azos[12]);
      Aver.AreEqual(0xCF, azos[13]);
      Aver.AreEqual(0x43, azos[14]);
      Aver.AreEqual(0x38, azos[15]);
    }

    [Run]
    public void GuidFromNetworkByteOrder()
    {
      var data = new byte[] { 0xAE, 0xCB, 0xF3, 0xB2,
                              0xC9, 0x0E,
                              0x4F, 0x2D,
                              0xB5, 0x1C,
                              0x4E, 0xBA, 0xBE, 0xCF, 0x43, 0x38, };

      var guid = data.GuidFromNetworkByteOrder();

      Aver.AreEqual(Guid.Parse("AECBF3B2-C90E-4F2D-B51C-4EBABECF4338"), guid);
    }

    [Run]
    public void GuidFromNetworkByteOrder_WithOffset()
    {
      var data = new byte[] { 0x00, 0x00, 0x00,
                              0xAE, 0xCB, 0xF3, 0xB2,
                              0xC9, 0x0E,
                              0x4F, 0x2D,
                              0xB5, 0x1C,
                              0x4E, 0xBA, 0xBE, 0xCF, 0x43, 0x38, };

      var guid = data.GuidFromNetworkByteOrder(3);

      Aver.AreEqual(Guid.Parse("AECBF3B2-C90E-4F2D-B51C-4EBABECF4338"), guid);
    }

    [Run]
    public void IsValidXMLName()
    {
      Aver.IsFalse(((string)null).IsValidXMLName());
      Aver.IsFalse("".IsValidXMLName());
      Aver.IsFalse("<".IsValidXMLName());
      Aver.IsFalse(">".IsValidXMLName());

      Aver.IsFalse("2a".IsValidXMLName());
      Aver.IsTrue("a2".IsValidXMLName());

      Aver.IsTrue("a".IsValidXMLName());
      Aver.IsTrue("b".IsValidXMLName());

      Aver.IsTrue("a-b".IsValidXMLName());
      Aver.IsTrue("a_b".IsValidXMLName());
    }

    [Run]
    public void AlignDailyMinutesTests_001()
    {
      var a = new DateTime(1980, 1, 1, 13, 45, 18, DateTimeKind.Local);
      var b = a.AlignDailyMinutes(15);
      var c = new DateTime(1980, 1, 1, 13, 45, 00, DateTimeKind.Local);
      Aver.AreEqual(c, b);

      b = a.AlignDailyMinutes(30);
      c = new DateTime(1980, 1, 1, 14, 00, 00, DateTimeKind.Local);
      Aver.AreEqual(c, b);
    }

    [Run]
    public void AlignDailyMinutesTests_002()
    {
      var a = new DateTime(1980, 1, 1, 13, 46, 18, DateTimeKind.Local);
      var b = a.AlignDailyMinutes(15);
      var c = new DateTime(1980, 1, 1, 14, 00, 00, DateTimeKind.Local);
      Aver.AreEqual(c, b);
    }

    [Run]
    public void AlignDailyMinutesTests_003()
    {
      var a = new DateTime(1980, 1, 1, 13, 59, 18, DateTimeKind.Local);
      var b = a.AlignDailyMinutes(15);
      var c = new DateTime(1980, 1, 1, 14, 00, 00, DateTimeKind.Local);
      Aver.AreEqual(c, b);
    }

    [Run]
    public void AlignDailyMinutesTests_004()
    {
      var a = new DateTime(1980, 1, 1, 14, 01, 18, DateTimeKind.Local);
      var b = a.AlignDailyMinutes(15);
      var c = new DateTime(1980, 1, 1, 14, 15, 00, DateTimeKind.Local);
      Aver.AreEqual(c, b);
    }

    [Run]
    public void AlignDailyMinutesTests_005()
    {
      var a = new DateTime(1980, 1, 1, 14, 14, 59, DateTimeKind.Local);
      var b = a.AlignDailyMinutes(15);
      var c = new DateTime(1980, 1, 1, 14, 15, 00, DateTimeKind.Local);
      Aver.AreEqual(c, b);
    }

    [Run]
    public void AlignDailyMinutesTests_005_utc()
    {
      var a = new DateTime(1980, 1, 1, 14, 14, 59, DateTimeKind.Utc);
      var b = a.AlignDailyMinutes(15);
      var c = new DateTime(1980, 1, 1, 14, 15, 00, DateTimeKind.Utc);
      Aver.AreEqual(c, b);
    }

    [Run]
    public void AlignDailyMinutesTests_006()
    {
      var a = new DateTime(1980, 1, 1, 14, 15, 59, DateTimeKind.Local);
      var b = a.AlignDailyMinutes(15);
      var c = new DateTime(1980, 1, 1, 14, 15, 00, DateTimeKind.Local);
      Aver.AreEqual(c, b);
    }

    [Run]
    public void AlignDailyMinutesTests_006_utc()
    {
      var a = new DateTime(1980, 1, 1, 14, 15, 59, DateTimeKind.Utc);
      var b = a.AlignDailyMinutes(15);
      var c = new DateTime(1980, 1, 1, 14, 15, 00, DateTimeKind.Utc);
      Aver.AreEqual(c, b);
    }

    [Run]
    public void AlignDailyMinutesTests_007()
    {
      var a = new DateTime(1980, 1, 1, 2, 02, 00, DateTimeKind.Utc);
      var b = a.AlignDailyMinutes(120);//two hours
      var c = new DateTime(1980, 1, 1, 4, 0, 00, DateTimeKind.Utc);
      Aver.AreEqual(c, b);

      a = b.AddMinutes(2);
      b = a.AlignDailyMinutes(120);//two hours
      c = new DateTime(1980, 1, 1, 6, 0, 00, DateTimeKind.Utc);
      Aver.AreEqual(c, b);
    }

    [Run]
    public void AlignDailyMinutesTests_008()
    {
      var a = new DateTime(1980, 1, 1, 2, 02, 00, DateTimeKind.Utc);
      var b = a.AlignDailyMinutes(300);//5 hours
      var c = new DateTime(1980, 1, 1, 5, 0, 00, DateTimeKind.Utc);
      Aver.AreEqual(c, b);

      a = new DateTime(1980, 1, 1, 18, 02, 00, DateTimeKind.Utc);
      b = a.AlignDailyMinutes(300);//5 h
      c = new DateTime(1980, 1, 1, 20, 0, 00, DateTimeKind.Utc);
      Aver.AreEqual(c, b);

      a = b.AddMinutes(2);
      b = a.AlignDailyMinutes(300);//5 h
      c = new DateTime(1980, 1, 2, 1, 0, 00, DateTimeKind.Utc);
      Aver.AreEqual(c, b);
    }

    [Run]
    public void AlignDailyMinutesTests_009()
    {
      var a = new DateTime(1980, 1, 1, 2, 02, 00, DateTimeKind.Utc);
      var b = a.AlignDailyMinutes(60 * 25);
      var c = new DateTime(1980, 1, 2, 1, 0, 00, DateTimeKind.Utc);
      Aver.AreEqual(c, b);

      a = b.AddMinutes(1);
      b = a.AlignDailyMinutes(60 * 25);
      c = new DateTime(1980, 1, 3, 1, 0, 00, DateTimeKind.Utc);
      Aver.AreEqual(c, b);

      a = new DateTime(1980, 1, 1, 18, 37, 12, DateTimeKind.Utc);
      b = a.AlignDailyMinutes(60 * 25);
      c = new DateTime(1980, 1, 2, 1, 0, 00, DateTimeKind.Utc);
      Aver.AreEqual(c, b);

      a = new DateTime(1980, 1, 1, 23, 59, 59, DateTimeKind.Utc);
      b = a.AlignDailyMinutes(60 * 25);
      c = new DateTime(1980, 1, 2, 1, 0, 00, DateTimeKind.Utc);
      Aver.AreEqual(c, b);

      a = new DateTime(1980, 1, 2, 0, 1, 0, DateTimeKind.Utc);
      b = a.AlignDailyMinutes(60 * 25);
      c = new DateTime(1980, 1, 3, 1, 0, 00, DateTimeKind.Utc);
      Aver.AreEqual(c, b);
    }


    [Run]
    public void AlignDailyMinutesTests_010()
    {
      var hs = new HashSet<DateTime>();
      for(var s = 0; s < 86400; s ++)
      {
        var d =new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(s);

        var got = d.AlignDailyMinutes(10);
        hs.Add(got);
      }

      hs.See();
      Aver.AreEqual(1 + (24 * 6), hs.Count);
    }

    [Run]
    public void AlignDailyMinutesTests_011()
    {
      var hs = new HashSet<DateTime>();
      for (var s = 0; s < 86400; s++)
      {
        var d = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(s);

        var got = d.AlignDailyMinutes(60);
        hs.Add(got);
      }

      hs.See();
      Aver.AreEqual(1 + (24 * 1), hs.Count);
    }

  }
}
