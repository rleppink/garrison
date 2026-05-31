using Garrison.Shared.Net;
using UnityEngine;

namespace Garrison.Shared.Config
{
    // Persistent scene service that hands the inspector-wired config source to
    // spawned client-side behaviours without those behaviours depending on a concrete
    // slice.
    //
    // Caveat: keep this as a narrow runtime-spawn bridge, not a general service
    // locator. Author-time references still belong in prefabs/scenes, and
    // server-authoritative systems should be configured by their owning server path.
    public sealed class ConfigConsumerBinder : SpawnedIdentityBinder<IConfigConsumer>
    {
        [SerializeField] private MonoBehaviour configSource;

        private IConfig Config => configSource as IConfig;

        protected override void ValidateConfiguration()
        {
            if (configSource == null)
                Debug.LogError("ConfigConsumerBinder has no config source wired; spawned config consumers will use fallbacks.");
            else if (Config == null)
                Debug.LogError("ConfigConsumerBinder configSource must implement IConfig.");
        }

        protected override void Bind(IConfigConsumer consumer, bool bound)
        {
            consumer.Configure(bound ? Config : null);
        }
    }
}
