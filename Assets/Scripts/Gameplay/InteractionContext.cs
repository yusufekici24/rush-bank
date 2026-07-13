using UnityEngine;

namespace RushBank.Gameplay
{
    public readonly struct InteractionContext
    {
        public InteractionContext(GameObject actor, ScenarioRunner scenarioRunner)
        {
            Actor = actor;
            ScenarioRunner = scenarioRunner;
        }

        public GameObject Actor { get; }
        public ScenarioRunner ScenarioRunner { get; }
    }
}
