namespace TabletController.Shared.Controls
{
    public class CircularProgressDrawable : IDrawable
    {
        public double Progress { get; set; } = 1.0;
        public string Text { get; set; } = string.Empty;
        public Color TrackColor { get; set; } = Colors.LightGray;
        public Color ProgressColor { get; set; } = Color.FromArgb("#231F20");
        public Color TextColor { get; set; } = Color.FromArgb("#231F20");
        public float StrokeWidth { get; set; } = 8f;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var centerX = dirtyRect.Width / 2;
            var centerY = dirtyRect.Height / 2;
            var radius = Math.Min(centerX, centerY) - StrokeWidth / 2 - 2;

            canvas.StrokeColor = TrackColor;
            canvas.StrokeSize = StrokeWidth;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawCircle(centerX, centerY, radius);

            if (Progress > 0)
            {
                canvas.StrokeColor = ProgressColor;
                canvas.StrokeSize = StrokeWidth;
                canvas.StrokeLineCap = LineCap.Round;

                var endAngle = 90f;
                var startAngle = 90f - (float)(Progress * 360);

                canvas.DrawArc(
                    centerX - radius,
                    centerY - radius,
                    radius * 2,
                    radius * 2,
                    startAngle,
                    endAngle,
                    false,
                    false);
            }

            if (!string.IsNullOrEmpty(Text))
            {
                canvas.FontColor = TextColor;
                canvas.FontSize = radius * 0.8f;
                canvas.Font = Microsoft.Maui.Graphics.Font.Default;

                canvas.DrawString(
                    Text,
                    0,
                    0,
                    dirtyRect.Width,
                    dirtyRect.Height,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Center);
            }
        }
    }
}
