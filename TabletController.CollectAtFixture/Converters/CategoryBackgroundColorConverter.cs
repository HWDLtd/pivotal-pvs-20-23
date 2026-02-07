using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using TabletController.Core.DTOs;
using TabletController.CollectAtFixture.ViewModels;

namespace TabletController.CollectAtFixture.Converters
{
    public class CategoryBackgroundColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Category currentCategory)
            {
                ProductListViewModel? viewModel = null;
                
                // Try to get ViewModel from parameter
                if (parameter is ProductListViewModel vm)
                {
                    viewModel = vm;
                }
                // Try to get ViewModel from Element parameter
                else if (parameter is Element element)
                {
                    var parent = element.Parent;
                    while (parent != null && parent is not ContentPage)
                    {
                        parent = parent.Parent;
                    }
                    
                    if (parent is ContentPage page)
                    {
                        viewModel = page.BindingContext as ProductListViewModel;
                    }
                }
                
                if (viewModel != null && viewModel.SelectedCategory?.CategoryId == currentCategory.CategoryId)
                {
                    return Application.Current?.Resources["CategorySelected"] ?? Colors.Black;
                }
            }

            return Application.Current?.Resources["CategoryUnselected"] ?? Colors.White;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

