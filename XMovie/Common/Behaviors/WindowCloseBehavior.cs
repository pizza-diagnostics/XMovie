using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace XMovie.Common.Behaviors
{
    public class WindowCloseBehavior : Behavior<Window>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(WindowCloseBehavior), null);


        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(WindowCloseBehavior), null);

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public static readonly DependencyProperty CloseButtonProperty =
            DependencyProperty.Register("CloseButton",
                typeof(Button),
                typeof(WindowCloseBehavior),
                new FrameworkPropertyMetadata(null, OnButtonChanged));

        public Button CloseButton
        {
            get { return (Button)GetValue(CloseButtonProperty); }
            set { SetValue(CloseButtonProperty, value); }
        }

        private static void OnButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = (Window)((WindowCloseBehavior)d).AssociatedObject;
            ((Button)e.NewValue).Click += (s, e1) =>
            {
                var command = ((WindowCloseBehavior)d).Command;
                var commandParameter = ((WindowCloseBehavior)d).CommandParameter;
                if (command != null)
                {
                    command.Execute(commandParameter);
                }
                window.Close();

            };
        }
    }
}
