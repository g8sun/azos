﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Text;

using Azos.Conf;
using Azos.Wave.Handlers;

namespace Azos.Wave.Tv
{
  /// <summary>
  /// Handles SSE stream for console events
  /// </summary>
  public class ConPortSSEHandler : SSEMailboxHandler
  {
    public ConPortSSEHandler(WorkHandler director, string name, int order, WorkMatch match)
                       : base(director, name, order, match){ }

    public ConPortSSEHandler(WorkHandler director, IConfigSectionNode confNode)
                       : base(director, confNode) { }

    protected override (bool isNew, Mailbox mbox) ConnectMailbox(WorkContext work)
    {
      return base.ConnectMailbox(work);
    }

  }
}
