using Microsoft.Extensions.Logging;
using TabletController.SelectAndPay.Pages;
using TabletController.SelectAndPay.Services;
using TabletController.SelectAndPay.ViewModels;
using TabletController.Core.Interfaces;
using TabletController.Shared.Services;
using TabletController.Hardware.Services;
#if ANDROID
using TabletController.Hardware.Platforms.Android.Services;
#endif
using IInactivityTimeoutService = TabletController.Shared.Services.IInactivityTimeoutService;

namespace TabletController.SelectAndPay
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
            builder.Services.AddSingleton<Core.Interfaces.IProductDataService, DemoProductDataService>();
            builder.Services.AddSingleton<Core.Interfaces.IReceiptService, ReceiptService>();

            // Hardware services (platform-specific)
#if ANDROID
            builder.Services.AddSingleton<IBarcodeScanner, BarcodeScannerService>();
            builder.Services.AddSingleton<IPrinterService, EpsonPrinterService>();
#else
            builder.Services.AddSingleton<IBarcodeScanner, StubBarcodeScannerService>();
            builder.Services.AddSingleton<IPrinterService, StubPrinterService>();
#endif

            // App-specific ViewModels
            builder.Services.AddSingleton<IdleViewModel>();
            builder.Services.AddSingleton<ProductListViewModel>();
            builder.Services.AddTransient<ProductDetailViewModel>();
            builder.Services.AddTransient<PaymentViewModel>();
            builder.Services.AddTransient<ThankYouViewModel>();
            builder.Services.AddTransient<ReceiptViewModel>();

            // App-specific Pages
            builder.Services.AddSingleton<IdlePage>();
            builder.Services.AddSingleton<ProductListPage>();
            builder.Services.AddTransient<ProductDetailPage>();
            builder.Services.AddTransient<PaymentPage>();
            builder.Services.AddTransient<ThankYouPage>();
            builder.Services.AddTransient<ReceiptPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
