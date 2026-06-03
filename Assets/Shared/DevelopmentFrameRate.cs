using UnityEngine;

namespace Garrison.Shared
{
    public static class DevelopmentFrameRate
    {
        private const int TargetFrameRate = 30;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
#endif
        }
    }
}
