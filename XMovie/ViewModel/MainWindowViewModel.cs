using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XMovie.Common;

namespace XMovie.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private Logger logger = Logger.Instace;

        public async void MainWindowLoaded()
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    System.Threading.Thread.Sleep(1000);
                    logger.Debug($"WTF {this}");
                }
            });
        }
    }
}
