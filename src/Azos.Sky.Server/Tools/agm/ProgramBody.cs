/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;

using Azos.IO.Console;
using Azos.Conf;
using Azos.Apps;
using Azos.Platform;
using Azos.Serialization.JSON;

using Azos.Sky.Identification;

namespace Azos.Sky.Tools.agm
{
  [Platform.ProcessActivation.ProgramBody("agm", Description = "Gdid manager")]
  public static class ProgramBody
  {
    public static void Main(string[] args)
    {
      try
      {
        run(args);
      }
      catch (Exception error)
      {
        ConsoleUtils.Error(error.ToMessageWithType());
        ConsoleUtils.Info("Exception details:");
        Console.WriteLine(error.ToString());

        Environment.ExitCode = -1;
      }
    }

    static void run(string[] args)
    {
      using (var app = new AzosApplication(args, null))
      {
        var silent = app.CommandArgs["s", "silent"].Exists;
        if (!silent)
        {
          ConsoleUtils.WriteMarkupContent(typeof(ProgramBody).GetText("Welcome.txt"));

          ConsoleUtils.Info("Build information:");
          Console.WriteLine(" Azos:     " + BuildInformation.ForFramework);
          Console.WriteLine(" Tool:     " + new BuildInformation(typeof(agm.ProgramBody).Assembly));
        }

        if (app.CommandArgs["?", "h", "help"].Exists)
        {
          ConsoleUtils.WriteMarkupContent(typeof(ProgramBody).GetText("Help.txt"));
          return;
        }

        IGdidAuthorityAccessor accessor = null;
        string connectToAuthority = null;
        var authority = app.CommandArgs.AttrByIndex(0).Value;

        if (authority.StartsWith("@"))//use accessor instead
        {
          authority = authority.Remove(0, 1);
          var cfg = authority.AsLaconicConfig(handling: Data.ConvertErrorHandling.Throw);
          accessor = FactoryUtils.MakeAndConfigureComponent<IGdidAuthorityAccessor>(app, cfg);
        }
        else
        {
          connectToAuthority = authority.ToResolvedServiceNode(false).ConnectString;
        }

        var scope = app.CommandArgs.AttrByIndex(1).Value;
        var seq = app.CommandArgs.AttrByIndex(2).Value;
        var bsize = app.CommandArgs.AttrByIndex(3).ValueAsInt(1);

        if (!silent)
        {
          ConsoleUtils.Info("Authority:  " + authority);
          ConsoleUtils.Info(accessor!=null ? ("Connect via: {0}" + accessor.GetType().Name) : ("Connect to: {0}" + connectToAuthority));
          ConsoleUtils.Info("Scope:      " + scope);
          ConsoleUtils.Info("Sequence:   " + seq);
          ConsoleUtils.Info("Block Size: " + bsize);
        }



        var w = System.Diagnostics.Stopwatch.StartNew();

        var generator = new GdidGenerator(app, accessor);
        if (connectToAuthority.IsNotNullOrWhiteSpace())
        {
            generator.AuthorityHosts.Register(new GdidGenerator.AuthorityHost(app, connectToAuthority));
        }


        var json = app.CommandArgs["j", "json"].Exists;
        var arr = app.CommandArgs["array"].Exists;


        if (arr) Console.WriteLine("[");

        for (var i = 0; i < bsize; i++)
        {
          var gdid = generator.GenerateOneGdid(scope, seq, bsize - i, noLWM: true);
          string line;

          if (json)
            line = new
            {
              Era = gdid.Era,
              ID = gdid.ID,
              Authority = gdid.Authority,
              Counter = gdid.Counter
            }.ToJson(JsonWritingOptions.Compact);
          else
            line = "{0}:  {1}".Args(i, gdid);


          Console.Write(line);
          Console.WriteLine(arr && i != bsize - 1 ? ", " : " ");
        }

        if (arr) Console.WriteLine("]");

        if (!silent)
        {
          Console.WriteLine();
          ConsoleUtils.Info("Run time: " + w.Elapsed.ToString());
        }

        DisposableObject.DisposeAndNull(ref generator);
        DisposableObject.DisposeIfDisposableAndNull(ref accessor);
      }//using APP

    }
  }
}
