using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace XMovie.Common
{
    public class ScrollIntoViewBehavior : Behavior<ListView>
    {
        protected override void OnAttached()
        {
            var listView = AssociatedObject;
            ((INotifyCollectionChanged)listView.Items).CollectionChanged += ScrollIntoViewBehavior_CollectionChanged;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            var listView = AssociatedObject;
            ((INotifyCollectionChanged)listView.Items).CollectionChanged -= ScrollIntoViewBehavior_CollectionChanged;
            base.OnDetaching();
        }

        private void ScrollIntoViewBehavior_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var listView = AssociatedObject;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                listView.ScrollIntoView(e.NewItems[0]);
            }
        }
    }
}
