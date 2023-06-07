/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;

using System.IO;
using System.Net;
using System.Threading;

using Azos;
using Azos.Graphics;
using Azos.Wave.Mvc;
using Azos.Security.CAPTCHA;
using Azos.Serialization.JSON;
using Azos.Data;

using WaveTestSite.Pages;
using Azos.Serialization.Bix;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WaveTestSite.Controllers
{
  /// <summary>
  /// Adds numbers
  /// </summary>
  [ApiControllerDoc(BaseUri ="/mvc/tester", Title = "Tester", Description ="Testing controller")]
  public class Tester : Controller
  {
      [Action]
      public object Story(bool buffered = false, int len = 500)
      {
        WorkContext.Response.Buffered = buffered;
        return Azos.Text.NaturalTextGenerator.Generate(len.KeepBetween(1, 128*1024));
      }

      [Action]
      public Task<object> StoryAsync(bool buffered = false, int len = 500)
      {
        WorkContext.Response.Buffered = buffered;
        return Task.FromResult<object>(Azos.Text.NaturalTextGenerator.Generate(len.KeepBetween(1, 128 * 1024)));
      }

      [Action]
      public object Error(string text = "Unspecified text")
      {
        throw new AzosException("Error thrown with: "+ text);
      }

      [Action]
      public async Task<object> ErrorAsync(string text = "Unspecified text async")
      {
        await Task.Delay(1000);
        throw new AzosException("Async Error thrown with: " + text);
      }

      [Action, ApiEndpointDoc]
      public object AboutUs()
      {
        return new Pages.AboutUs();
      }


      [Action, ApiEndpointDoc]
      public async Task SlowImage(string url, int dbDelayMs = 100, int netDelayMs = 0)
      {
        WorkContext.Response.ContentType = Azos.Web.ContentType.JPEG;
        WorkContext.Response.SetCacheControlHeaders(Azos.Web.CacheControl.PrivateMaxAgeSec(2), false);

        // emulate a pause accessing DB
        Thread.Sleep(dbDelayMs);

        // get image from url or make random image
        var stream = await WorkContext.Response.GetDirectOutputStreamForWritingAsync();
        using (var image = string.IsNullOrWhiteSpace(url) ? makeRandomImage() : downloadImage(url))
        {
          var buffer = new byte[255];
          while (image.CanRead)
          {
            var count = image.Read(buffer, 0, buffer.Length);
            if (count == 0)
              break;

            stream.Write(buffer, 0, count);

            // emulate slow network
            Thread.Sleep(App.Random.NextScaledRandomInteger(0, netDelayMs));
          }
        }
      }



      [Action, ApiEndpointDoc]
      public object Zekret()
      {
        var cookie = new CookieOptions();
        cookie.Path = "/";
        WorkContext.Response.AppendCookie("ZEKRET", "Hello", cookie);
        return new Redirect("/pages/Welcome");
      }

      //[Action("TestEmpty1", 1, "match{methods=GET}")]
      [ActionOnGet(Name ="TestEmpty"), ApiEndpointDoc]
      public string TestEmpty()
      {
        return null;
      }


      [Action(Name ="add", MatchScript = "match{is-local=true}"), ApiEndpointDoc]
      public string PlusLocal(int a, int b, string text = "Was added")
      {
        return "LOCAL {0} {1}".Args(text, a+b);
      }

      [Action(Name ="add"), ApiEndpointDoc]
      public string Plus(int a, int b, string text = "Was added")
      {
       // WorkContext.NeedsSession();//<------!
        return "{0} {1}".Args(text, a+b);
      }


      [Action, ApiEndpointDoc]
      public string Multiply(int a=2, int b=3, string text = "Was multiplied")
      {
        return "{0} {1}".Args(text, a*b);
      }

      [Action, ApiEndpointDoc]
      public string Divide(double a = 1, double b = 1, string text = "Was divided")
      {
        return "{0} {1}".Args(text, a / b);
      }

      [Action, ApiEndpointDoc]
      public object Rnd(int from=0, int to = 100)
      {
         var list = new List<object>();



         for(var i=from;i<to;i++)
           list.Add(  new
                     {
                       RandomNumber = App.Random.NextRandomInteger,
                       When = DateTime.Now
                     });

         return list;
      }

      [Action, ApiEndpointDoc]
      public object SingleRnd()
      {
         return new
              {
                RandomNumber = App.Random.NextRandomInteger,
                When = DateTime.Now
              };
      }

      [Action, ApiEndpointDoc]
      public object Hello()
      {
         return new Pages.Welcome();
      }

      [Action, ApiEndpointDoc]
      public object Yahoo()
      {
         return new Redirect("http://yahoo.com");
      }

      [Action, ApiEndpointDoc]
      public object IBox()
      {
         return new ImageBoxTest();
      }

      [Action(Name ="download", MatchScript = "match{is-local=true}"), ApiEndpointDoc]//notice that this action is only allowed for local requestors
      public object Download(string fpath, bool attachment = false)
      {
         return new FileDownload(fpath, attachment);
      }


      [Azos.Security.AdHocPermission("test", "SpecialPermission", 10)]
      [Action, ApiEndpointDoc]
      public string SpecialCase()
      {
        return "Obviously you have been granted permission to see this!";
      }


      [Action, ApiEndpointDoc(Uri="echo", Title ="Payload Echo", Description = "Echoes back the payload supplied as JSON map")]
      public object Echo(JsonDataMap data)
      {
        return new
        {
          ServerMessage = "You have supplied content and here is my response",
          ServerDateTime = DateTime.Now,
          RequestedData = data
        };
      }

      [Action]
      [ApiEndpointDoc(Uri = "echolimits", Title = "Payload Echo", Description = "Echoes back the payload supplied as JSON map")]
      [JsonReadingOptions(MaxObjectItems = 5,
                          MaxDepth = 4,
                          MaxKeyLength = 10,
                          MaxObjects = 10,
                          MaxArrays = 11,
                          MaxArrayItems = 3,
                          MaxCharLength = 4000,
                          MaxStringLength = 100,
                          MaxCommentLength = 25)]
      public object EchoLimits(JsonDataMap data)
      {
        return new
        {
          ServerMessage = "You have supplied content, set limits, and here is my response",
          ServerDateTime = DateTime.Now,
          RequestedData = data,

          limits = WorkContext.JsonOptions
        };
      }

     [Action, ApiEndpointDoc]
      public object CAPTCHA(string key=null)
      {
        WorkContext.NeedsSession();
        WorkContext.Response.SetNoCacheHeaders();

        if (key.IsNotNullOrWhiteSpace() && WorkContext.Session != null)
        {
          var pk = WorkContext.Session.Items[key] as PuzzleKeypad;
          if (pk != null) return pk.DefaultRender(App);
        }

        return (new PuzzleKeypad("01234")).DefaultRender(App);
      }

      [Action, ApiEndpointDoc]
      public object CAPTCHA2(string secret="0123456789", string fn = null)
      {
        return new Picture((new PuzzleKeypad(secret)).DefaultRender(App, System.Drawing.Color.Yellow), JpegImageFormat.Standard, fn);
      }

      [ActionOnGet(Name ="person"), ApiEndpointDoc]
      public object PersonGet(JsonDataMap req)
      {
        makePuzzle();
        var row = new Person{
          ID = req!=null ? req["PersonID"].AsString("500") : null,
          FirstName = "Yuriy",
          LastName = "Gagarin",
          DOB = new DateTime(1980, 07, 05),
          Puzzle = new JsonDataMap{ {"Image", "/mvc/tester/captcha?key=PersonPuzzle"}, {"Question", "Enter the current Year"}}
        };
        return new ClientRecord(row, null, Atom.ZERO);
      }

      [ActionOnPost(Name ="person"), ApiEndpointDoc]
      public object PersonPost(Person doc)
      {
        var puzzlePass = false;
        WorkContext.NeedsSession();
        if (WorkContext.Session != null && doc.Puzzle != null)
        {
          var pk = WorkContext.Session["PersonPuzzle"] as PuzzleKeypad;
          if (pk != null)
          {
            var answer = doc.Puzzle["Answer"] as JsonDataArray;
            if (answer != null)
              puzzlePass = pk.DecipherCoordinates(answer) == pk.Code;
          }
        }

        Exception error = null;
        if (puzzlePass)
        {
          doc.YearsInService++;
          error = doc.Validate();
        }
        else
          error = new FieldValidationException("Person", "Puzzle", "Please answer the question correctly");

        if (doc.Puzzle != null)
        doc.Puzzle.Remove("Answer");

        makePuzzle();
        return new ClientRecord(doc, error, Atom.ZERO);
      }

      [Action, ApiEndpointDoc]
      public object MultipartMap(JsonDataMap data)
      {
        return new
        {
            ServerSays = "ECHO: You Have posted to me as MAP",
            Data = data
        };
      }

      [Action, ApiEndpointDoc]
      public object MultipartRow(MultipartTestDoc data, bool map = true)
      {
        var result = new
        {
            ServerSays = "ECHO: You Have posted to me as ROW",
            Data = data,
            Map = map
        };

        if (map) return result;

        return new JsonResult(result, JsonWritingOptions.Compact);
      }

      [Action, ApiEndpointDoc]
      public object MultipartByteArray(string customerNumber, byte[] picture, string picture_filename)
      {
        return new
        {
            ServerSays = "ECHO: You Have posted to me BYte Array",
            Customer = customerNumber,
            Picture = picture,
            File = picture_filename
        };
      }

      [Action, ApiEndpointDoc]
      public object MultipartStream(string customerNumber, Stream picture, string picture_filename)
      {
        return new
        {
            ServerSays = "ECHO: You Have posted to me STREAM!",
            Customer = customerNumber,
            PictureSize = picture.Length,
            File = picture_filename
        };
      }



      private void makePuzzle()
      {
        var pk = new PuzzleKeypad(DateTime.Now.Year.ToString(), "0123456789?*@abzqw", 8);
        WorkContext.NeedsSession();
        WorkContext.Session["PersonPuzzle"] = pk;
      }

      static Tester()
      {
        Azos.Wave.Client.RecordModelGenerator.DefaultInstance.ModelLocalization += (_, schema, prop, val, lang) =>
        {
          if (prop=="Description" && val=="Private Status") return "Частный Статус";
          if (prop=="Description" && val=="Salary") return "Заработная Плата";
          return val;
        };
      }

     #region .pvt

     private Stream makeRandomImage()
     {
       // make image
       var image = Image.Of(200, 200);
       var imageFormat = JpegImageFormat.Standard;

       //todo: ...

       // save it to memory stream
       var stream = new MemoryStream();
       image.Save(stream, imageFormat);
       stream.Position = 0;

       return stream;
     }

     private Stream downloadImage(string url)
     {
       using var webClient = new WebClient();
       byte[] imageBytes = webClient.DownloadData(url);
       return new MemoryStream(imageBytes);
     }

     #endregion .pvt

  }

        public enum StatusCode{None=0 , Beginner, Advanced, Master}

        [Bix("d5af97b4-33df-433a-a11e-b69b5169c2c2")]
        public class Person : TypedDoc
        {
          [Field(metadata: "z=true b=234")]
          public string ID { get; set;}

          [Field(metadata:@"pub{Placeholder='Enter Your First Name' Case='caps'}")]// LookupDict='{""key1"": ""value1"", ""key2"": ""value2""}'}")]
          public string FirstName { get; set;}

          [Field(required: true, visible: true, metadata:"pvt='privatedata' pub{a=1 b='some string'}")]
          public string LastName { get; set;}


          [Field(required: true, description: "Public Status", metadata: "pub{msg='everything is public'}")]//, backendName: "zhaba")]
          public StatusCode PubStatus{get; set;}

          [Field(required: false, description: "Private Status")]
          public StatusCode? PvtStatus{get; set;}

          [Field]
          public DateTime? DOB { get; set;}

          [Field]
          public int YearsInService { get; set;}

          [Field(required: true, description: "Salary")]
          public decimal Salary { get; set;}

          [Field]
          public bool  Registered { get; set;}

          [Field(valueList: "CAR: Car Driver,SMK: Smoker, REL: Religious, CNT: Country music lover, GLD: Gold collector")]
          public string Various { get; set;}

          [Field(storeFlag: StoreFlag.None, metadata:@"ControlType=Puzzle Stored=true")]
          public JsonDataMap Puzzle { get; set;}


          [Field]
          public Person PersonField { get; set; }

          [Field]
          public List<int> AListOfIntField{ get; set;}

          [Field]
          public List<int?> AListOfNullableIntField { get; set; }

          [Field]
          public List<List<int?>> AListOfListOfNullableIntField { get; set; }

          [Field]
          public Dictionary<string,int> ADictOfStringIntField { get; set; }

          [Field]
          public Dictionary<string, int?[]> ADictOfStringNullableIntArrayField { get; set; }

          [Field]
          public Dictionary<string, List<Person>> ADictOfStringPersonListField { get; set; }

          [Field]
          public Dictionary<Person, List<Person>> ADictOfPersonPersonListField { get; set; }

        }


        public class MultipartTestDoc : TypedDoc
        {
          [Field]
          public string CustomerNumber { get; set;}


          [Field]
          public byte[] Picture { get; set;}

          [Field]
          public string Picture_filename { get; set;}

          [Field]
          public string Picture_contenttype { get; set;}
        }


}
