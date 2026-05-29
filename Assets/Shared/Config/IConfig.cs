namespace Garrison.Shared.Config
{
    public interface IConfig
    {
        event System.Action Changed;

        int GetInt(ConfigKey key, int fallback = 0);
        float GetFloat(ConfigKey key, float fallback = 0f);
        bool GetBool(ConfigKey key, bool fallback = false);
    }
}
