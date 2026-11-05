using UnityEngine;
using UnityEngine.SceneManagement;

namespace KnightOrder.Duel.Presentation
{
    /// <summary>
    /// Opens the Duel scene at startup when the player is launched with <c>--scene Duel</c> (or with
    /// <c>--evidence-story 004</c>), so the duel can be played before Story 007 links it into the run.
    /// Implements: Story 004.
    ///
    /// Usage:
    /// <code>
    /// KnightOrder.exe --scene Duel
    /// </code>
    /// </summary>
    public static class DuelSceneLauncher
    {
        /// <summary>Name of the duel scene in the build list.</summary>
        public const string SCENE_NAME = "Duel";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if ((Arg("--scene") == SCENE_NAME || Arg("--evidence-story") == "004")
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
