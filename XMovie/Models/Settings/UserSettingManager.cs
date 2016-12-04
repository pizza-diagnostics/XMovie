using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using XMovie.Common;

namespace XMovie.Models.Settings
{
    public sealed class UserSettingManager
    {
        private static readonly Lazy<UserSettingManager> instance =
            new Lazy<UserSettingManager>(() => new UserSettingManager());

        private UserSettings userSettings;

        private Logger logger = Logger.Instace;

        private UserSettingManager()
        {
        }

        public static UserSettingManager Instance
        {
            get { return instance.Value; }
        }

        public UserSettings GetUserSettings()
        {
            lock (this)
            {
                if (userSettings != null)
                {
                    return userSettings;
                }
                try
                {
                    // TODO: 設定読み込み時にManagerで例外をハンドルする?
                    if (File.Exists(Util.UserSettingFilePath))
                    {
                        using (var stream = new FileStream(Util.UserSettingFilePath, FileMode.Open, FileAccess.Read))
                        {
                            var deserializer = new DataContractJsonSerializer(typeof(UserSettings));
                            userSettings = (UserSettings)deserializer.ReadObject(stream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
                finally
                {
                    if (userSettings == null)
                    {
                        userSettings = new UserSettings();
                    }
                }
                return userSettings;
            }
        }

        public void Save()
        {
            lock (this)
            {
                if (userSettings != null)
                {
                    var serializer = new DataContractJsonSerializer(typeof(UserSettings));
                    using (var stream = new FileStream(Util.UserSettingFilePath, FileMode.Create, FileAccess.Write))
                    {
                        serializer.WriteObject(stream, userSettings);
                    }
                }
            }
        }
    }
}
