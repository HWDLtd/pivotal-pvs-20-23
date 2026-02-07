using System.Globalization;
using TabletController.Core.DTOs;

namespace TabletController.SelectAndPay.Converters
{
    public class CategorySelectionConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is Element element)
            {
                if (element.BindingContext is Category currentCategory)
                {
                    if (value is int selectedCategoryId)
                    {
                        return selectedCategoryId == currentCategory.CategoryId;
                    }
                }
                else
                {
                    var parent = element.Parent;
                    while (parent != null && parent.BindingContext is not Category)
                    {
                        parent = parent.Parent;
                    }
                    
                    if (parent?.BindingContext is Category currentCategory2)
                    {
                        if (value is int selectedCategoryId)
                        {
                            return selectedCategoryId == currentCategory2.CategoryId;
                        }
                    }
                }
            }

            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

