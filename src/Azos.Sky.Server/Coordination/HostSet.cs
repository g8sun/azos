/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Azos.Apps;
using Azos.Conf;
using Azos.Collections;
using Azos.Log;
using Azos.Time;

using Azos.Sky.Metabase;
using Azos.Sky.Contracts;
using Azos.Data;

namespace Azos.Sky.Coordination
{
  /// <summary>
  /// Represents a set of hosts that perform some common work.
  /// Primarily used for sharding work among hosts in the set.
  /// The data is fed from the Metabase, and supports static and dynamic sets.
  /// Static sets have a metabase-fixed number of hosts, whereas dynamic sets
  /// may include dynamic hosts (as allocated by IaaS provider).
  /// The dynamic sets are not supported until IaaS providers are implemented
  /// </summary>
  public class HostSet : ApplicationComponent, INamed
  {
    #region CONSTS
    public const MessageType DEFAULT_LOG_LEVEL = MessageType.Warning;

    public const string CONFIG_HEARTBEAT_INTERVAL_SEC = "heartbeat-interval-sec";
    public const int DEFAULT_HEARTBEAT_INTERVAL_SEC = 3 * 60;
    public const int MIN_HEARTBEAT_INTERVAL_SEC = 30;
    #endregion

    #region Inner
    public sealed class Host : INamed, IOrdered
    {
      public Host(Metabank.SectionHost host, int o) { m_Section = host; m_Order = o; }
      private Metabank.SectionHost m_Section;
      private int m_Order;

      public string Name { get { return m_Section.RegionPath; } }
      public Metabank.SectionHost Section { get { return m_Section; } }
      public int Order { get { return m_Order; } }
      public DateTime? LastDownTime { get; internal set; }
    }

    public struct HostPair : IEnumerable<Metabank.SectionHost>
    {
      public HostPair(Metabank.SectionHost p, Metabank.SectionHost s) { Primary = p; Secondary = s; }
      public readonly Metabank.SectionHost Primary;
      public readonly Metabank.SectionHost Secondary;

      public bool Assigned { get { return Primary != null; } }

      public IEnumerator<Metabank.SectionHost> GetEnumerator()
      {
        if (Primary != null) yield return Primary;
        if (Secondary != null) yield return Secondary;
      }

      IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
    #endregion

    #region .ctor
    protected HostSet(IApplication app, string setName, string reqPath, string path, IConfigSectionNode config) : base(app)
    {
      m_Name = setName;
      m_RequestedPath = path;
      m_Path = path;
      m_Dynamic = false;

      var mb = App.GetMetabase();

      foreach (var hnode in config.Children.Where(c => c.IsSameName(Metabank.CONFIG_HOST_SET_HOST_SECTION)))
      {
        var n = hnode.AttrByName(Configuration.CONFIG_NAME_ATTR).Value;
        var o = hnode.AttrByName(Configuration.CONFIG_ORDER_ATTR).ValueAsInt();
        if (n.IsNullOrWhiteSpace()) continue;

        var hp = Metabank.RegCatalog.JoinPathSegments(path, n);
        var hsect = mb.CatalogReg.NavigateHost(hp);

        if (hsect.Dynamic)
          m_Dynamic = true;

        m_DeclaredHosts.Register(new Host(hsect, o));
      }

      BuildHostList();

      var heartbeatSec = config.AttrByName(CONFIG_HEARTBEAT_INTERVAL_SEC).ValueAsInt(DEFAULT_HEARTBEAT_INTERVAL_SEC);
      if (heartbeatSec < 0) heartbeatSec = 0;

      if (heartbeatSec > 0 && heartbeatSec < MIN_HEARTBEAT_INTERVAL_SEC) heartbeatSec = MIN_HEARTBEAT_INTERVAL_SEC;

      if (heartbeatSec > 0)
      {
        m_HeartbeatScan = new Event(App.EventTimer,
                                    body: e => DoHeartbeat(),
                                    interval: TimeSpan.FromSeconds(heartbeatSec + App.Random.NextScaledRandomInteger(-5, 5)),
                                    bodyAsyncModel: EventBodyAsyncModel.AsyncTask,
                                    enabled: false)
        {
          StartDate = App.TimeSource.UTCNow.AddSeconds(10),
          TimeLocation = TimeLocation.UTC
        };
        m_HeartbeatScan.Enabled = true;
      }

      ConfigAttribute.Apply(this, config);
    }

    protected override void Destructor()
    {
      DisposeAndNull(ref m_HeartbeatScan);
      base.Destructor();
    }
    #endregion

    #region Fields
    private string m_Name;
    private string m_RequestedPath;
    private string m_Path;

    private bool m_Dynamic;
    private Event m_HeartbeatScan;
    private OrderedRegistry<Host> m_DeclaredHosts = new OrderedRegistry<Host>();
    private Host[] m_Hosts;
    #endregion

    #region Properties
    public override string ComponentLogTopic => SysConsts.LOG_TOPIC_WORKER;


    /// <summary>
    /// Returns HostSet Name
    /// </summary>
    public string Name { get { return m_Name; } }

    /// <summary>
    /// Returns the region path that was requested
    /// </summary>
    public string RequestedPath { get { return m_RequestedPath; } }

    /// <summary>
    /// Returns the actual resolved region path at which the set operates
    /// </summary>
    public string Path { get { return m_Path; } }

    /// <summary>
    /// True to indicate that the number of hosts in the set is flexible
    /// </summary>
    public bool Dynamic { get { return m_Dynamic; } }

    /// <summary>
    /// The hosts that are declared in set
    /// </summary>
    public IOrderedRegistry<Host> DeclaredHosts { get { return m_DeclaredHosts; } }
    #endregion

    #region Public
    /// <summary>
    /// Assigns a worker from the set for the supplied sharding key.
    /// If key is null then a random member is assigned.
    /// Returns null if there is no host available for assignment
    /// </summary>
    public virtual HostPair AssignHost(ShardKey shardingKey)
    {
      var hosts = m_Hosts;//thread-safe copy, as during execution another may swap

      if (hosts == null || hosts.Length == 0) return new HostPair();

      if (!shardingKey.Assigned) shardingKey = new ShardKey(App.Random.NextRandomUnsignedLong);

      var idx = (uint)(shardingKey.GetDistributedStableHash()) % hosts.Length;

      var idx1 = -1L;
      for (var c = 0; c < hosts.Length; c++)
      {
        var current = idx;
        idx = (current + 1) % hosts.Length;
        var host = hosts[current];
        if (!host.LastDownTime.HasValue)
        {
          idx1 = current;
          break;
        }
      }

      var idx2 = -1L;
      for (var c = 0; c < hosts.Length; c++)
      {
        var current = idx;
        idx = (current + 1) % hosts.Length;
        var host = hosts[current];
        if (!host.LastDownTime.HasValue && current != idx1)
        {
          idx2 = current;
          break;
        }
      }

      return new HostPair(idx1 >= 0 ? hosts[idx1].Section : null, idx2 >= 0 ? hosts[idx2].Section : null);
    }

    /// <summary>
    /// Refreshes the list of hosts in the set
    /// </summary>
    public void Refresh()
    {
      BuildHostList();
      Task.Factory.StartNew(DoHeartbeat);
    }
    #endregion

    #region Protected
    /// <summary>
    /// Override to determine the lists of hosts in the set.
    /// In case of dynamic hosts this method may be called many times
    /// </summary>
    protected virtual void BuildHostList()
    {
      m_Hosts = m_DeclaredHosts.OrderedValues.ToArray();
    }

    protected virtual void DoHeartbeat()
    {
      var hosts = m_Hosts;

      foreach (var host in hosts)
      {
        try
        {
          using (var pinger = App.GetServiceClientHub().MakeNew<IPingerClient>(host.Section))
            pinger.Ping();

          host.LastDownTime = null;
        }
        catch (Exception error)
        {
          host.LastDownTime = App.TimeSource.UTCNow;
          //todo instrument
          WriteLog(MessageType.Error, "heartbeat()", "Sending heartbeat to '{0}' failed: {1}".Args(host.Section.RegionPath, error.ToMessageWithType()), error);
        }
      }
    }

    #endregion
  }
}
