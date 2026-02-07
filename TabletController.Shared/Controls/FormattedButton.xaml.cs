using System.Windows.Input;

namespace TabletController.Shared.Controls
{
    public partial class FormattedButton : ContentView
    {
        public static readonly BindableProperty CommandProperty =
            BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(FormattedButton));

        public static readonly BindableProperty CommandParameterProperty =
            BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(FormattedButton));

        public static readonly BindableProperty FormattedTextProperty =
            BindableProperty.Create(nameof(FormattedText), typeof(FormattedString), typeof(FormattedButton),
                propertyChanged: OnFormattedTextChanged);

        public static readonly BindableProperty BorderPaddingProperty =
            BindableProperty.Create(nameof(BorderPadding), typeof(Thickness), typeof(FormattedButton),
                defaultValue: new Thickness(15), propertyChanged: OnBorderPaddingChanged);

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public FormattedString? FormattedText
        {
            get => (FormattedString?)GetValue(FormattedTextProperty);
            set => SetValue(FormattedTextProperty, value);
        }

        public Thickness BorderPadding
        {
            get => (Thickness)GetValue(BorderPaddingProperty);
            set => SetValue(BorderPaddingProperty, value);
        }

        public FormattedButton()
        {
            InitializeComponent();

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnTapped;
            ButtonBorder.GestureRecognizers.Add(tapGesture);
        }

        private static void OnFormattedTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is FormattedButton button && button.ButtonLabel != null)
            {
                button.ButtonLabel.FormattedText = newValue as FormattedString;
            }
        }

        private static void OnBorderPaddingChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is FormattedButton button && button.ButtonBorder != null)
            {
                button.ButtonBorder.Padding = (Thickness)newValue;
            }
        }

        private void OnTapped(object? sender, TappedEventArgs e)
        {
            // Execute command IMMEDIATELY
            if (Command?.CanExecute(CommandParameter) == true)
            {
                Command.Execute(CommandParameter);
            }

            // Run visual feedback concurrently (fire-and-forget)
            _ = AnimateAsync();
        }

        private async Task AnimateAsync()
        {
            await ButtonBorder.FadeTo(0.6, 50);
            await ButtonBorder.FadeTo(1, 50);
        }
    }
}
