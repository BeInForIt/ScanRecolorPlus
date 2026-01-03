using UnityEngine.SceneManagement;

namespace hoppinhauler.ScanRecolorRework
{
    internal static class SceneHooks
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed) return;
            _installed = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public static void Uninstall()
        {
            if (!_installed) return;
            _installed = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            
            if (ModConfig.ResetOnSceneLoad.Value)
                HUDManagerPatch.RequestApply();
            else
                HUDManagerPatch.RequestApply(); 
        }

    }
}
