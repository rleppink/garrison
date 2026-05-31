namespace Garrison.Shared.Config
{
    // Shared seam for runtime-spawned behaviours that need the lobby/config values
    // injected without the spawner knowing which slice owns the component.
    public interface IConfigConsumer
    {
        void Configure(IConfig source);
    }
}
