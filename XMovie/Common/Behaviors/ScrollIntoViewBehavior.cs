using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace XMovie.Common.Behaviors
{
    public class ScrollIntoViewBehavior : Behavior<ListView>
    {
        private bool IsCleanedUp { get; set; } = false;

        protected override void OnAttached()
        {
            base.OnAttached();
            ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged += AssociatedObject_CollectionChanged;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
        }

        private void AssociatedObject_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                AssociatedObject.ScrollIntoView(e.NewItems[0]);
            }
        }

        private void AssociatedObject_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            CleanUp();
        }

        protected override void OnDetaching()
        {
            CleanUp();
            base.OnDetaching();
        }

        private void CleanUp()
        {
            if (!IsCleanedUp)
            {
                IsCleanedUp = true;
                ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged -= AssociatedObject_CollectionChanged;
            }
        }
    }
}
