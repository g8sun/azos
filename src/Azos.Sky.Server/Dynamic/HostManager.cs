/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Linq;

using Azos.Apps;
using Azos.Conf;
using Azos.Instrumentation;

using Azos.Sky.Metabase;

namespace Azos.Sky.Dynamic
{
  public class HostManager : DaemonWithInstrumentation<IApplicationComponent>, IHostManagerImplementation
  {
    #region CONSTS
    private static readonly TimeSpan INSTRUMENTATION_INTERVAL = TimeSpan.FromMilliseconds(3700);
    #endregion

    #region .ctor
    public HostManager(ISkyApplication director) : base(director) { }

    protected override void Destructor()
    {
      DisposableObject.DisposeAndNull(ref m_InstrumentationEvent);
      base.Destructor();
    }
    #endregion

    #region Fields
    private bool m_InstrumentationEnabled;
    private Time.Event m_InstrumentationEvent;

    private Collections.NamedInterlocked m_Stats = new Collections.NamedInterlocked();
    #endregion

    #region Properties

    public override string ComponentLogTopic => SysConsts.LOG_TOPIC_DYNHOST_GOV;

    /// <summary>
    /// Implements IInstrumentable
    /// </summary>
    [Config(Default = false)]
    [ExternalParameter(CoreConsts.EXT_PARAM_GROUP_LOCKING, CoreConsts.EXT_PARAM_GROUP_INSTRUMENTATION)]
    public override bool InstrumentationEnabled
    {
      get { return m_InstrumentationEnabled; }
      set
      {
        m_InstrumentationEnabled = value;
        if (m_InstrumentationEvent == null)
        {
          if (!value) return;
          m_Stats.Clear();
          m_InstrumentationEvent = new Time.Event(App.EventTimer, null, e => AcceptManagerVisit(this, e.LocalizedTime), INSTRUMENTATION_INTERVAL);
        }
        else
        {
          if (value) return;
          DisposableObject.DisposeAndNull(ref m_InstrumentationEvent);
          m_Stats.Clear();
        }
      }
    }
    #endregion

    #region Public
    public Contracts.DynamicHostID Spawn(Metabank.SectionHost host, string id)
    {
      if (!host.Dynamic) throw new DynamicException("Target host is not dynamic");//todo Move to constant

      var hosts = host.ParentZone.ZoneGovernorHosts;
      return App.GetServiceClientHub().CallWithRetry<Contracts.IZoneHostRegistryClient, Contracts.DynamicHostID>
      (
        (controller) => controller.Spawn(host.RegionPath, id),
        hosts.Select(h => h.RegionPath)
      );
    }

    public string GetHostName(Contracts.DynamicHostID hid)
    {
      var zone = App.GetMetabase().CatalogReg.NavigateZone(hid.Zone);
      var hosts = zone.ZoneGovernorHosts;
      return App.GetServiceClientHub().CallWithRetry<Contracts.IZoneHostReplicatorClient, Contracts.DynamicHostInfo>
      (
        (controller) => controller.GetDynamicHostInfo(hid),
        hosts.Select(h => h.RegionPath)
      ).Host;
    }
    #endregion
  }
}
