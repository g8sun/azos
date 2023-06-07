/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Azos.WinForms.Controls
{

  /// <summary>
  /// A ComboBox that allows to specify the color of the control.
  /// Note that the ForeColor of the parent ComboBox only paints the drop-down list
  /// but not the face color of the ComboBox itself.
  /// </summary>
  public class ComboBoxEx : ComboBox
  {

    public ComboBoxEx()
    {
      base.DrawMode = DrawMode.OwnerDrawFixed;
    }


    private Color m_HighlightColor = Color.Gray;


    new public DrawMode DrawMode
    {
      get { return DrawMode.OwnerDrawFixed; }
      set {}
    }

    [Browsable(true)]
    public Color HighlightColor
    {
      get { return m_HighlightColor;}
      set
      {
        m_HighlightColor = value;
        Refresh();
      }
    }


    protected override void OnDrawItem(DrawItemEventArgs e)
    {
      if (e.Index < 0) return;

      e.Graphics.FillRectangle(
          (e.State & DrawItemState.Selected) == DrawItemState.Selected
              ? new SolidBrush(HighlightColor)
              : new SolidBrush(this.BackColor),
          e.Bounds);

      e.Graphics.DrawString(Items[e.Index].ToString(), e.Font,
                            new SolidBrush(ForeColor),
                            new Point(e.Bounds.X, e.Bounds.Y));

      e.DrawFocusRectangle();

      base.OnDrawItem(e);
    }

  }
}
