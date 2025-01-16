/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.Collections.Generic;
using System.Linq;
using Azos.Scripting;
using Azos.Text;

namespace Azos.Tests.Nub.Parsing
{
  [Runnable]
  public class TagParsingTests
  {

    [Run]
    public void ParseSegments_00()
    {
      var got = "".ParseSegments().ToArray();
      Aver.AreEqual(0, got.Length);

      got = ((string)null).ParseSegments().ToArray();
      Aver.AreEqual(0, got.Length);

      got = "   ".ParseSegments().ToArray();
      Aver.AreEqual(1, got.Length);
      Aver.IsFalse(got[0].IsTag);
      Aver.AreEqual("   ", got[0].Content);
      Aver.AreEqual(0, got[0].IdxStart);
      Aver.AreEqual(2, got[0].IdxEnd);
      Aver.AreEqual(3, got[0].Length);
    }


    [Run]
    public void ParseSegments_01()
    {
      var got = "How is <monster> doing?".ParseSegments().ToArray();

      got.See();

      Aver.AreEqual(3, got.Length);

      Aver.IsFalse(got[0].IsTag);
      Aver.AreEqual("How is ", got[0].Content);
      Aver.AreEqual(0, got[0].IdxStart);
      Aver.AreEqual(6, got[0].IdxEnd);
      Aver.AreEqual(7, got[0].Length);

      Aver.IsTrue(got[1].IsTag);
      Aver.AreEqual("monster", got[1].Content);
      Aver.AreEqual(7, got[1].IdxStart);
      Aver.AreEqual(15, got[1].IdxEnd);
      Aver.AreEqual(9, got[1].Length);//with < >

      Aver.IsFalse(got[2].IsTag);
      Aver.AreEqual(" doing?", got[2].Content);
      Aver.AreEqual(16, got[2].IdxStart);
      Aver.AreEqual(22, got[2].IdxEnd);
      Aver.AreEqual(7, got[2].Length);
    }

    [Run]
    public void ParseSegments_01_2()
    {
      var got = "How is (monster) doing?".ParseSegments('(', ')').ToArray();

      got.See();

      Aver.AreEqual(3, got.Length);

      Aver.IsFalse(got[0].IsTag);
      Aver.AreEqual("How is ", got[0].Content);
      Aver.AreEqual(0, got[0].IdxStart);
      Aver.AreEqual(6, got[0].IdxEnd);
      Aver.AreEqual(7, got[0].Length);

      Aver.IsTrue(got[1].IsTag);
      Aver.AreEqual("monster", got[1].Content);
      Aver.AreEqual(7, got[1].IdxStart);
      Aver.AreEqual(15, got[1].IdxEnd);
      Aver.AreEqual(9, got[1].Length);//with < >

      Aver.IsFalse(got[2].IsTag);
      Aver.AreEqual(" doing?", got[2].Content);
      Aver.AreEqual(16, got[2].IdxStart);
      Aver.AreEqual(22, got[2].IdxEnd);
      Aver.AreEqual(7, got[2].Length);
    }


    [Run]
    public void ParseSegments_02()
    {
      var got = "How is <monster>".ParseSegments().ToArray();

      got.See();

      Aver.AreEqual(2, got.Length);

      Aver.IsFalse(got[0].IsTag);
      Aver.AreEqual("How is ", got[0].Content);
      Aver.AreEqual(0, got[0].IdxStart);
      Aver.AreEqual(6, got[0].IdxEnd);
      Aver.AreEqual(7, got[0].Length);

      Aver.IsTrue(got[1].IsTag);
      Aver.AreEqual("monster", got[1].Content);
      Aver.AreEqual(7, got[1].IdxStart);
      Aver.AreEqual(15, got[1].IdxEnd);
      Aver.AreEqual(9, got[1].Length);//with < >
    }

    [Run]
    public void ParseSegments_02_2()
    {
      var got = "How is <monster".ParseSegments().ToArray();

      got.See();

      Aver.AreEqual(2, got.Length);

      Aver.IsFalse(got[0].IsTag);
      Aver.AreEqual("How is ", got[0].Content);
      Aver.AreEqual(0, got[0].IdxStart);
      Aver.AreEqual(6, got[0].IdxEnd);
      Aver.AreEqual(7, got[0].Length);

      Aver.IsTrue(got[1].IsTag);
      Aver.AreEqual("monster", got[1].Content);
      Aver.AreEqual(7, got[1].IdxStart);
      Aver.AreEqual(14, got[1].IdxEnd);
      Aver.AreEqual(8, got[1].Length);//with < >
    }

    [Run]
    public void ParseSegments_03()
    {
      var got = "<monster> How is".ParseSegments().ToArray();

      got.See();

      Aver.AreEqual(2, got.Length);

      Aver.IsTrue(got[0].IsTag);
      Aver.AreEqual("monster", got[0].Content);
      Aver.AreEqual(0, got[0].IdxStart);
      Aver.AreEqual(8, got[0].IdxEnd);
      Aver.AreEqual(9, got[0].Length);//with < >

      Aver.IsFalse(got[1].IsTag);
      Aver.AreEqual(" How is", got[1].Content);
      Aver.AreEqual(9, got[1].IdxStart);
      Aver.AreEqual(15, got[1].IdxEnd);
      Aver.AreEqual(7, got[1].Length);
    }


    [Run]
    public void ParseTags_01()
    {
      var got = "Hello <b><@ X{ a=123   }></b>! How are you?".ParseSegments()
                                                .ParseTags("@", new KeyValuePair<string, string>("&lt;", "<"),
                                                                new KeyValuePair<string, string>("&gt;", ">"),
                                                                new KeyValuePair<string, string>("&amp;", "&"))
                                                .ToArray();

      got.See();

      Aver.AreEqual(5, got.Length);

      Aver.IsFalse(got[0].Segment.IsTag);
      Aver.AreEqual("Hello ", got[0].Segment.Content);
      Aver.AreEqual(0, got[0].Segment.IdxStart);
      Aver.AreEqual(5, got[0].Segment.IdxEnd);
      Aver.AreEqual(6, got[0].Segment.Length);
      Aver.IsNull(got[0].Def);

      Aver.IsTrue(got[1].Segment.IsTag);
      Aver.AreEqual("b", got[1].Segment.Content);
      Aver.AreEqual(6, got[1].Segment.IdxStart);
      Aver.AreEqual(8, got[1].Segment.IdxEnd);
      Aver.AreEqual(3, got[1].Segment.Length);
      Aver.IsNull(got[1].Def);

      Aver.IsTrue(got[2].Segment.IsTag);
      Aver.AreEqual("@ X{ a=123   }", got[2].Segment.Content);
      Aver.AreEqual(9, got[2].Segment.IdxStart);
      Aver.AreEqual(24, got[2].Segment.IdxEnd);
      Aver.AreEqual(16, got[2].Segment.Length);
      Aver.IsNotNull(got[2].Def);
      Aver.AreEqual("X", got[2].Def.Name);
      Aver.AreEqual(123, got[2].Def.Of("a").ValueAsInt());
    }


    [Run]
    public void ExpandHtmlTags_01()
    {
      var got = "Hello <b><@ X{ a=123   }></b>! How are you? Thank you for your <@Y{what='Ferrari &amp; BMW &gt;&lt; Dodge'}>!".ExpandHtmlTags((sb, tag) => {
        if (tag.Def.Name == "X") sb.Append("Murariy");
        else if (tag.Def.Name == "Y") sb.Append(tag.Def.ValOf("what"));
      }).ToString();

      got.See();

      Aver.AreEqual("Hello <b>Murariy</b>! How are you? Thank you for your Ferrari & BMW >< Dodge!", got);
    }

    [Run]
    public void ExpandHtmlTags_02()
    {
      var got = "Hello <b><@n=123></b>! How are you? Thank you for your <@n=-190.2>!".ExpandHtmlTags((sb, tag) => sb.Append(tag.Def.ValOf("n"))).ToString();

      got.See();

      Aver.AreEqual("Hello <b>123</b>! How are you? Thank you for your -190.2!", got);
    }

  }
}
