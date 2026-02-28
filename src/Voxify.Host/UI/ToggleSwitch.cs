using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Voxify.UI;

/// <summary>
/// Modern toggle switch control (like iOS/Android switch).
/// </summary>
public class ToggleSwitch : CheckBox
{
    private Rectangle _thumbRect;
    private Rectangle _trackRect;
    private const int ThumbSize = 20;
    private const int TrackHeight = 24;
    private const int TrackWidth = 44;

    public ToggleSwitch()
    {
        Appearance = Appearance.Button;
        AutoSize = false;
        Size = new Size(TrackWidth + 10, TrackHeight + 4);
        Padding = new Padding(0);
        Margin = new Padding(0);
        FlatStyle = FlatStyle.Flat;
        TextAlign = ContentAlignment.MiddleRight;
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Calculate rectangles
        _trackRect = new Rectangle(5, (Height - TrackHeight) / 2, TrackWidth, TrackHeight);
        int thumbX = Checked ? (_trackRect.Right - ThumbSize - 2) : (_trackRect.Left + 2);
        _thumbRect = new Rectangle(thumbX, (Height - ThumbSize) / 2, ThumbSize, ThumbSize);

        // Draw track (background)
        using (var trackBrush = new SolidBrush(Checked ? Color.FromArgb(0, 120, 215) : Color.Gray))
        {
            g.FillEllipse(trackBrush, _trackRect);
        }

        // Draw thumb (circle)
        using (var thumbBrush = new SolidBrush(Checked ? Color.White : Color.WhiteSmoke))
        {
            g.FillEllipse(thumbBrush, _thumbRect);
        }

        // Draw thumb border
        using (var thumbPen = new Pen(Color.LightGray, 1))
        {
            g.DrawEllipse(thumbPen, _thumbRect);
        }
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);
        Invalidate(); // Redraw control
    }
}
