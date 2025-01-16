﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Linq;
using Azos.Scripting;
using Azos.Time;
using HLS = Azos.Time.HourList.Span;


namespace Azos.Tests.Nub.Time
{
  [Runnable]
  public class HourListSpanTests
  {
    [Run]
    public void Unassigned00()
    {
      var sut = new HLS();
      Aver.IsFalse(sut.IsAssigned);
      Aver.AreEqual("", sut.ToString());
      Aver.AreEqual("0:00", sut.Start);
      Aver.AreEqual("", sut.Finish);
      Aver.AreEqual(0, sut.StartMinute);
      Aver.AreEqual(-1, sut.FinishMinute);
      Aver.AreEqual(0, sut.GetHashCode());
      Aver.IsTrue(sut.Equals(new HLS()));
    }

    [Run]
    public void Unassigned01()
    {
      var sut = new HLS(0, 0);
      Aver.IsFalse(sut.IsAssigned);
      Aver.AreEqual("", sut.ToString());
      Aver.AreEqual("0:00", sut.Start);
      Aver.AreEqual("", sut.Finish);
      Aver.AreEqual(0, sut.StartMinute);
      Aver.AreEqual(-1, sut.FinishMinute);
      Aver.AreEqual(0, sut.GetHashCode());
      Aver.IsTrue(sut.Equals(new HLS()));
    }

    [Run]
    public void Basic01()
    {
      var sut = new HLS(0, 1);
      Aver.IsTrue(sut.IsAssigned);
      Aver.AreEqual("0:00-0:01", sut.ToString());
      Aver.AreEqual("0:00", sut.Start);
      Aver.AreEqual("0:01", sut.Finish);
      Aver.AreEqual(0, sut.StartMinute);
      Aver.AreEqual(0, sut.FinishMinute);
      Aver.AreEqual(0, sut.GetHashCode());
      Aver.IsTrue(sut.Equals(new HLS(0, 1)));
      Aver.IsFalse(sut.Equals(new HLS(0, 2)));

      Aver.IsTrue(sut == new HLS(0, 1));
      Aver.IsTrue(sut != new HLS(0, 2));

      Aver.AreEqual(sut, HourList.Parse(sut.ToString()).Spans.First());
    }

    [Run]
    public void Basic02()
    {
      var sut = new HLS(59, 2);
      Aver.IsTrue(sut.IsAssigned);
      Aver.AreEqual("0:59-1:01", sut.ToString());
      Aver.AreEqual("0:59", sut.Start);
      Aver.AreEqual("1:01", sut.Finish);
      Aver.AreEqual(59, sut.StartMinute);
      Aver.AreEqual(60, sut.FinishMinute);
      Aver.IsTrue(0 != sut.GetHashCode());
      Aver.IsTrue(sut.Equals(new HLS(59, 2)));
      Aver.IsFalse(sut.Equals(new HLS(58, 2)));
      Aver.IsFalse(sut.Equals(new HLS(59, 3)));

      Aver.IsTrue(sut == new HLS(59, 2));
      Aver.IsTrue(sut != new HLS(58, 2));
      Aver.IsTrue(sut != new HLS(59, 3));

      Aver.AreEqual(sut, HourList.Parse(sut.ToString()).Spans.First());
    }

    [Run]
    public void IsAssigned()
    {
      Aver.IsFalse(new HLS().IsAssigned);
      Aver.IsFalse(new HLS(0, 0).IsAssigned);
      Aver.IsTrue(new HLS(0, 1).IsAssigned);
      Aver.IsTrue(new HLS(1, 0).IsAssigned);
    }

    [Run]
    public void ToStringTest()
    {
      Aver.AreEqual("", new HLS().ToString());

      Aver.AreEqual("13:10-13:11", new HLS(13 * 60 + 10, 1).ToString());
      Aver.AreEqual("13:10-13:12", new HLS(13 * 60 + 10, 2).ToString());
      Aver.AreEqual("13:10-13:35", new HLS(13 * 60 + 10, 25).ToString());

      Aver.AreEqual("0:59-1:00", new HLS(59, 1).ToString());
      Aver.AreEqual("0:59-1:01", new HLS(59, 2).ToString());
      Aver.AreEqual("0:59-1:02", new HLS(59, 3).ToString());
      Aver.AreEqual("0:59-1:07", new HLS(59, 8).ToString());

      Aver.AreEqual("3:59-4:07", new HLS(3 * 60 + 59, 8).ToString());
    }

    [Run]
    public void ToStringAndParseTest()
    {
      Aver.AreEqual("", new HLS().ToString());

      Aver.AreEqual("13:10-13:11", HourList.Parse(new HLS(13 * 60 + 10, 1).ToString()).Spans.First().ToString());
      Aver.AreEqual("13:10-13:12", HourList.Parse(new HLS(13 * 60 + 10, 2).ToString()).Spans.First().ToString());
      Aver.AreEqual("13:10-13:35", HourList.Parse(new HLS(13 * 60 + 10, 25).ToString()).Spans.First().ToString());

      Aver.AreEqual("0:59-1:00", HourList.Parse(new HLS(59, 1).ToString()).Spans.First().ToString());
      Aver.AreEqual("0:59-1:01", HourList.Parse(new HLS(59, 2).ToString()).Spans.First().ToString());
      Aver.AreEqual("0:59-1:02", HourList.Parse(new HLS(59, 3).ToString()).Spans.First().ToString());
      Aver.AreEqual("0:59-1:07", HourList.Parse(new HLS(59, 8).ToString()).Spans.First().ToString());

      Aver.AreEqual("3:59-4:07", HourList.Parse(new HLS(3 * 60 + 59, 8).ToString()).Spans.First().ToString());
    }


    [Run]
    public void IntersectsWith()
    {
      Aver.IsFalse(new HLS().IntersectsWith(new HLS()));
      Aver.IsTrue(new HLS(0, 1).IntersectsWith(new HLS(0, 1)));

      Aver.IsTrue(new HLS(123, 100).IntersectsWith(new HLS(100, 24)));
      Aver.IsFalse(new HLS(123, 100).IntersectsWith(new HLS(100, 23)));

      Aver.IsTrue(new HLS(123, 100).IntersectsWith(new HLS(100, 50)));
      Aver.IsTrue(new HLS(123, 100).IntersectsWith(new HLS(100, 500)));
      Aver.IsTrue(new HLS(123, 100).IntersectsWith(new HLS(128, 1)));
      Aver.IsFalse(new HLS(123, 100).IntersectsWith(new HLS(128, 0)));

      Aver.IsTrue(new HLS(123, 100).IntersectsWith(new HLS(222, 1)));
      Aver.IsFalse(new HLS(123, 100).IntersectsWith(new HLS(223, 1)));


      Aver.IsFalse(new HLS(123, 100).IntersectsWith(new HLS(0, 123)));
      Aver.IsTrue(new HLS(123, 100).IntersectsWith(new HLS(0, 124)));

      Aver.IsFalse(new HLS(123, 100).IntersectsWith(new HLS(223, 1)));
      Aver.IsTrue(new HLS(123, 100).IntersectsWith(new HLS(222, 1)));

      Aver.IsTrue(new HLS(123, 1).IntersectsWith(new HLS(123, 1)));
      Aver.IsFalse(new HLS(123, 1).IntersectsWith(new HLS(124, 1)));
    }

    [Run]
    public void IntersectUnassigned()
    {
      Aver.AreEqual(false, new HLS(10 * 60, 10).Intersect(new HLS(10 * 60, 0)).IsAssigned);
      Aver.AreEqual(false, new HLS(10 * 60, 0).Intersect(new HLS(10 * 60, 10)).IsAssigned);
    }

    [Run]
    public void Intersect01()
    {
      Aver.AreEqual( new HLS(9,1), new HLS(0, 10).Intersect(new HLS(9, 1)));
      Aver.AreEqual(new HLS(0, 10), new HLS(0, 10).Intersect(new HLS(0, 10)));
      Aver.AreEqual(new HLS(1, 9), new HLS(0, 10).Intersect(new HLS(1, 10)));

      Aver.AreEqual(new HLS(9, 1), new HLS(9, 1).Intersect(new HLS(0, 10)));
      Aver.AreEqual(new HLS(1, 9), new HLS(1, 10).Intersect(new HLS(0, 10)));
    }

    [Run]
    public void Intersect02()
    {
      Aver.AreEqual(new HLS(10 * 60, 1), new HLS(10 * 60, 10).Intersect(new HLS(10 * 60, 1)));

      Aver.AreEqual(new HLS(), new HLS(500, 10).Intersect(new HLS(0, 10)));
      Aver.AreEqual(new HLS(), new HLS(500, 10).Intersect(new HLS(800, 10)));

      Aver.AreEqual(new HLS(505, 2), new HLS(500, 10).Intersect(new HLS(505, 2)));
      Aver.AreEqual(new HLS(505, 2), new HLS(505, 2).Intersect(new HLS(500, 10)));

      Aver.AreEqual(new HLS(500, 10), new HLS(500, 10).Intersect(new HLS(0, 1000)));
    }

    [Run]
    public void Intersect03()
    {
      Aver.AreEqual(new HLS(), new HLS(100, 10).Intersect(new HLS(91, 1)));
      Aver.AreEqual(new HLS(100, 1), new HLS(100, 10).Intersect(new HLS(91, 10)));
      Aver.AreEqual(new HLS(100, 1), new HLS(100, 10).Intersect(new HLS(90, 11)));

      Aver.AreEqual(new HLS(109, 1), new HLS(100, 10).Intersect(new HLS(109, 1)));
      Aver.AreEqual(new HLS(), new HLS(100, 10).Intersect(new HLS(110, 1)));
    }


    [Run]
    public void CoversAnother01()
    {
      Aver.IsTrue(new HLS(100, 5).CoversAnother(new HLS(100, 5)));
      Aver.IsFalse(new HLS(100, 5).CoversAnother(new HLS(99, 5)));
      Aver.IsFalse(new HLS(100, 5).CoversAnother(new HLS(99, 6)));

      Aver.IsTrue(new HLS(100, 5).CoversAnother(new HLS(101, 1)));
      Aver.IsFalse(new HLS(101, 1).CoversAnother(new HLS(100, 5)));

      Aver.IsFalse(new HLS(100, 5).CoversAnother(new HLS(101, 20)));

      Aver.IsTrue(new HLS(0, 28 * 60).CoversAnother(new HLS(0, 25 * 60)));
      Aver.IsTrue(new HLS(0, 28 * 60).CoversAnother(new HLS(60, 25 * 60)));
      Aver.IsFalse(new HLS(0, 25 * 60).CoversAnother(new HLS(0, 28 * 60)));
      Aver.IsFalse(new HLS(60, 25 * 60).CoversAnother(new HLS(0, 28 * 60)));
    }

    [Run]
    public void CoversAnother02()
    {
      Aver.IsTrue(new HLS(0, 100).CoversAnother(new HLS(0, 100)));
      Aver.IsTrue(new HLS(0, 100).CoversAnother(new HLS(0, 1)));
      Aver.IsTrue(new HLS(0, 100).CoversAnother(new HLS(99, 1)));
      Aver.IsTrue(new HLS(0, 100).CoversAnother(new HLS(10, 10)));
      Aver.IsFalse(new HLS(0, 100).CoversAnother(new HLS(0, 101)));

      Aver.IsTrue(new HLS(100, 100).CoversAnother(new HLS(100, 100)));
      Aver.IsTrue(new HLS(100, 100).CoversAnother(new HLS(100, 1)));
      Aver.IsTrue(new HLS(100, 100).CoversAnother(new HLS(199, 1)));
      Aver.IsTrue(new HLS(100, 100).CoversAnother(new HLS(110, 10)));
      Aver.IsFalse(new HLS(100, 100).CoversAnother(new HLS(100, 201)));
      Aver.IsFalse(new HLS(100, 100).CoversAnother(new HLS(99, 50)));
      Aver.IsTrue(new HLS(100, 100).CoversAnother(new HLS(100, 50)));
    }


    [Run]
    public void Exclude00()
    {
      var (a, b, bn) = new HLS(100, 105).Exclude(new HLS());
      Aver.AreEqual(new HLS(100, 105), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);

      (a, b, bn) = new HLS().Exclude(new HLS(100, 105));
      Aver.AreEqual(new HLS(), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude01()
    {
      var (a,b, bn) = new HLS(100, 105).Exclude(new HLS(0, 1));
      Aver.AreEqual(new HLS(100, 105), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude02()
    {
      var (a, b, bn) = new HLS(0, 10).Exclude(new HLS(0, 9));
      Aver.AreEqual(new HLS(9, 1), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude03()
    {
      var (a, b, bn) = new HLS(0, 10).Exclude(new HLS(3, 2));
      Aver.AreEqual(new HLS(0, 3), a);
      Aver.AreEqual(new HLS(5, 5), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude04()
    {
      var (a, b, bn) = new HLS(0, 10).Exclude(new HLS(3, 50));
      Aver.AreEqual(new HLS(0, 3), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude05()
    {
      var sut = new HLS(2 * 60, 60);
      var ex = new HLS(3, 50);
      var (a, b, bn) = sut.Exclude(ex);
      Aver.IsFalse(bn);

      "{0} ^ {1}  ->  {2} {3}".SeeArgs(sut, ex, a, b);
      Aver.AreEqual(new HLS(2 * 60, 60), a);
      Aver.AreEqual(new HLS(), b);
    }

    [Run]
    public void Exclude06()
    {
      var sut = new HLS(2 * 60, 60);
      var ex = new HLS(2 * 60, 60);
      var (a, b, bn) = sut.Exclude(ex);

      "{0} ^ {1}  ->  {2} {3}".SeeArgs(sut, ex, a, b);
      Aver.AreEqual(new HLS(), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude07()
    {
      var sut = new HLS(2 * 60, 60);
      var ex = new HLS(2 * 60, 1);
      var (a, b, bn) = sut.Exclude(ex);

      "{0} ^ {1}  ->  {2} {3}".SeeArgs(sut, ex, a, b);
      Aver.AreEqual(new HLS((2 * 60)+1, 59), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude08()
    {
      var sut = new HLS(2 * 60, 60);
      var ex = new HLS((2 * 60) - 1, 1);
      var (a, b, bn) = sut.Exclude(ex);

      "{0} ^ {1}  ->  {2} {3}".SeeArgs(sut, ex, a, b);
      Aver.AreEqual(new HLS(2 * 60, 60), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude09()
    {
      var sut = new HLS(2 * 60, 60);
      var ex = new HLS(1 * 60, 65);
      var (a, b, bn) = sut.Exclude(ex);

      "{0} ^ {1}  ->  {2} {3}".SeeArgs(sut, ex, a, b);
      Aver.AreEqual(new HLS((2 * 60)+5, 60-5), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }


    [Run]
    public void Exclude10()
    {
      var sut = new HLS(2 * 60, 60);
      var ex = new HLS((2 * 60) + 10, 60);
      var (a, b, bn) = sut.Exclude(ex);

      "{0} ^ {1}  ->  {2} {3}".SeeArgs(sut, ex, a, b);
      Aver.AreEqual(new HLS(2 * 60, 10), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude11()
    {
      var sut = new HLS(2 * 60, 60);
      var ex = new HLS((2 * 60) + 10, 560);
      var (a, b, bn) = sut.Exclude(ex);

      "{0} ^ {1}  ->  {2} {3}".SeeArgs(sut, ex, a, b);
      Aver.AreEqual(new HLS(2 * 60, 10), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude12()
    {
      var sut = new HLS(2 * 60, 60);
      var ex = new HLS((2 * 60) + 50, 560);
      var (a, b, bn) = sut.Exclude(ex);

      "{0} ^ {1}  ->  {2} {3}".SeeArgs(sut, ex, a, b);
      Aver.AreEqual(new HLS(2 * 60, 50), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude13()
    {
      var sut = new HLS(2 * 60, 60);
      var ex = new HLS((2 * 60) + 50, 1);
      var (a, b, bn) = sut.Exclude(ex);

      "{0} ^ {1}  ->  {2} {3}".SeeArgs(sut, ex, a, b);
      Aver.AreEqual(new HLS(2 * 60, 50), a);
      Aver.AreEqual(new HLS((2 * 60) + 51, 9), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude14()
    {
      var sut = new HLS(2 * 60, 60);
      var ex = new HLS((2 * 60) + 50, 2);
      var (a, b, bn) = sut.Exclude(ex);

      "{0} ^ {1}  ->  {2} {3}".SeeArgs(sut, ex, a, b);
      Aver.AreEqual(new HLS(2 * 60, 50), a);
      Aver.AreEqual(new HLS((2 * 60) + 52, 8), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude15()
    {
      var sut = new HLS(2 * 60, 60);
      var ex = new HLS();
      var (a, b, bn) = sut.Exclude(ex);

      "{0} ^ {1}  ->  {2} {3}".SeeArgs(sut, ex, a, b);
      Aver.AreEqual(new HLS(2 * 60, 60), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude16()
    {
      var sut = new HLS(2 * 60, 60);
      var ex = new HLS(0, 1);
      var (a, b, bn) = sut.Exclude(ex);

      "{0} ^ {1}  ->  {2} {3}".SeeArgs(sut, ex, a, b);
      Aver.AreEqual(new HLS(2 * 60, 60), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude17()
    {
      var sut = new HLS(2 * 60, 60);
      var ex = new HLS(1000, 1);
      var (a, b, bn) = sut.Exclude(ex);

      "{0} ^ {1}  ->  {2} {3}".SeeArgs(sut, ex, a, b);
      Aver.AreEqual(new HLS(2 * 60, 60), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude18()
    {
      var sut = new HLS(2 * 60, 60);
      var ex = new HLS(0, 1000);
      var (a, b, bn) = sut.Exclude(ex);

      "{0} ^ {1}  ->  {2} {3}".SeeArgs(sut, ex, a, b);
      Aver.AreEqual(new HLS(), a);
      Aver.AreEqual(new HLS(), b);
      Aver.IsFalse(bn);
    }

    [Run]
    public void Exclude19()
    {
      var sut = new HLS(23 * 60, 2 * 60);
      var ex = new HLS(23 * 60 + 30, 60);
      var (a, b, bn) = sut.Exclude(ex);

      "{0} ^ {1}  ->  {2} {3} {4}".SeeArgs(sut, ex, a, b, bn);
      Aver.AreEqual(new HLS(23 * 60, 30), a);
      Aver.AreEqual(new HLS(30, 30), b);
      Aver.IsTrue(bn);
    }

    [Run]
    public void Join01()
    {
      Aver.AreEqual(new HLS(0, 10), new HLS(0, 10).Join(new HLS(0,10)));

      Aver.AreEqual(new HLS(0, 1), new HLS(0, 1).Join(new HLS(0, 1)));
      Aver.AreEqual(new HLS(0, 2), new HLS(0, 1).Join(new HLS(0, 2)));
      Aver.AreEqual(new HLS(0, 2), new HLS(0, 1).Join(new HLS(1, 1)));
    }

    [Run]
    public void Join02()
    {
      Aver.AreEqual(new HLS(0, 200 + 100), new HLS(0, 1).Join(new HLS(200, 100)));
      Aver.AreEqual(new HLS(150, 50 + 100), new HLS(150, 1).Join(new HLS(200, 100)));
    }


  }
}
