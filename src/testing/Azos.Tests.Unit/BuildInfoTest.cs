/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;

using Azos.Scripting;

using Azos.Conf;

namespace Azos.Tests.Unit
{
  [Runnable(TRUN.BASE)]
  public class BuildInfoTest
  {
    [Run]
    public void ForFramework()
    {
      Console.WriteLine(BuildInformation.ForFramework);
    }
  }
}
