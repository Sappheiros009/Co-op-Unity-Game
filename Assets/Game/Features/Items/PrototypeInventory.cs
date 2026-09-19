using System.Collections.Generic;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    public enum PrototypeItemKind { Medkit, Key, Lure, Rope }

    // Trial capacity and items. No shop, crafting, or random paid rewards.
    public sealed class PrototypeInventory
    {
        private readonly List<PrototypeItemKind> _items = new List<PrototypeItemKind>();
        public int Capacity { get; }
        public IReadOnlyList<PrototypeItemKind> Items => _items;
        public PrototypeInventory(int capacity) { Capacity = Mathf.Clamp(capacity, 2, 3); }
        public bool Has(PrototypeItemKind kind) => _items.Contains(kind);
        public bool Add(PrototypeItemKind kind)
        {
            if (_items.Count >= Capacity) return false;
            _items.Add(kind);
            return true;
        }
        public bool Consume(PrototypeItemKind kind) => _items.Remove(kind);
        public bool TransferTo(PrototypeInventory other, PrototypeItemKind kind)
        {
            if (other == null || other == this || !Has(kind) || !other.Add(kind)) return false;
            return Consume(kind);
        }
        public static string Label(PrototypeItemKind kind) => kind switch
        {
            PrototypeItemKind.Medkit => "회복팩", PrototypeItemKind.Key => "장치 열쇠",
            PrototypeItemKind.Lure => "유인구", _ => "이동 로프"
        };
    }
}
