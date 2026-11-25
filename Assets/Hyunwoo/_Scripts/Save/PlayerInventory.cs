using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hyunwoo.Save
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        [Serializable]
        private sealed class Slot
        {
            public string id;
            [Min(0)] public int quantity;
            public GameObject icon;
        }

        [SerializeField] private List<Slot> slots = new();

        private void Start() => RefreshIcons();

        public int Count(string id) => slots.Find(slot => slot.id == id)?.quantity ?? 0;

        public void Add(string id, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(id) || amount <= 0) return;
            Slot slot = slots.Find(entry => entry.id == id);
            if (slot == null)
            {
                slot = new Slot { id = id };
                slots.Add(slot);
            }
            slot.quantity += amount;
            if (slot.icon != null) slot.icon.SetActive(true);
        }

        public bool TryConsume(string id)
        {
            Slot slot = slots.Find(entry => entry.id == id);
            if (slot == null || slot.quantity < 1) return false;
            slot.quantity--;
            if (slot.icon != null) slot.icon.SetActive(slot.quantity > 0);
            return true;
        }

        public List<ItemQuantity> Capture()
        {
            var result = new List<ItemQuantity>();
            foreach (Slot slot in slots)
                if (slot.quantity > 0) result.Add(new ItemQuantity { id = slot.id, quantity = slot.quantity });
            return result;
        }

        public void Restore(List<ItemQuantity> items)
        {
            foreach (Slot slot in slots) slot.quantity = 0;
            if (items != null)
                foreach (var item in items) Add(item.id, item.quantity);
            RefreshIcons();
        }

        private void RefreshIcons()
        {
            foreach (Slot slot in slots)
                if (slot.icon != null) slot.icon.SetActive(slot.quantity > 0);
        }
    }
}
