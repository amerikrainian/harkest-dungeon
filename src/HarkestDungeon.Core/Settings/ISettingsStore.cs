namespace DD2A11y.Core.Settings {
    /// <summary>
    /// The persistence seam behind the mod's settings: load a value (returning the default when
    /// nothing is stored) and save one. An interface so Core stays free of any BepInEx/file
    /// reference - the plugin implements it over its BepInEx ConfigFile, the tests over an
    /// in-memory fake.
    /// </summary>
    public interface ISettingsStore {
        string GetString(string key, string defaultValue);
        void SetString(string key, string value);
    }
}
