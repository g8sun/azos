﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Net.Sockets;

using Azos.IO.Sipc;

namespace Azos.Tests.Nub.IO.Ipc
{
  public class MockSipcServer : SipcServer
  {
    public MockSipcServer(int startPort, int endPort) : base(startPort, endPort)
    {
    }

    public List<(Connection con, string cmd)> Received = new();
    public List<Exception> Errors = new();


    protected override void DoHandleLinkError(Exception error)
    {
      lock(Errors)
      {
        Errors.Add(error);
      }
    }

    protected override void DoHandleCommand(Connection connection, string command)
    {
      lock(Received)
      {
        Received.Add((connection, command));
      }
    }

    protected override Connection ObtainConnection(string name, TcpClient client, out bool isNew)
    {
      isNew = true;
      return new Connection(name, client);
    }
  }

  public class MockSipcClient : SipcClient
  {
    public MockSipcClient(int serverPort, string id) : base(serverPort, id)
    {
    }

    protected override void DoHandleUplinkFailure()
    {
      WasFailure = true;
    }

    public bool WasFailure;
    public List<(Connection con, string cmd)> Received = new List<(Connection con, string cmd)>();
    public List<Exception> Errors = new List<Exception>();


    protected override void DoHandleUplinkError(Exception error)
    {
      lock(Errors)
      {
        Errors.Add((error));
      }
    }

    protected override void DoHandleCommand(Connection connection, string command)
    {
      lock(Received)
      {
        Received.Add((connection, command));
      }
    }

    protected override Connection MakeNewConnection(string name, TcpClient client)
    {
      return new Connection(name, client);
    }
  }
}
