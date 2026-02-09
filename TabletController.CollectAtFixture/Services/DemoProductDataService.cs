using System.Text.Json;
using TabletController.Core.DTOs;
using TabletController.Core.Interfaces;
#if ANDROID
using Android.OS;
#endif
using Microsoft.Maui.Storage;

namespace TabletController.CollectAtFixture.Services
{
    public class DemoProductDataService : IProductDataService
    {
        private List<Category> _categories;
        private List<Product> _products;
        private readonly object _lockObject = new object();

        public DemoProductDataService()
        {
            _categories = new List<Category>();
            _products = new List<Product>();
            
            // Try to load data, but don't fail if permissions aren't granted yet
            // Data will be reloaded after permissions are granted
            try
            {
                LoadData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initial data load failed (may need permissions): {ex.Message}");
            }
        }

        public void LoadData()
        {
            lock (_lockObject)
            {
                try
                {
                    // DISABLED: Local file sync - currently using bundled package resource only.
                    // To re-enable loading from local filesystem, uncomment the line below:
                    // CheckAndUpdateDemoDataFile();
                    
                    var demoData = LoadDemoDataAsync().GetAwaiter().GetResult();
                    _categories = demoData.Categories ?? new List<Category>();
                    _products = demoData.Products ?? new List<Product>();
                    System.Diagnostics.Debug.WriteLine($"Loaded {_categories.Count} categories and {_products.Count} products");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load demo data: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Keep existing data if reload fails, or initialize empty lists
                    if (_categories == null) _categories = new List<Category>();
                    if (_products == null) _products = new List<Product>();
                }
            }
        }

        private void CheckAndUpdateDemoDataFile()
        {
            try
            {
                var dataDirectory = GetPublicDataDirectory();
                var dataFilePath = Path.Combine(dataDirectory, "demodata.json");
                
                // Ensure directory exists
                if (!Directory.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                }
                
                // Read raw resource file
                DemoData? rawDemoData = null;
                try
                {
                    using var stream = FileSystem.OpenAppPackageFileAsync("demodata.json").GetAwaiter().GetResult();
                    using var reader = new StreamReader(stream);
                    var rawJson = reader.ReadToEnd();
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    rawDemoData = JsonSerializer.Deserialize<DemoData>(rawJson, options);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading raw demodata.json: {ex.Message}");
                    return;
                }
                
                if (rawDemoData == null)
                {
                    System.Diagnostics.Debug.WriteLine("Raw demodata.json is null or invalid");
                    return;
                }
                
                var rawVersionCode = rawDemoData.VersionCode;
                System.Diagnostics.Debug.WriteLine($"Raw demodata.json version code: {rawVersionCode}");
                
                // Check if file exists on disk
                bool shouldUpdate = false;
                
                if (!File.Exists(dataFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("demodata.json does not exist on disk, will create it");
                    shouldUpdate = true;
                }
                else
                {
                    // File exists, check its version
                    try
                    {
                        var json = File.ReadAllText(dataFilePath);
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var existingData = JsonSerializer.Deserialize<DemoData>(json, options);
                        
                        if (existingData == null || existingData.VersionCode == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("Existing demodata.json has no version code or is invalid, will update");
                            shouldUpdate = true;
                        }
                        else if (existingData.VersionCode < rawVersionCode)
                        {
                            System.Diagnostics.Debug.WriteLine($"Existing version code ({existingData.VersionCode}) is less than raw version ({rawVersionCode}), will update");
                            shouldUpdate = true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Existing version code ({existingData.VersionCode}) is up to date");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error reading existing demodata.json: {ex.Message}, will update");
                        shouldUpdate = true;
                    }
                }
                
                // Update file if needed
                if (shouldUpdate)
                {
                    try
                    {
                        using var stream = FileSystem.OpenAppPackageFileAsync("demodata.json").GetAwaiter().GetResult();
                        using var reader = new StreamReader(stream);
                        var rawJson = reader.ReadToEnd();
                        
                        File.WriteAllText(dataFilePath, rawJson);
                        System.Diagnostics.Debug.WriteLine($"Updated demodata.json with version code {rawVersionCode}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error writing demodata.json: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CheckAndUpdateDemoDataFile: {ex.Message}");
            }
        }

        private async Task<DemoData> LoadDemoDataAsync()
        {
            // DISABLED: Local file loading - currently using bundled package resource only.
            // To re-enable loading from local filesystem, uncomment the block below and
            // comment out the bundled resource block.
            
            // --- START LOCAL FILE LOADING (DISABLED) ---
            // var dataDirectory = GetPublicDataDirectory();
            // var dataFilePath = Path.Combine(dataDirectory, "demodata.json");
            // 
            // System.Diagnostics.Debug.WriteLine($"Looking for demodata.json at: {dataFilePath}");
            // 
            // if (File.Exists(dataFilePath))
            // {
            //     try
            //     {
            //         var json = File.ReadAllText(dataFilePath);
            //         var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            //         return JsonSerializer.Deserialize<DemoData>(json, options) ?? new DemoData();
            //     }
            //     catch (Exception ex)
            //     {
            //         System.Diagnostics.Debug.WriteLine($"Error reading demodata.json: {ex.Message}");
            //         return new DemoData();
            //     }
            // }
            // 
            // System.Diagnostics.Debug.WriteLine($"demodata.json not found at: {dataFilePath}");
            // return new DemoData();
            // --- END LOCAL FILE LOADING (DISABLED) ---

            // Load from bundled app package resource
            try
            {
                System.Diagnostics.Debug.WriteLine("Loading demodata.json from bundled app package resource");
                using var stream = await FileSystem.OpenAppPackageFileAsync("demodata.json").ConfigureAwait(false);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync().ConfigureAwait(false);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return JsonSerializer.Deserialize<DemoData>(json, options) ?? new DemoData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading bundled demodata.json: {ex.Message}");
                return new DemoData();
            }
        }

        private string GetPublicDataDirectory()
        {
#if ANDROID
            // First, try to use ITAB folder at root of external storage (if user created it manually)
            // Path: /storage/emulated/0/ITAB
            // This persists across uninstalls but requires MANAGE_EXTERNAL_STORAGE permission on Android 11+
            var externalStorage = Android.OS.Environment.ExternalStorageDirectory;
            if (externalStorage != null)
            {
                var rootItabPath = Path.Combine(externalStorage.AbsolutePath, "ITAB");
                // If the folder exists, use it (user created it manually)
                if (Directory.Exists(rootItabPath))
                {
                    return rootItabPath;
                }
            }
            
            // Fallback: Use app's external files directory
            // Path: /storage/emulated/0/Android/data/[package]/files/ITAB
            // This works without permissions but gets deleted on uninstall
            var externalFilesDir = Android.App.Application.Context.GetExternalFilesDir(null);
            if (externalFilesDir != null)
            {
                var itabFolder = Path.Combine(externalFilesDir.AbsolutePath, "ITAB");
                return itabFolder;
            }
            
            // Last resort: internal files directory
            var internalFilesDir = Android.App.Application.Context.FilesDir;
            var fallbackFolder = Path.Combine(internalFilesDir.AbsolutePath, "ITAB");
            return fallbackFolder;
#elif WINDOWS
            // Use ITAB folder in user's home directory
            var userProfile = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            return Path.Combine(userProfile, "ITAB");
#else
            // Fallback to app data directory for other platforms
            return FileSystem.AppDataDirectory;
#endif
        }

        public Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            return Task.FromResult<IEnumerable<Category>>(_categories);
        }

        public Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            var products = _products.Where(p => p.CategoryId == categoryId);
            return Task.FromResult(products);
        }

        public Task<Product?> GetProductByIdAsync(int productId)
        {
            var product = _products.FirstOrDefault(p => p.ProductId == productId);
            return Task.FromResult(product);
        }

        public Task<Product?> GetProductByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return Task.FromResult<Product?>(null);
            }

            var product = _products.FirstOrDefault(p => 
                (!string.IsNullOrWhiteSpace(p.Barcode) && 
                 p.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(p.CollectionCode) && 
                 p.CollectionCode.Equals(barcode, StringComparison.OrdinalIgnoreCase)));
            return Task.FromResult(product);
        }

        public LockerConfiguration? GetLockerConfiguration()
        {
            lock (_lockObject)
            {
                try
                {
                    var demoData = LoadDemoDataAsync().GetAwaiter().GetResult();
                    return demoData.Locker;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load locker configuration: {ex.Message}");
                    return null;
                }
            }
        }
    }
}

