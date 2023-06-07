/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Windows.Forms;

using Azos.Financial.Market;

namespace Azos.WinForms.Controls.ChartKit.Temporal
{

  public delegate void ChartPaneMouseEventHandler(object sender, ChartPaneMouseEventArgs args);

  public class ChartPaneMouseEventArgs : EventArgs
  {
    public enum MouseEventType
    {
        /// <summary>
        /// Mouse moved
        /// </summary>
        Move,

        /// <summary>
        /// Mouse was clicked
        /// </summary>
        Click,

        /// <summary>
        /// Mouse did not change but chart content changed under the mouse as-if mouse moved
        /// </summary>
        ChartUpdate
    }


    internal ChartPaneMouseEventArgs( MouseEventType type,
                                      TimeSeriesChart chart,
                                      PlotPane pane,
                                      MouseEventArgs mouse,
                                      ITimeSeriesSample sample,
                                      float value)
    {
      this.EventType = type;
      this.Chart = chart;
      this.Pane = pane;
      this.MouseEvent = mouse;
      this.SampleAtX = sample;
      this.ValueAtY = value;
    }

    public readonly MouseEventType EventType;
    public readonly TimeSeriesChart Chart;
    public readonly PlotPane Pane;
    public readonly MouseEventArgs MouseEvent;
    public readonly ITimeSeriesSample SampleAtX;
    public readonly float ValueAtY;
  }

}
