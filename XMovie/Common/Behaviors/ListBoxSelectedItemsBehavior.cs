using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace XMovie.Common.Behaviors
{
    public class ListBoxSelectedItemsBehavior : Behavior<ListBox>
    {
        private bool IsCleanedUp { get; set; } = false;

        private void CleanUp()
        {
            if (!IsCleanedUp)
            {
                IsCleanedUp = true;

                AssociatedObject.SelectionChanged -= AssociatedObject_SelectionChanged;
                AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged += AssociatedObject_SelectionChanged;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            CleanUp();
        }

        private void AssociatedObject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var array = new object[AssociatedObject.SelectedItems.Count];
            AssociatedObject.SelectedItems.CopyTo(array, 0);
            SelectedItems = new ObservableCollection<object>(array);
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanUp();
        }

        private static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(ObservableCollection<object>), typeof(ListBoxSelectedItemsBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public ObservableCollection<object> SelectedItems
        {
            get { return (ObservableCollection<object>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }
    }
}
