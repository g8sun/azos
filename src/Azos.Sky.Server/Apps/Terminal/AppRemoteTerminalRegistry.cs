/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;

using Azos.Collections;


namespace Azos.Apps.Terminal
{
  /// <summary>
  /// Internal framework registry of AppRemoteTerminal
  /// </summary>
  internal static class AppRemoteTerminalRegistry
  {
    private static Registry<AppRemoteTerminal> s_Registry = new Registry<AppRemoteTerminal>();


    public static IEnumerable<AppRemoteTerminal> All => s_Registry;

    public static ulong GenerateId() => FID.Generate().ID;


    public static bool Register(AppRemoteTerminal term) => s_Registry.Register(term);
    public static bool Unregister(AppRemoteTerminal term) => s_Registry.Unregister(term);
  }

}
