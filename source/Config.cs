using GTA;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace DualWield
{
    public class Config : Script
    {
        public static string GetPath()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        public static readonly ScriptSettings iniFile = ScriptSettings.Load(Path.Combine(GetPath(), "DualWield.ini"));
        public static readonly Keys toggleKey = iniFile.GetValue("Config", "ToggleKey", Keys.X, CultureInfo.InvariantCulture);
        public static readonly bool useFPCam = iniFile.GetValue("Config", "FirstPersonCam", false, CultureInfo.InvariantCulture);
        public static readonly float fov = iniFile.GetValue("Config", "FirstPersonFOV", 70f, CultureInfo.InvariantCulture);
        public static readonly float dmg = iniFile.GetValue("Config", "DamageMult", 5f, CultureInfo.InvariantCulture);
        public static readonly float spread = iniFile.GetValue("Config", "BulletSpread", 1.0f, CultureInfo.InvariantCulture);
        public static readonly int pistolAnim = iniFile.GetValue("Config", "PistolAnimStyle", 0, CultureInfo.InvariantCulture);
        public static readonly bool noWalkAnim = iniFile.GetValue("Config", "DisableWalkingAnim", false, CultureInfo.InvariantCulture);
        public static readonly bool noRunAnim = iniFile.GetValue("Config", "DisableRunningAnim", false, CultureInfo.InvariantCulture);
        public static readonly bool noIdleAnim = iniFile.GetValue("Config", "DisableIdleAnim", false, CultureInfo.InvariantCulture);
        public static readonly bool useSecondary = iniFile.GetValue("Config", "SecondaryFlagAnim", false, CultureInfo.InvariantCulture);
        public static readonly bool useHUD = iniFile.GetValue("Config", "ShowAmmoHUD", true, CultureInfo.InvariantCulture);
        public static readonly bool useCrosshair = iniFile.GetValue("Config", "ShowCrosshair", true, CultureInfo.InvariantCulture);
        public static readonly bool debugLog = iniFile.GetValue("Config", "DebugLog", false, CultureInfo.InvariantCulture);
    }
}
