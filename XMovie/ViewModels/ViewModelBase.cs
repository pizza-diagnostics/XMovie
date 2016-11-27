using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged, IDataErrorInfo
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);

            return true;
        }

        private Dictionary<string, string> errors = new Dictionary<string, string>();

        public string Error
        {
            get { return null; }
        }

        public string this[string columnName]
        {
            get
            {
                return errors.ContainsKey(columnName) ? errors[columnName] : null;
            }
        }

        protected void SetError(string propertyName, string errorMessage)
        {
            errors[propertyName] = errorMessage;
        }

        protected void ClearError(string propertyName)
        {
            if (errors.ContainsKey(propertyName))
            {
                errors.Remove(propertyName);
            }
        }
    }
}
