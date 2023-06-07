﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azos.Apps;
using Azos.Apps.Injection;
using Azos.Client;
using Azos.Collections;
using Azos.Conf;
using Azos.Data;
using Azos.Data.Idgen;
using Azos.Log;
using Azos.Serialization.JSON;

namespace Azos.Sky.Chronicle.Feed
{
  /// <summary>
  /// Pulls chronicle data feed from X number of channels into local receptacle
  /// </summary>
  public sealed class PullAgentDaemon : Daemon
  {
    private const int THREAD_SPIN_MS = 2000;

    public PullAgentDaemon(IApplication application) : base(application) { }
    public PullAgentDaemon(IModule parent) : base(parent) { }

    protected override void Destructor()
    {
      cleanupChannels();
      base.Destructor();
    }

    private void cleanupChannels()
    {
      var channels = m_Sources.ToArray();
      m_Sources.Clear();
      channels.ForEach(channel => this.DontLeak(() => channel.Dispose()));
    }

    [Inject] ILogChronicleLogic m_Chronicle;


    [Config] private string m_DataDir;
    private Registry<Source> m_Sources = new Registry<Source>();

    public override string ComponentLogTopic => CoreConsts.DATA_TOPIC;

    [Config]
    public string DataDir
    {
      get => m_DataDir;
      set
      {
        CheckDaemonInactive();
        m_DataDir = value;
      }
    }


    protected override void DoConfigure(IConfigSectionNode node)
    {
      base.DoConfigure(node);
      cleanupChannels();
      if (node == null) return;

      foreach(var nSource in node.ChildrenNamed(Source.CONFIG_SOURCE_SECTION))
      {
        var source = FactoryUtils.MakeDirectedComponent<Source>(this, nSource, typeof(Source), new[]{ nSource });
        m_Sources.Register(source);
      }

    }

    protected override void DoStart()
    {
      Name.NonBlank("Configured daemon name");
      (m_Sources.Count > 0).IsTrue("Configured sources");
      Directory.Exists(m_DataDir.NonBlank(nameof(DataDir))).IsTrue("Existing data dir");
      base.DoStart();
    }

    protected override void DoWaitForCompleteStop()
    {
      base.DoWaitForCompleteStop();
      writeCheckpoints();
    }


    private void spin(Task antecedent)
    {
      try
      {

      }
      catch(Exception error)
      {

      }
    }

    private void writeCheckpoints()
    {
      this.DontLeak(() => writeCheckpointsUnsafe(),
                      "Error writing checkpoints to disk: ",
                      nameof(writeCheckpoints),
                      errorLogType: MessageType.CatastrophicError);
    }

    private void writeCheckpointsUnsafe()
    {
      var fn = $"{nameof(PullAgentDaemon)}.{App.AppId}.{Name}.chkpt";
      var fullFn = Path.Combine(m_DataDir, fn);

      var cfg = new LaconicConfiguration();
      cfg.Create("checkpoints");
      cfg.Root.AddAttributeNode("utc-now", App.TimeSource.UTCNow);
      cfg.Root.AddAttributeNode("app-id", App.AppId);
      cfg.Root.AddAttributeNode("agent-id", Name);
      cfg.Root.AddAttributeNode("host", Azos.Platform.Computer.HostName);

      foreach(var source in m_Sources)
      {
        var nChannel = cfg.Root.AddChildNode(Source.CONFIG_SOURCE_SECTION);
        nChannel.AddAttributeNode("name", source.Name);
        var chkUtc = source.CheckpointUtc;
        nChannel.AddAttributeNode("utc-checkpoint", chkUtc.ToString("o"));//ISO8601 with time zone (utc Z)
        nChannel.AddAttributeNode("utc-checkpoint-nix-ms", chkUtc.ToMillisecondsSinceUnixEpochStart());
      }
      cfg.SaveAs(fullFn, CodeAnalysis.Laconfig.LaconfigWritingOptions.PrettyPrint);
    }
  }
}
