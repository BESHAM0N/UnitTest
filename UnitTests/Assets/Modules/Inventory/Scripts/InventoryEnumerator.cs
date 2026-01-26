using System.Collections;
using System.Collections.Generic;

namespace Modules.Inventories
{
    public struct InventoryEnumerator : IEnumerator<Item>
    {
        private readonly Inventory _inventory;
        private int _index;

        public InventoryEnumerator(Inventory inventory)
        {
            _inventory = inventory;
            _index = -1;
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _inventory.Count;
        }

        public void Reset() => _index = -1;

        public Item Current => _inventory.Items[_index];

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }
}