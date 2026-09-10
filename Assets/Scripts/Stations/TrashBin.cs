using KitchenGame.Core;
using KitchenGame.Player;
using KitchenGame.Stations;
using UnityEngine;

namespace KitchenGame.Stations
{
    /// <summary>
    /// Trash station. Destroys whatever ingredient the player is holding.
    /// </summary>
    public class TrashBin : MonoBehaviour, IInteractable
    {
        public bool CanInteract(PlayerController player)
        {
            return GameManager.Instance.IsPlaying && player.HasItem;
        }

        public void Interact(PlayerController player)
        {
            if (!CanInteract(player)) return;
            player.DiscardItem();
        }

        public string GetInteractHint(PlayerController player)
        {
            return player.HasItem ? "Press E - Trash item" : "Trash (empty hands)";
        }
    }
}
