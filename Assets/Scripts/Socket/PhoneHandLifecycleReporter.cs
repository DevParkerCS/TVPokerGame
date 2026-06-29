using System.Linq;
using UnityEngine;

public class PhoneHandLifecycleReporter : MonoBehaviour
{
    private SocketManager socketManager;
    private GameManager gameManager;
    private float nextCheckAt;
    private bool hadActiveHand;
    private bool sentStartedForCurrentHand;
    private int handId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateReporter()
    {
        GameObject existing = GameObject.Find(nameof(PhoneHandLifecycleReporter));
        if (existing != null)
            return;

        GameObject reporter = new GameObject(nameof(PhoneHandLifecycleReporter));
        reporter.AddComponent<PhoneHandLifecycleReporter>();
        DontDestroyOnLoad(reporter);
    }

    private void Update()
    {
        if (Time.time < nextCheckAt)
            return;

        nextCheckAt = Time.time + 0.1f;
        EnsureReferences();
        ReportLifecycleIfChanged();
    }

    private void EnsureReferences()
    {
        if (socketManager == null)
            socketManager = FindObjectOfType<SocketManager>();

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
    }

    private void ReportLifecycleIfChanged()
    {
        if (socketManager == null || gameManager == null || gameManager.Players == null || gameManager.Players.Count == 0)
            return;

        bool hasAnyCards = gameManager.Players.Any(pm => pm != null && pm.Player != null && pm.Player.Cards.Count > 0);
        bool allActivePlayersHaveHoleCards = gameManager.Players
            .Where(pm => pm != null && pm.Player != null && !pm.Player.HasFolded)
            .All(pm => pm.Player.Cards.Count == 2);

        if (hadActiveHand && !hasAnyCards)
        {
            handId++;
            sentStartedForCurrentHand = false;
            socketManager.SendHandLifecycleToPhones("hand-reset", handId, "Waiting for new hand");
        }

        if (!sentStartedForCurrentHand && allActivePlayersHaveHoleCards)
        {
            if (!hadActiveHand)
                handId++;

            sentStartedForCurrentHand = true;
            socketManager.SendHandLifecycleToPhones("hand-started", handId, "New hand started");
        }

        hadActiveHand = hasAnyCards;
    }
}
