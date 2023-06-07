/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;

using Azos.Scripting;
using Azos.Serialization.JSON;
using Azos.Wave;

namespace Azos.Tests.Unit.Wave
{
  [Runnable(TRUN.BASE)]
  public class URIPatternTests
  {
    [Run]
    public void T1()
    {
      var uri = "/2012/sep/mayor-gets-elected";
      var pat = new UriPattern("/{year}/{month}/{title}");

      var match = pat.MatchUriPath(uri);
      Aver.IsNotNull(match);
      Aver.AreObjectsEqual("2012", match["year"]);
      Aver.AreObjectsEqual("sep", match["month"]);
      Aver.AreObjectsEqual("mayor-gets-elected", match["title"]);
    }

    [Run]
    public void T2()
    {
      var uri = "/news/2012/sep/mayor-gets-elected";
      var pat = new UriPattern("/{year}/{month}/{title}");

      var match = pat.MatchUriPath(uri);
      Aver.IsNull(match);
    }

    [Run]
    public void T3()
    {
      var uri = "/news/2012/sep/mayor-gets-elected";
      var pat = new UriPattern("/news/{year}/{month}/{title}");

      var match = pat.MatchUriPath(uri);
      Aver.IsNotNull(match);
      Aver.AreObjectsEqual("2012", match["year"]);
      Aver.AreObjectsEqual("sep", match["month"]);
      Aver.AreObjectsEqual("mayor-gets-elected", match["title"]);
    }

    [Run]
    public void T3_no_slash()
    {
      var uri = "/news/2012/sep/mayor-gets-elected";
      var pat = new UriPattern("news/{year}/{month}/{title}");

      var match = pat.MatchUriPath(uri);
      Aver.IsNotNull(match);
      Aver.AreObjectsEqual("2012", match["year"]);
      Aver.AreObjectsEqual("sep", match["month"]);
      Aver.AreObjectsEqual("mayor-gets-elected", match["title"]);
    }

    [Run]
    public void T4_defaults()
    {
      var uri = "/news/2012/sep/mayor-gets-elected";
      var pat = new UriPattern("/news/{year}/{month}/{title=overview}");

      var match = pat.MatchUriPath(uri);
      Aver.IsNotNull(match);
      Aver.AreObjectsEqual("2012", match["year"]);
      Aver.AreObjectsEqual("sep", match["month"]);
      Aver.AreObjectsEqual("mayor-gets-elected", match["title"]);
    }

    [Run]
    public void T5_defaults()
    {
      var uri = "/news/2012/sep/";
      var pat = new UriPattern("/news/{year}/{month}/{title=overview}");

      var match = pat.MatchUriPath(uri);
      Aver.IsNotNull(match);
      Aver.AreObjectsEqual("2012", match["year"]);
      Aver.AreObjectsEqual("sep", match["month"]);
      Aver.AreObjectsEqual("overview", match["title"]);
    }

    [Run]
    public void T6()
    {
      var uri = "/news/2012/sep/mayor-gets-elected";
      var pat = new UriPattern("/news/{year}/{month}/{title}/presidential");

      var match = pat.MatchUriPath(uri);
      Aver.IsNull(match);
    }

    [Run]
    public void T7()
    {
      var uri = "/news/2012/sep/mayor-gets-elected/presidential";
      var pat = new UriPattern("/news/{year}/{month}/{title}/presidential");

      var match = pat.MatchUriPath(uri);
      Aver.IsNotNull(match);
      Aver.AreObjectsEqual("2012", match["year"]);
      Aver.AreObjectsEqual("sep", match["month"]);
      Aver.AreObjectsEqual("mayor-gets-elected", match["title"]);
    }


    [Run]
    public void T8()
    {
      var uri = "/news/2012/sep/mayor-gets-elected/presidential";
      var pat = new UriPattern("/news/{*path}");

      var match = pat.MatchUriPath(uri);
      Aver.IsNotNull(match);
      Aver.AreObjectsEqual("2012/sep/mayor-gets-elected/presidential", match["path"]);
    }


    [Run]
    [Aver.Throws(typeof(WaveException), Message="wildcard capture variable", MsgMatch= Aver.ThrowsAttribute.MatchType.Contains)]
    public void T9()
    {
   //   var uri = "/news/2012/sep/mayor-gets-elected/presidential?bonus=true";
      var pat = new UriPattern("/news/{*path}/cantbe");
    }


    [Run]
    public void T10()
    {
      var uri = "/news/2012/sep/mayor%2egets%2eelected/";
      var pat = new UriPattern("/news/{year}/{month}/{title}");

      var match = pat.MatchUriPath(uri);
      Aver.IsNotNull(match);
      Aver.AreObjectsEqual("2012", match["year"]);
      Aver.AreObjectsEqual("sep", match["month"]);
      Aver.AreObjectsEqual("mayor.gets.elected", match["title"]);
    }

    [Run]
    public void T10_notrailingslash()
    {
      var uri = "/news/2012/sep/mayor%2egets%2eelected";
      var pat = new UriPattern("/news/{year}/{month}/{title}");

      var match = pat.MatchUriPath(uri);
      Aver.IsNotNull(match);
      Aver.AreObjectsEqual("2012", match["year"]);
      Aver.AreObjectsEqual("sep", match["month"]);
      Aver.AreObjectsEqual("mayor.gets.elected", match["title"]);
    }

    [Run]
    public void T11_case_insensitive()
    {
      var uri = "/news/2012/sep/mayor%2egets%2eelected/";
      var pat = new UriPattern("/NEWS/{year}/{month}/{title}");

      var match = pat.MatchUriPath(uri);
      Aver.IsNotNull(match);
      Aver.AreObjectsEqual("2012", match["year"]);
      Aver.AreObjectsEqual("sep", match["month"]);
      Aver.AreObjectsEqual("mayor.gets.elected", match["title"]);
    }

    [Run]
    public void T11_case_sensitive()
    {
      var uri = "/news/2012/sep/mayor%2egets%2eelected/";
      var pat = new UriPattern("/NEWS/{year}/{month}/{title}");

      var match = pat.MatchUriPath(uri, senseCase: true);
      Aver.IsNull(match);
    }

    [Run]
    public void T12_path_with_slashes()
    {
      var uri = "/news/2012/sep%2fmayor%2egets%2eelected/";
      var pat = new UriPattern("/news/{year}/{title}");

      var match = pat.MatchUriPath(uri);
      Aver.IsNotNull(match);
      Aver.AreObjectsEqual("2012", match["year"]);
      Aver.AreObjectsEqual("sep/mayor.gets.elected", match["title"]);
    }
  /*
    [Run]
    public void T13_mixed_seg_var()
    {
      var uri = "http://contoso.com/news/2012/title/");
      var pat = new UriPattern("news/20{year}/{title}");

Console.WriteLine(pat._____Chunks);

      var match = pat.MatchUriPath(uri);
      Aver.IsNotNull(match);
      Aver.AreObjectsEqual("12", match["year"]);
      Aver.AreObjectsEqual("title", match["title"]);
    }

    [Run]
    public void T14_mixed_seg_var()
    {
      var uri = "http://contoso.com/news/2012/sep%2fmayor%2egets%2d25%25%20profit");
      var pat = new UriPattern("news/20{year}/{title}");

      var match = pat.MatchUriPath(uri);
      Aver.IsNotNull(match);
      Aver.AreObjectsEqual("12", match["year"]);
      Aver.AreObjectsEqual("sep/mayor.gets-25% profit", match["title"]);
    }
 */
    [Run]
    public void MakeURI_T1_noprefix()
    {
        var pattern = new UriPattern("/news/{year}/{month}/{title}");
        var map = new JsonDataMap { { "year", "1981" }, { "month", 12 }, { "title", "some_title" } };

        var uri = pattern.MakeUri(map);

        Aver.AreObjectsEqual(new Uri("/news/1981/12/some_title", UriKind.RelativeOrAbsolute), uri);
    }

    [Run]
    public void MakeURI_T1_prefix()
    {
        var pattern = new UriPattern("/{year}/{month}/{title}");
        var prefix = new Uri("http://test.com");
        var map = new JsonDataMap { { "year", "1981" }, { "month", 12 }, { "title", "some_title" } };

        var uri = pattern.MakeUri(map, prefix);

        Aver.AreObjectsEqual(new Uri(prefix, "http://test.com/1981/12/some_title"), uri);
    }

    [Run]
    public void MakeURI_T2_params()
    {
        var pattern = new UriPattern("/{year}/values?{p1}={v1}&par2={v2}");
        var map = new JsonDataMap { { "year", "1980" }, { "p1", "par1" }, { "v1", 10 }, { "v2", "val2" } };

        var uri = pattern.MakeUri(map);

        Aver.AreObjectsEqual(new Uri("/1980/values?par1%3D10%26par2%3Dval2", UriKind.Relative), uri);
    }

    [Run]
    public void MakeURI_T2_params_prefix()
    {
        var pattern = new UriPattern("/{year}/values?{p1}={v1}&par2={v2}");
        var map = new JsonDataMap { { "year", "1980" }, { "p1", "par1" }, { "v1", 10 }, { "v2", "val2" } };
        var prefix = new Uri("http://test.org/");

        var uri = pattern.MakeUri(map, prefix);

        Aver.AreObjectsEqual(new Uri("http://test.org/1980/values?par1%3D10%26par2%3Dval2", UriKind.Absolute), uri);
    }

  }
}
