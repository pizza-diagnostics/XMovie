using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using XMovie.Models;
using XMovie.Models.Data;
using XMovie.ViewModels;

namespace XMovie.Common
{
    public class TagCommandParameterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // TODO: Converterを整理する


            // Tag
            if (values[0] != null && values[0] != DependencyProperty.UnsetValue && values[0].GetType() == typeof(SearchTagMenuItemViewModel))
            {
                return new TagCommandParameter()
                {
                    Tag = ((SearchTagMenuItemViewModel)values[0]).Tag
                };
            }

            if (values.Length == 2 && 
                (values[0] == null || values[0] == DependencyProperty.UnsetValue) ||
                (values[1] == null || values[1] == DependencyProperty.UnsetValue))
            {
                return null;
            }

            return new TagCommandParameter()
            {
                Name = (string)values[0],
                TagCategoryId = (int)values[1]
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
