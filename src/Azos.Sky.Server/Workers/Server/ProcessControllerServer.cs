/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System.Collections.Generic;

using Azos.Apps.Injection;

namespace Azos.Sky.Workers.Server
{
  /// <summary>
  /// Glue trampoline for ProcessControllerService
  /// </summary>
  public class ProcessControllerServer : Contracts.IProcessController
  {
    [Inject] IApplication m_App;

    public ProcessControllerService Service => m_App.NonNull(nameof(m_App))
                                              .Singletons
                                              .Get<ProcessControllerService>()
                                              .NonNull(nameof(ProcessControllerService));

    public void Spawn(ProcessFrame frame) => Service.Spawn(frame);

    public ProcessFrame Get(PID pid) => Service.Get(pid);

    public ProcessDescriptor GetDescriptor(PID pid) => Service.GetDescriptor(pid);

    public SignalFrame Dispatch(SignalFrame signal) => Service.Dispatch(signal);

    public IEnumerable<ProcessDescriptor> List(int processorID)=> Service.List(processorID);
  }

}
