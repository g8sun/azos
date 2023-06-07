/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Runtime.Serialization;


namespace Azos.Sky.Workers
{
  /// <summary>
  /// Thrown to indicate workers related problems
  /// </summary>
  [Serializable]
  public class WorkersException : SkyException
  {
    public WorkersException() : base() {}
    public WorkersException(string message) : base(message) {}
    public WorkersException(string message, Exception inner) : base(message, inner) { }
    protected WorkersException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }
}
