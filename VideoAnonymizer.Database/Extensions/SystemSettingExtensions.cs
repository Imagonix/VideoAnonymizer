using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Database.Extensions
{
    public static class SystemSettingExtensions
    {
        public static bool ReadBooleanValue(this SystemSetting setting, bool defaultValue = false)
        {
            if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
                return defaultValue;

            return bool.TryParse(setting.Value, out var result)
                ? result
                : defaultValue;
        }

        public static void SetBooleanValue(this SystemSetting setting, bool value)
        {
            setting.Value = value.ToString().ToLowerInvariant();
            setting.UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
