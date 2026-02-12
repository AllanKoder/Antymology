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
        // Displays whether non-evolving mode is active
        public TMP_Text nonEvolveText;
        // Displays whether fast-forward is active
        public TMP_Text isFastForwardingText;
        private GameObject mainCameraObject;
        // Track currently highlighted ant in the scene
        private Ant highlightedAnt = null;

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
                string evolvedText = "Best Genome (evolved): N/A";
                if (EvolutionManager.Instance != null)
                {
                    var bg = EvolutionManager.Instance.BestGenome;
                    evolvedText = "Best Genome (evolved):\n" + bg.ToString();
                }

                bestGenomeText.text = evolvedText;
            }

            // Update non-evolving mode text
            if (nonEvolveText != null)
            {
                bool nonEvolving = false;
                if (EvolutionManager.Instance != null)
                {
                    nonEvolving = !EvolutionManager.Instance.IsRunning;
                }
                nonEvolveText.text = nonEvolving ? "Non-Evolving Mode: ON" : "Non-Evolving Mode: OFF";
            }

            // Update fast-forwarding text
            if (isFastForwardingText != null)
            {
                bool fast = Antymology.Simulation.SimulationSettings.FastMode;
                isFastForwardingText.text = fast ? "Fast Forwarding: ON" : "Fast Forwarding: OFF";
            }

            // Toggle fast mode with F key: disables main camera and speeds up simulation
            if (Input.GetKeyDown(KeyCode.F))
            {
                Antymology.Simulation.SimulationSettings.FastMode = !Antymology.Simulation.SimulationSettings.FastMode;
                // Keep camera enabled during fast mode — do not disable camera or its GameObject
                if (mainCameraObject != null)
                {
                    var camComp = mainCameraObject.GetComponent<Camera>();
                    if (camComp != null)
                        camComp.enabled = true;
                    else
                        mainCameraObject.SetActive(true);
                }
                else
                {
                    var cam = Camera.main;
                    if (cam != null)
                    {
                        mainCameraObject = cam.gameObject;
                        var camComp = mainCameraObject.GetComponent<Camera>();
                        if (camComp != null)
                            camComp.enabled = true;
                        else
                            mainCameraObject.SetActive(true);
                    }
                }
            }

            // Toggle non-evolving mode with N key: stop/start evolution and spawn best genome when stopping
            if (Input.GetKeyDown(KeyCode.N))
            {
                if (EvolutionManager.Instance != null)
                {
                    if (EvolutionManager.Instance.IsRunning)
                    {
                        EvolutionManager.Instance.StopEvolutionIfRunning();
                        var best = EvolutionManager.Instance.BestGenome;
                        if (Antymology.Agents.AntManager.Instance != null)
                        {
                            Antymology.Agents.AntManager.Instance.ClearAllAnts();
                            Antymology.Agents.AntManager.Instance.SpawnColonyWithGenome(best);
                        }
                    }
                    else
                    {
                        EvolutionManager.Instance.StartEvolution();
                    }
                }
            }
        }
    }
}
