using UnityEngine;
using TMPro;
using Antymology.Agents;

namespace Antymology.UI
{
    /// <summary>
    /// Simple UI manager that updates TextMeshPro text elements with runtime stats.
    /// Assign TMP Text references in the inspector.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        public TMP_Text nestsText;
        public TMP_Text generationText;
        public TMP_Text aliveText;
        public TMP_Text bestGenomeText;
        private GameObject mainCameraObject;

        void Awake()
        {
            mainCameraObject = (Camera.main != null) ? Camera.main.gameObject : null;
        }

        void Update()
        {
            if (AntManager.Instance != null)
            {
                if (nestsText != null) nestsText.text = $"Nests: {AntManager.Instance.TotalNestsProduced}";
                if (generationText != null) generationText.text = $"Generation: {AntManager.Instance.CurrentGeneration}";
                if (aliveText != null) aliveText.text = $"Alive ants: {AntManager.Instance.AliveAntCount}";
            }


            if (bestGenomeText != null)
            {
                if (EvolutionManager.Instance != null)
                {
                    var bg = EvolutionManager.Instance.BestGenome;
                    bestGenomeText.text = "Best Genome:\n" + bg.ToString();
                }
                else
                {
                    bestGenomeText.text = "Best Genome: N/A";
                }
            }

            // Toggle fast mode with F key: disables main camera and speeds up simulation
            if (Input.GetKeyDown(KeyCode.F))
            {
                Antymology.Simulation.SimulationSettings.FastMode = !Antymology.Simulation.SimulationSettings.FastMode;
                // Use stored camera object so toggling back works even if Camera.main becomes null when disabled
                if (mainCameraObject != null)
                {
                    mainCameraObject.SetActive(!Antymology.Simulation.SimulationSettings.FastMode);
                }
                else
                {
                    var cam = Camera.main;
                    if (cam != null)
                    {
                        mainCameraObject = cam.gameObject;
                        mainCameraObject.SetActive(!Antymology.Simulation.SimulationSettings.FastMode);
                    }
                }
            }
        }
    }
}
