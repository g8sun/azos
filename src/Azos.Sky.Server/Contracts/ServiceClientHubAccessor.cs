/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;

using Azos.Conf;
using Azos.Sky.Contracts;

namespace Azos.Sky //for convenience it is in the root SKY namespace
{
  /// <summary>
  /// Provides access to ServiceClientHub singleton
  /// </summary>
  public static class ServiceClientHubAccessor
  {
    /// <summary>
    /// Provides access to ServiceClientHub singleton instance of the app context
    /// </summary>
    public static ServiceClientHub GetServiceClientHub(this IApplication app)
      => app.NonNull(nameof(app))
            .Singletons
            .GetOrCreate(() =>
            {
              string tpn = typeof(ServiceClientHub).FullName;
              try
              {
                var mbNode = app.AsSky().Metabase.ServiceClientHubConfNode as ConfigSectionNode;
                var appNode = app.ConfigRoot[SysConsts.APPLICATION_CONFIG_ROOT_SECTION]
                                            [ServiceClientHub.CONFIG_SERVICE_CLIENT_HUB_SECTION] as ConfigSectionNode;

                var effectiveConf = new MemoryConfiguration();
                effectiveConf.CreateFromMerge(mbNode, appNode);
                var effective = effectiveConf.Root;

                tpn = effective.AttrByName(FactoryUtils.CONFIG_TYPE_ATTR).ValueAsString(typeof(ServiceClientHub).FullName);

                return FactoryUtils.MakeComponent<ServiceClientHub>(app, effective, typeof(ServiceClientHub), new object[] { effective });
              }
              catch (Exception error)
              {
                throw new Clients.SkyClientException(ServerStringConsts.SKY_SVC_CLIENT_HUB_SINGLETON_CTOR_ERROR
                                                                 .Args(tpn, error.ToMessageWithType()), error);
              }
            }).instance;
  }

}
