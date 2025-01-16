/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Linq;
using System.Reflection;

using Azos.Apps;
using Azos.Conf;
using Azos.IO.Console;
using Azos.Platform;
using Azos.Scripting;
using Azos.Scripting.Dsl;
using Azos.Serialization.JSON;

namespace Azos.Tools.Srun
{
    /// <summary>
    /// Program body (entry point) for SRUN "script runner" utility
    /// </summary>
    [Platform.ProcessActivation.ProgramBody("srun", Description = "Script runner")]
    public static class ProgramBody
    {
        public static void Main(string[] args)
        {
          try
          {
            Security.TheSafe.Init(onlyWhenHasNotInitBefore: true);

            using (var app = new AzosApplication(true, args, null))
            {
              app.SetConsolePort(LocalConsolePort.Default);

              Console.CancelKeyPress += (_, e) =>
              {
                app.Stop();
                //20210210 DKh stop the inner most app as well, which can be a unit test local app
                ((IApplicationImplementation)ExecutionContext.Application).Stop();
                e.Cancel = true;
              };

              System.Environment.ExitCode = run(app);
            }
          }
          catch(Exception error)
          {
            ConsoleUtils.Error(error.ToMessageWithType());
            Console.WriteLine();

            var e = new WrappedExceptionData(error, true, true);
            Console.WriteLine(e.ToJson(JsonWritingOptions.PrettyPrintRowsAsMapASCII));

            Console.WriteLine();
            System.Environment.ExitCode = -1;
          }
        }


        private static int run(AzosApplication app)
        {
          var config = app.CommandArgs;
          var silent = config["s", "silent"].Exists;

          if (!silent)
          {
            ConsoleUtils.WriteMarkupContent( typeof(ProgramBody).GetText("Welcome.txt") );
          }


          if (config["?"].Exists ||
              config["h"].Exists ||
              config["help"].Exists)
          {
             ConsoleUtils.WriteMarkupContent( typeof(ProgramBody).GetText("Help.txt") );
             return 0;
          }

          var rootFile = config.AttrByIndex(0);

          if (!rootFile.Exists)
          {
            ConsoleUtils.Error("No file to run");
            return -2;
          }

          var rootFilePath = rootFile.Value;

          if (rootFilePath.IsNullOrWhiteSpace() || !System.IO.File.Exists(rootFilePath))
          {
            ConsoleUtils.Error("File does not exist");
            return -2;
          }

          if (!silent)
          {
            Console.ForegroundColor =  ConsoleColor.DarkGray;
            Console.Write("Platform runtime: ");
            Console.ForegroundColor =  ConsoleColor.Yellow;
            Console.WriteLine(Platform.Abstraction.PlatformAbstractionLayer.PlatformName);
            Console.ForegroundColor =  ConsoleColor.Gray;
          }

          var rnode = config["runner"];

          var runner =  FactoryUtils.Make<ScriptSource>(rnode, typeof(ScriptSource), new object[]{app, rootFilePath});

          if(!silent)
          {
            Console.ForegroundColor =  ConsoleColor.DarkGray;
            Console.Write("Runner: ");
            Console.ForegroundColor =  ConsoleColor.Yellow;
            Console.WriteLine(runner.GetType().DisplayNameWithExpandedGenericArgs());
            Console.ForegroundColor =  ConsoleColor.Gray;
          }

          if (config["state"].Exists)
          {
            var fn = config["state"].AttrByIndex(0).Value;
            if (!System.IO.File.Exists(fn))
            {
              ConsoleUtils.Error("State JSON File does not exist");
              return -3;
            }
            var json = JsonReader.DeserializeDataObjectFromFile(fn) as JsonDataMap;
            if (json ==null)
            {
              ConsoleUtils.Error("State JSON File does not contain a valid JSON map");
              return -3;
            }
            runner.GenericRunner.GlobalState.Append(json, true);
          }

          if (config["vars"].Exists)
          {
            foreach(var nvar in config["vars"].Attributes)
            {
              runner.GenericRunner.GlobalState[nvar.Name] = nvar.Value;
            }
          }

          if (config["dump-source"].Exists)
          {
            var src = runner.GenericRunner.RootSource.ToLaconicString(CodeAnalysis.Laconfig.LaconfigWritingOptions.PrettyPrint);
            Console.WriteLine(src);
            return 0;
          }


          var entryPointName = config.AttrByIndex(1).Value;
          var entryPoint = runner.GenericRunner.EntryPoints.FirstOrDefault(i => i.Name.EqualsOrdIgnoreCase(entryPointName));

          runner.RunAsync(entryPoint).GetAwaiter().GetResult();


          if (config["r","result"].Exists)
          {
            Console.WriteLine(runner.GenericRunner.Result.ToJson(JsonWritingOptions.PrettyPrintRowsAsMapASCII));
          }

          if (config["g", "global"].Exists)
          {
            Console.WriteLine(runner.GenericRunner.GlobalState.ToJson(JsonWritingOptions.PrettyPrintRowsAsMapASCII));
          }

          return  0;
        }
    }
}
