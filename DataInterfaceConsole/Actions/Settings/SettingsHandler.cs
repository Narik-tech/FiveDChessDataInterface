using FiveDChessDataInterface.MemoryHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataInterfaceConsole.Actions.Settings
{
    class SettingsHandler
    {
        const string settingsFilePath = "settings.json";

        public ISettingsValue[] GetSettings() => this.settingsStore.Values.ToArray();

        private readonly Dictionary<string, ISettingsValue> settingsStore = new Dictionary<string, ISettingsValue>(); // string : SettingsValue<T>
        private void AddSetting(ISettingsValue s)
        {
            this.settingsStore.Add(s.Id, s);
        }

        private ISettingsValue GetSetting(string Id) => this.settingsStore[Id];


        public SettingsHandler()
        {
            AddSetting(new SettingsValueWhitelisted<string>("ForceTimetravelAnimationValue", "Force timetravel animation value", "Whether or not to force the timetravel button to a certain state",
                new[] { "ignore", "always_on", "always_off" }, "ignore"));

            AddSetting(new SettingsValuePrimitive<int?>("Clock1BaseTime", "Short Timer Base Time", "The base time of the first clock in total seconds", null));
            AddSetting(new SettingsValuePrimitive<int?>("Clock1Increment", "Short Timer Increment", "The increment of the first clock in seconds", null));
            AddSetting(new SettingsValuePrimitive<int?>("Clock2BaseTime", "Medium Timer Base Time", "The base time of the second clock in total seconds", null));
            AddSetting(new SettingsValuePrimitive<int?>("Clock2Increment", "Medium Timer Increment", "The increment of the second clock in seconds", null));
            AddSetting(new SettingsValuePrimitive<int?>("Clock3BaseTime", "Long Timer Base Time", "The base time of the third clock in total seconds", null));
            AddSetting(new SettingsValuePrimitive<int?>("Clock3Increment", "Long Timer Increment", "The increment of the third clock in seconds", null));
            AddSetting(new SettingsValuePrimitive<int?>("PoolDividerLength", "Pool Divider Length", "The length of the pool divider in meters", null));

        }


        public static SettingsHandler LoadOrCreateNew()
        {
            var sh = new SettingsHandler();

            if (File.Exists(settingsFilePath))
            {
                var str = File.ReadAllText(settingsFilePath);
                var jObj = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(str);
                foreach (var (key, value) in jObj)
                {
                    var s = sh.GetSetting(key);
                    s.SetValueDirect(value);
                }
                Console.WriteLine("Settings loaded.");
            }

            return sh;
        }

        public void Save()
        {
            var str = JsonConvert.SerializeObject(this.settingsStore.ToDictionary(x => x.Key, x => x.Value.GetValue()), Formatting.Indented);
            File.WriteAllText(settingsFilePath, str);
            Console.WriteLine("Settings saved.");
        }

        public void Tick()
        {
            var di = Program.instance.di;
            if (di == null || !di.IsValid())
                return;

            try
            {
                var ttanim = (this.settingsStore["ForceTimetravelAnimationValue"] as SettingsValueWhitelisted<string>).Value;
                if (ttanim != "ignore")
                    di.MemLocTimeTravelAnimationEnabled.SetValue(ttanim == "always_on" ? 1 : 0);

                ApplyClockSetting("Clock1BaseTime", di.MemLocClock1BaseTime);
                ApplyClockSetting("Clock1Increment", di.MemLocClock1Increment);
                ApplyClockSetting("Clock2BaseTime", di.MemLocClock2BaseTime);
                ApplyClockSetting("Clock2Increment", di.MemLocClock2Increment);
                ApplyClockSetting("Clock3BaseTime", di.MemLocClock3BaseTime);
                ApplyClockSetting("Clock3Increment", di.MemLocClock3Increment);
                ApplyClockSetting("PoolDividerLength", di.MemLocPoolDividers);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while ticking settings handler:\n{ex.ToSanitizedString()}");
            }

            void ApplyClockSetting(string settingName, MemoryLocationRestorable<int> memLocation)
            {
                var settingValue = (this.settingsStore[settingName] as SettingsValuePrimitive<int?>).Value;

                if (settingValue.HasValue)
                    memLocation.SetValue(settingValue.Value);
                else
                    memLocation.RestoreOriginal();
            }
        }
    }
}
