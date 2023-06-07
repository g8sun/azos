/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Runtime.Serialization;


namespace Azos.Sky.EventHub
{
  /// <summary>
  /// Thrown to indicate eventhub-related problems: sending and getting events
  /// </summary>
  [Serializable]
  public class EventHubException : SkyException
  {
    public EventHubException() : base() {}
    public EventHubException(string message) : base(message) {}
    public EventHubException(string message, Exception inner) : base(message, inner) { }
    protected EventHubException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }
}
