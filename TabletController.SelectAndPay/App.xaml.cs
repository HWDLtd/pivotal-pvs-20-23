using TabletController.Core.Interfaces;
using Microsoft.Maui.ApplicationModel;
#if ANDROID
using Android;
using Android.Content;
using Android.OS;
using AndroidX.Core.Content;
#endif

namespace TabletController.SelectAndPay
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        public App(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        protected override void OnStart()
        {
            base.OnStart();
            
            // Initialize hardware services at app startup
            // This ensures services are initialized regardless of which page loads first
            _ = InitializeServicesAsync();
        }

        protected override void OnResume()
        {
            base.OnResume();
            
#if ANDROID
            // Check if file permissions were granted while app was in background
            // (e.g., user granted permission in Settings)
            _ = CheckAndReloadDataAsync();
#endif
        }

#if ANDROID
        private async Task CheckAndReloadDataAsync()
        {
            try
            {
                // Check if root ITAB folder exists and we now have permission
                var externalStorage = Android.OS.Environment.ExternalStorageDirectory;
                if (externalStorage != null)
                {
                    var rootItabPath = System.IO.Path.Combine(externalStorage.AbsolutePath, "ITAB");
                    if (System.IO.Directory.Exists(rootItabPath))
                    {
                        // Check if we have permission now
                        bool hasPermission = false;
                        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                        {
                            hasPermission = Android.OS.Environment.IsExternalStorageManager;
                        }
                        else
                        {
                            var context = Platform.CurrentActivity ?? Platform.AppContext;
                            if (context != null)
                            {
                                hasPermission = ContextCompat.CheckSelfPermission(context, Manifest.Permission.ReadExternalStorage) 
                                    == Android.Content.PM.Permission.Granted;
                            }
                        }
                        
                        if (hasPermission)
                        {
                            System.Diagnostics.Debug.WriteLine("File permission granted, reloading product data...");
                            // Reload data from the service
                            var productDataService = Services.GetService<Core.Interfaces.IProductDataService>();
                            if (productDataService is Services.DemoProductDataService demoService)
                            {
                                demoService.LoadData();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking and reloading data: {ex.Message}");
            }
        }
#endif

        private async Task InitializeServicesAsync()
        {
            try
            {
                // Step 1: Request file permissions first (for Android 11+)
                // Wait for permissions to be granted before continuing
                var filePermissionGranted = await RequestFilePermissionsAsync().ConfigureAwait(false);
                if (!filePermissionGranted)
                {
                    System.Diagnostics.Debug.WriteLine("File permissions not granted, continuing anyway...");
                }
                
                var printerService = Services.GetService<IPrinterService>();
                var scannerService = Services.GetService<IBarcodeScanner>();

                if (printerService == null || scannerService == null)
                    return;

                // Step 2: Initialize both services together
                await printerService.InitializeAsync().ConfigureAwait(false);
                var scannerInitialized = await scannerService.InitializeAsync().ConfigureAwait(false);
                
                // Connect printer (this will request permission if needed)
                var printerConnected = await printerService.ConnectAsync().ConfigureAwait(false);
                
                System.Diagnostics.Debug.WriteLine($"Printer ConnectAsync returned: {printerConnected}, IsConnected: {printerService.IsConnected}");
                
                // If printer permission was requested (not connected yet), wait for it to be granted
                // Android can only show one permission dialog at a time, so we must wait
                // for the printer permission dialog to be dismissed before requesting scanner permission
                if (!printerService.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine("Printer not connected, waiting for permission dialog...");
                    
                    // Give the permission dialog time to appear (longer delay to ensure it's shown)
                    await Task.Delay(1000).ConfigureAwait(false);
                    
                    // Poll for printer connection status with shorter timeout
                    // This waits for the user to grant or deny printer permission
                    // We use a shorter timeout (10 seconds) to avoid waiting too long if denied
                    var maxWaitTime = TimeSpan.FromSeconds(10);
                    var pollInterval = TimeSpan.FromMilliseconds(300);
                    var startTime = DateTime.UtcNow;
                    var wasConnected = false;
                    
                    while (!printerService.IsConnected && (DateTime.UtcNow - startTime) < maxWaitTime)
                    {
                        await Task.Delay(pollInterval).ConfigureAwait(false);
                        if (printerService.IsConnected)
                        {
                            wasConnected = true;
                            break;
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"After polling, printer IsConnected: {printerService.IsConnected}");
                    
                    // Wait for dialog to fully dismiss before requesting next permission
                    // This is critical - Android must fully dismiss one dialog before showing another
                    if (printerService.IsConnected)
                    {
                        System.Diagnostics.Debug.WriteLine("Printer permission granted, waiting for dialog to dismiss...");
                        await Task.Delay(1000).ConfigureAwait(false);
                    }
                    else
                    {
                        // Permission was denied or timed out, still proceed with scanner
                        // Give enough time for any denial dialog/message to dismiss
                        System.Diagnostics.Debug.WriteLine("Printer permission denied or timed out, waiting before scanner...");
                        await Task.Delay(1500).ConfigureAwait(false);
                    }
                }
                else
                {
                    // Printer already has permission, short delay to ensure UI is ready
                    System.Diagnostics.Debug.WriteLine("Printer already connected, short delay before scanner...");
                    await Task.Delay(300).ConfigureAwait(false);
                }
                
                // Now request scanner permission (printer permission should be resolved)
                // The delay/polling ensures that if printer permission was requested,
                // the scanner permission request will happen after the user responds
                // Ensure this happens on the main thread for proper permission dialog display
                if (scannerInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("Requesting scanner permission...");
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await scannerService.StartAsync().ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Service initialization failed: {ex.Message}");
            }
        }

#if ANDROID
        private async Task<bool> RequestFilePermissionsAsync()
        {
            try
            {
                var context = Platform.CurrentActivity ?? Platform.AppContext;
                if (context == null)
                {
                    System.Diagnostics.Debug.WriteLine("No Android context available for file permissions");
                    return false;
                }

                // Always check if permissions are already granted first
                // Only request if not already granted, regardless of folder existence
                
                // For Android 11+ (API 30+), need MANAGE_EXTERNAL_STORAGE
                if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                {
                    if (!Android.OS.Environment.IsExternalStorageManager)
                    {
                        System.Diagnostics.Debug.WriteLine("Requesting MANAGE_EXTERNAL_STORAGE permission...");
                        
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            try
                            {
                                var intent = new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
                                var uri = Android.Net.Uri.Parse($"package:{context.PackageName}");
                                intent.SetData(uri);
                                
                                if (context is Android.App.Activity activity)
                                {
                                    activity.StartActivity(intent);
                                }
                                else
                                {
                                    intent.AddFlags(ActivityFlags.NewTask);
                                    context.StartActivity(intent);
                                }
                                
                                System.Diagnostics.Debug.WriteLine("Opened Settings for MANAGE_EXTERNAL_STORAGE permission");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error opening Settings: {ex.Message}");
                            }
                        });
                        
                        // For Android 11+, we open Settings and wait for user to return
                        // We can't reliably poll while app is in background, so we'll check on resume
                        System.Diagnostics.Debug.WriteLine("Opened Settings for MANAGE_EXTERNAL_STORAGE permission");
                        System.Diagnostics.Debug.WriteLine("Waiting for user to grant permission and return to app...");
                        
                        // Wait a reasonable time for user to grant permission and return
                        // The OnResume handler will check and reload data when user returns
                        var maxWaitTime = TimeSpan.FromSeconds(180); // 3 minutes
                        var pollInterval = TimeSpan.FromMilliseconds(1000);
                        var startTime = DateTime.UtcNow;
                        
                        while (!Android.OS.Environment.IsExternalStorageManager && (DateTime.UtcNow - startTime) < maxWaitTime)
                        {
                            await Task.Delay(pollInterval).ConfigureAwait(false);
                            
                            // Check if permission was granted
                            if (Android.OS.Environment.IsExternalStorageManager)
                            {
                                System.Diagnostics.Debug.WriteLine("MANAGE_EXTERNAL_STORAGE permission granted!");
                                // Wait a bit for app to come to foreground
                                await Task.Delay(1000).ConfigureAwait(false);
                                
                                // Reload data now that permission is granted
                                var productDataService = Services.GetService<Core.Interfaces.IProductDataService>();
                                if (productDataService is Services.DemoProductDataService demoService)
                                {
                                    System.Diagnostics.Debug.WriteLine("Reloading product data after permission grant...");
                                    demoService.LoadData();
                                }
                                
                                return true;
                            }
                        }
                        
                        if (Android.OS.Environment.IsExternalStorageManager)
                        {
                            System.Diagnostics.Debug.WriteLine("MANAGE_EXTERNAL_STORAGE permission granted!");
                            return true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("MANAGE_EXTERNAL_STORAGE permission not granted or timed out");
                            System.Diagnostics.Debug.WriteLine("Will check again when app resumes");
                            return false;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("MANAGE_EXTERNAL_STORAGE permission already granted");
                        return true;
                    }
                }
                else
                {
                    // Android 10 and below - request READ_EXTERNAL_STORAGE
                    if (ContextCompat.CheckSelfPermission(context, Manifest.Permission.ReadExternalStorage) 
                        != Android.Content.PM.Permission.Granted)
                    {
                        System.Diagnostics.Debug.WriteLine("Requesting READ_EXTERNAL_STORAGE permission...");
                        
                        var permissionGranted = false;
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            if (context is Android.App.Activity activity)
                            {
                                AndroidX.Core.App.ActivityCompat.RequestPermissions(
                                    activity,
                                    new[] { Manifest.Permission.ReadExternalStorage },
                                    1000);
                            }
                        });
                        
                        // Poll for permission to be granted (wait up to 30 seconds)
                        System.Diagnostics.Debug.WriteLine("Waiting for user to grant READ_EXTERNAL_STORAGE permission...");
                        var maxWaitTime = TimeSpan.FromSeconds(30);
                        var pollInterval = TimeSpan.FromMilliseconds(500);
                        var startTime = DateTime.UtcNow;
                        
                        while (!permissionGranted && (DateTime.UtcNow - startTime) < maxWaitTime)
                        {
                            await Task.Delay(pollInterval).ConfigureAwait(false);
                            if (ContextCompat.CheckSelfPermission(context, Manifest.Permission.ReadExternalStorage) 
                                == Android.Content.PM.Permission.Granted)
                            {
                                permissionGranted = true;
                            }
                        }
                        
                        if (permissionGranted)
                        {
                            System.Diagnostics.Debug.WriteLine("READ_EXTERNAL_STORAGE permission granted!");
                            await Task.Delay(500).ConfigureAwait(false);
                            return true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("READ_EXTERNAL_STORAGE permission not granted or timed out");
                            return false;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("READ_EXTERNAL_STORAGE permission already granted");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error requesting file permissions: {ex.Message}");
                return false;
            }
        }
#else
        private Task<bool> RequestFilePermissionsAsync()
        {
            return Task.FromResult(true);
        }
#endif
    }
}