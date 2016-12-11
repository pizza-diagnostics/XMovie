using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace XMovie.Common.Properties
{
    public static class FocusProperty
    {
        private static DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached("IsFocused",
                                                typeof(bool),
                                                typeof(FocusProperty),
                                                new UIPropertyMetadata(false, OnIsFocusedPropertyChanged));

        public static bool GetIsFocused(DependencyObject d)
        {
            return (bool)d.GetValue(IsFocusedProperty);
        }

        public static void SetIsFocused(DependencyObject d, bool value)
        {
            d.SetValue(IsFocusedProperty, value);
        }

        private static void OnIsFocusedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = (UIElement)d;
            if ((bool)e.NewValue)
            {
                element.Focus();
            }
        }

    }
}
