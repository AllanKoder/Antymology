using UnityEngine;
using UnityEngine.UI;

namespace Antymology.UI
{
    /// <summary>
    /// UI component to display simulation statistics.
    /// </summary>
    public class SimulationUI : MonoBehaviour
    {
        [Header("UI References")]
        public Text nestCountText;
        public Text generationText;
        public Text antCountText;

        private void Update()
        {
            UpdateUI();
        }

        /// <summary>
        /// Update all UI text elements.
        /// </summary>
        private void UpdateUI()
        {
            if (Agents.AntManager.Instance == null) return;

            if (nestCountText != null)
            {
                nestCountText.text = $"Nests: {Agents.AntManager.Instance.TotalNestsProduced}";
            }

            if (generationText != null)
            {
                generationText.text = $"Generation: {Agents.AntManager.Instance.CurrentGeneration}";
            }

            if (antCountText != null)
            {
                antCountText.text = $"Ants Alive: {Agents.AntManager.Instance.AliveAntCount}";
            }
        }
    }
}
