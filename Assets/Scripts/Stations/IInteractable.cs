using KitchenGame.Player;

namespace KitchenGame.Stations
{
    /// <summary>
    /// Every interactable station in the kitchen implements this interface.
    /// The player calls Interact() when pressing the interact key while in range.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Returns true if the player is allowed to interact right now.</summary>
        bool CanInteract(PlayerController player);

        /// <summary>Execute the interaction logic.</summary>
        void Interact(PlayerController player);

        /// <summary>Optional: display a hint label (e.g., "Press E to chop").</summary>
        string GetInteractHint(PlayerController player);
    }
}
