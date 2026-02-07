namespace TabletController.Shared.Controls
{
    public partial class DrawerCollectionPopup : ContentView
    {
        public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(
            nameof(IsVisible),
            typeof(bool),
            typeof(DrawerCollectionPopup),
            false,
            propertyChanged: OnIsVisibleChanged);

        public DrawerCollectionPopup()
        {
            InitializeComponent();
        }

        public bool IsVisible
        {
            get => (bool)GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        private static void OnIsVisibleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is DrawerCollectionPopup popup && popup.MainGrid != null)
            {
                popup.MainGrid.IsVisible = (bool)newValue;
            }
        }
    }
}
