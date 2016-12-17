using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using XMovie.ViewModels;

namespace XMovie.Common.Behaviors
{
    public class ListBoxSelectedMovieItemsBehavior : ListBoxSelectedItemsBehavior<MovieItemViewModel> { }

    public class ListBoxSelectedItemsBehavior<T> : Behavior<ListBox>
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
            var array = new T[AssociatedObject.SelectedItems.Count];
            AssociatedObject.SelectedItems.CopyTo(array, 0);
            SelectedItems = new ObservableCollection<T>(array);
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanUp();
        }

        private static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(ObservableCollection<T>), typeof(ListBoxSelectedItemsBehavior<T>),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public ObservableCollection<T> SelectedItems
        {
            get { return (ObservableCollection<T>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }
    }
}
