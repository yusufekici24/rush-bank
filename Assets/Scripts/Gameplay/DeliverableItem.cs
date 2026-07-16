using UnityEngine;

namespace RushBank.Gameplay
{
    public class DeliverableItem : MonoBehaviour
    {
        [SerializeField] private string itemId = "item_id";
        [SerializeField] private Color itemColor = Color.white;

        public string ItemId => itemId;
        public Color ItemColor => itemColor;

        public void Configure(string newItemId, Color newItemColor)
        {
            itemId = newItemId;
            itemColor = newItemColor;
        }
    }
}
