namespace TabletController.Shared.Controls
{
    public partial class CircularProgressIndicator : ContentView
    {
        private readonly CircularProgressDrawable _drawable;

        public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
            nameof(Progress),
            typeof(double),
            typeof(CircularProgressIndicator),
            1.0,
            propertyChanged: OnProgressChanged);

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(CircularProgressIndicator),
            string.Empty,
            propertyChanged: OnTextChanged);

        public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
            nameof(TrackColor),
            typeof(Color),
            typeof(CircularProgressIndicator),
            Colors.LightGray,
            propertyChanged: OnTrackColorChanged);

        public static readonly BindableProperty ProgressColorProperty = BindableProperty.Create(
            nameof(ProgressColor),
            typeof(Color),
            typeof(CircularProgressIndicator),
            Color.FromArgb("#231F20"),
            propertyChanged: OnProgressColorChanged);

        public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(CircularProgressIndicator),
            Color.FromArgb("#231F20"),
            propertyChanged: OnTextColorChanged);

        public static readonly BindableProperty StrokeWidthProperty = BindableProperty.Create(
            nameof(StrokeWidth),
            typeof(float),
            typeof(CircularProgressIndicator),
            8f,
            propertyChanged: OnStrokeWidthChanged);

        public CircularProgressIndicator()
        {
            InitializeComponent();
            _drawable = new CircularProgressDrawable();
            ProgressGraphicsView.Drawable = _drawable;
        }

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Color TrackColor
        {
            get => (Color)GetValue(TrackColorProperty);
            set => SetValue(TrackColorProperty, value);
        }

        public Color ProgressColor
        {
            get => (Color)GetValue(ProgressColorProperty);
            set => SetValue(ProgressColorProperty, value);
        }

        public Color TextColor
        {
            get => (Color)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        public float StrokeWidth
        {
            get => (float)GetValue(StrokeWidthProperty);
            set => SetValue(StrokeWidthProperty, value);
        }

        private static void OnProgressChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CircularProgressIndicator indicator)
            {
                indicator._drawable.Progress = (double)newValue;
                indicator.ProgressGraphicsView.Invalidate();
            }
        }

        private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CircularProgressIndicator indicator)
            {
                indicator._drawable.Text = (string)newValue ?? string.Empty;
                indicator.ProgressGraphicsView.Invalidate();
            }
        }

        private static void OnTrackColorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CircularProgressIndicator indicator)
            {
                indicator._drawable.TrackColor = (Color)newValue;
                indicator.ProgressGraphicsView.Invalidate();
            }
        }

        private static void OnProgressColorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CircularProgressIndicator indicator)
            {
                indicator._drawable.ProgressColor = (Color)newValue;
                indicator.ProgressGraphicsView.Invalidate();
            }
        }

        private static void OnTextColorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CircularProgressIndicator indicator)
            {
                indicator._drawable.TextColor = (Color)newValue;
                indicator.ProgressGraphicsView.Invalidate();
            }
        }

        private static void OnStrokeWidthChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CircularProgressIndicator indicator)
            {
                indicator._drawable.StrokeWidth = (float)newValue;
                indicator.ProgressGraphicsView.Invalidate();
            }
        }
    }
}
