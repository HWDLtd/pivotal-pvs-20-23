using System.Windows.Input;

namespace TabletController.Shared.Controls
{
    public partial class InactivityPopup : ContentView
    {
        public static readonly BindableProperty CountdownSecondsProperty = BindableProperty.Create(
            nameof(CountdownSeconds),
            typeof(int),
            typeof(InactivityPopup),
            10,
            propertyChanged: OnCountdownSecondsChanged);

        public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
            nameof(Progress),
            typeof(double),
            typeof(InactivityPopup),
            1.0);

        public static readonly BindableProperty DismissCommandProperty = BindableProperty.Create(
            nameof(DismissCommand),
            typeof(ICommand),
            typeof(InactivityPopup));

        public static readonly BindableProperty NextCustomerCommandProperty = BindableProperty.Create(
            nameof(NextCustomerCommand),
            typeof(ICommand),
            typeof(InactivityPopup));

        public InactivityPopup()
        {
            InitializeComponent();
        }

        public int CountdownSeconds
        {
            get => (int)GetValue(CountdownSecondsProperty);
            set => SetValue(CountdownSecondsProperty, value);
        }

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public ICommand? DismissCommand
        {
            get => (ICommand?)GetValue(DismissCommandProperty);
            set => SetValue(DismissCommandProperty, value);
        }

        public ICommand? NextCustomerCommand
        {
            get => (ICommand?)GetValue(NextCustomerCommandProperty);
            set => SetValue(NextCustomerCommandProperty, value);
        }

        private static void OnCountdownSecondsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is InactivityPopup popup)
            {
                popup.ProgressIndicator.Text = newValue?.ToString() ?? string.Empty;
            }
        }

        private void OnBackgroundTapped(object? sender, TappedEventArgs e)
        {
            if (DismissCommand?.CanExecute(null) == true)
            {
                DismissCommand.Execute(null);
            }
        }

        private void OnNextCustomerClicked(object? sender, EventArgs e)
        {
            if (NextCustomerCommand?.CanExecute(null) == true)
            {
                NextCustomerCommand.Execute(null);
            }
        }
    }
}
