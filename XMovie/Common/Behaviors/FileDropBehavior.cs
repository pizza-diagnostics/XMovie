using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using static System.Diagnostics.Debug;

namespace XMovie.Common.Behaviors
{
    public class FileDropBehavior : Behavior<FrameworkElement>
    {
        private bool IsCleanedUp { get; set; } = false;

        public ICommand DropCommand
        {
            get { return (ICommand)GetValue(DropCommandProperty); }
            set { SetValue(DropCommandProperty, value); }
        }
        private static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.Register("DropCommand", typeof(ICommand), typeof(FileDropBehavior), new PropertyMetadata());
         
        private void CleanUp()
        {
            if (!IsCleanedUp)
            {
                IsCleanedUp = true;

                AssociatedObject.DragOver -= AssociatedObject_DragOver;
                AssociatedObject.Drop -= AssociatedObject_Drop;
                AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
            }
        }


        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.DragOver += AssociatedObject_DragOver;
            AssociatedObject.Drop += AssociatedObject_Drop;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
        }

        private void AssociatedObject_Drop(object sender, DragEventArgs e)
        {
            var args = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (args != null)
            {
                DropCommand.Execute(args);
            }
        }

        private void AssociatedObject_DragOver(object sender, DragEventArgs e)
        {
            if (e.AllowedEffects.HasFlag(DragDropEffects.Copy) && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanUp();
        }
    }
}
