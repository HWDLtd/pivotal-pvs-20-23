using Microsoft.Extensions.Logging;
using TabletController.CollectAtFixture.Pages;
using TabletController.CollectAtFixture.Services;
using TabletController.CollectAtFixture.ViewModels;
using TabletController.Core.Interfaces;
#if ANDROID
using TabletController.Hardware.Platforms.Android.Services;
#endif
using TabletController.Shared.Services;
using TabletController.Hardware.Services;
using IInactivityTimeoutService = TabletController.Shared.Services.IInactivityTimeoutService;

namespace TabletController.CollectAtFixture
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("Hikasami-Bold.ttf", "HikasamiBold");
                    fonts.AddFont("Hikasami-Regular.ttf", "HikasamiRegular");
                    fonts.AddFont("Hikasami-SemiBold.ttf", "HikasamiSemiBold");
                    fonts.AddFont("Hikasami-Medium.ttf", "HikasamiMedium");
                    fonts.AddFont("Hikasami-VF.ttf", "HikasamiVF");
                });

            // Shared services
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<IInactivityTimeoutService, InactivityTimeoutService>();

            // App-specific services (reused from SelectAndPay)
            builder.Services.AddSingleton<Core.Interfaces.IProductDataService, DemoProductDataService>();

            // Hardware services (platform-specific) - Scanner and Locker only, NO printer
#if ANDROID
            builder.Services.AddSingleton<IBarcodeScanner, BarcodeScannerService>();
#else
            builder.Services.AddSingleton<IBarcodeScanner, StubBarcodeScannerService>();
#endif
            builder.Services.AddSingleton<ILockerService, Etd8a12LockerService>();

            // App-specific ViewModels (reused from SelectAndPay for product browsing)
            builder.Services.AddSingleton<IdleViewModel>();
            builder.Services.AddSingleton<ProductListViewModel>();
            builder.Services.AddTransient<ProductDetailViewModel>();
            builder.Services.AddTransient<PaymentViewModel>();
            builder.Services.AddTransient<AllDoneViewModel>();

            // App-specific Pages
            builder.Services.AddSingleton<IdlePage>();
            builder.Services.AddSingleton<ProductListPage>();
            builder.Services.AddTransient<ProductDetailPage>();
            builder.Services.AddTransient<PaymentPage>();
            builder.Services.AddTransient<AllDonePage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
