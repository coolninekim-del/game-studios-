using UnityEngine;
using UnityEngine.SceneManagement;

namespace KnightOrder.UI.Stable
{
    /// <summary>
    /// Opens the Stable scene at startup when the player is launched with <c>--scene Stable</c> (or with
    /// <c>--evidence-story 005</c>), so the stable can be played before Story 007 links it into the run.
    /// Implements: Story 005.
    ///
    /// Usage:
    /// <code>
    /// KnightOrder.exe --scene Stable
    /// </code>
    /// </summary>
    public static class StableSceneLauncher
    {
        /// <summary>Name of the stable scene in the build list.</summary>
        public const string SCENE_NAME = "Stable";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if ((Arg("--scene") == SCENE_NAME || Arg("--evidence-story") == "005")
                && SceneManager.GetActiveScene().name != SCENE_NAME)
            {
                SceneManager.LoadScene(SCENE_NAME);
            }
        }

        /// <summary>The value after a command-line flag.</summary>
        /// <param name="name">Flag, for example "--scene".</param>
        /// <returns>The next argument, or null if the flag is absent.</returns>
        public static string Arg(string name)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
