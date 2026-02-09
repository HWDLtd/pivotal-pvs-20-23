using Microsoft.Extensions.Logging;
using TabletController.Collect.Pages;
using TabletController.Collect.ViewModels;
using TabletController.Core.Interfaces;
using TabletController.Shared.Services;
using TabletController.Hardware.Services;
#if ANDROID
using TabletController.Hardware.Platforms.Android.Services;
#endif
using IInactivityTimeoutService = TabletController.Shared.Services.IInactivityTimeoutService;

namespace TabletController.Collect
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

            // App-specific services
            builder.Services.AddSingleton<Core.Interfaces.IProductDataService, Services.DemoProductDataService>();

            // Hardware services (platform-specific) - Scanner and Locker only, NO printer
#if ANDROID
            builder.Services.AddSingleton<IBarcodeScanner, BarcodeScannerService>();
#else
            builder.Services.AddSingleton<IBarcodeScanner, StubBarcodeScannerService>();
#endif
            builder.Services.AddSingleton<ILockerService, Etd8a12LockerService>();

            // App-specific ViewModels
            builder.Services.AddSingleton<IdleViewModel>();
            builder.Services.AddTransient<JustAMomentViewModel>();
            builder.Services.AddTransient<SuccessViewModel>();
            builder.Services.AddTransient<ErrorViewModel>();

            // App-specific Pages
            builder.Services.AddSingleton<IdlePage>();
            builder.Services.AddTransient<JustAMomentPage>();
            builder.Services.AddTransient<SuccessPage>();
            builder.Services.AddTransient<ErrorPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
