using System.Windows.Input;
using TabletController.Core.DTOs;

namespace TabletController.Shared.Controls
{
    public partial class ProductCard : Border
    {
        public static readonly BindableProperty ProductProperty =
            BindableProperty.Create(
                nameof(Product),
                typeof(ProductSummary),
                typeof(ProductCard),
                null);

        public ProductSummary? Product
        {
            get => (ProductSummary?)GetValue(ProductProperty);
            set => SetValue(ProductProperty, value);
        }

        public static readonly BindableProperty ShowPriceAsButtonProperty =
            BindableProperty.Create(
                nameof(ShowPriceAsButton),
                typeof(bool),
                typeof(ProductCard),
                true,
                propertyChanged: OnShowPriceAsButtonChanged);

        public bool ShowPriceAsButton
        {
            get => (bool)GetValue(ShowPriceAsButtonProperty);
            set => SetValue(ShowPriceAsButtonProperty, value);
        }

        public bool ShowPriceAsText => !ShowPriceAsButton;

        public static readonly BindableProperty CommandProperty =
            BindableProperty.Create(
                nameof(Command),
                typeof(ICommand),
                typeof(ProductCard),
                null);

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly BindableProperty CommandParameterProperty =
            BindableProperty.Create(
                nameof(CommandParameter),
                typeof(object),
                typeof(ProductCard),
                null);

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public ProductCard()
        {
            InitializeComponent();

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnTapped;
            GestureRecognizers.Add(tapGesture);
        }

        private async void OnTapped(object? sender, TappedEventArgs e)
        {
            // Visual feedback
            await this.FadeTo(0.7, 100);
            await this.FadeTo(1, 100);

            // Execute command
            if (Command?.CanExecute(CommandParameter) == true)
            {
                Command.Execute(CommandParameter);
            }
        }

        private static void OnShowPriceAsButtonChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ProductCard card)
            {
                card.OnPropertyChanged(nameof(ShowPriceAsText));
            }
        }
    }
}
