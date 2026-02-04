using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Modules.Inventories
{
    public class Inventory : IEnumerable<Item>
    {
        public event Action<Item, Vector2Int> OnAdded;
        public event Action<Item, Vector2Int> OnRemoved;
        public event Action<Item, Vector2Int> OnMoved;
        public event Action OnCleared;

        public int Width { get; private set; }
        public int Height { get; private set; }

        public int Count => _count;

        private readonly Item[,] _inventory;
        
        private Slot[] _slots;
        private int[] _buckets;

        private int _capacity;
        private int _count;
        private int _lastIndex;
        private int _freeList;

        private static SortEntry[] _sortBuffer;
        private static readonly IComparer<SortEntry> _sortComparer = new SortEntryComparer();

        private const int START_INVENTORY_SIZE = 4;
        private const int UNDEFINED_INDEX = -1;

        public Inventory(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("width or height can't be less 0");

            Width = width;
            Height = height;

            _inventory = new Item[Width, Height];
            InitTable(START_INVENTORY_SIZE);
        }

        public Inventory(
            int width,
            int height,
            params KeyValuePair<Item, Vector2Int>[] items
        ) : this(width, height)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            AddItemsInternal(items);
        }

        public Inventory(
            int width,
            int height,
            params Item[] items
        ) : this(width, height)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            AddItemsInternal(items);
        }

        public Inventory(
            int width,
            int height,
            IEnumerable<KeyValuePair<Item, Vector2Int>> items
        ) : this(width, height)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            AddItemsInternal(items);
        }

        public Inventory(
            int width,
            int height,
            IEnumerable<Item> items
        ) : this(width, height)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            AddItemsInternal(items);
        }

        /// <summary>
        /// Creates new inventory 
        /// </summary>
        public Inventory(Inventory inventory)
        {
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));

            Width = inventory.Width;
            Height = inventory.Height;

            _inventory = new Item[Width, Height];
            InitTable(Math.Max(START_INVENTORY_SIZE, inventory.Count));
            CopyFrom(inventory);
        }

        /// <summary>
        /// Checks for adding an item on a specified position
        /// </summary>
        public bool CanAddItem(Item item, Vector2Int position)
        {
            return IsValidItem(item) &&
                   !Contains(item) &&
                   IsPositionWithinBounds(position, item.Size) &&
                   IsFreeSpaceBySize(position.x, position.y, item.Size.x, item.Size.y);
        }

        public bool CanAddItem(Item item, int startX, int startY) => CanAddItem(item, new Vector2Int(startX, startY));

        /// <summary>
        /// Adds an item on a specified position
        /// </summary>
        public bool AddItem(Item item, Vector2Int position)
        {
            if (!CanAddItem(item, position))
                return false;

            FillInventoryGrid(item, position);
            AddRecord(item, position);

            OnAdded?.Invoke(item, position);
            return true;
        }

        public bool AddItem(Item item, int startX, int startY) => AddItem(item, new Vector2Int(startX, startY));

        /// <summary>
        /// Checks for adding an item on a free position
        /// </summary>
        public bool CanAddItem(Item item)
        {
            return item != null && !Contains(item) && (item.Size.x <= 0 || item.Size.y <= 0
                ? throw new ArgumentException("item size must be greater 0")
                : FindFreePosition(item, out _));
        }

        /// <summary>
        /// Adds an item on a free position
        /// </summary>
        public bool AddItem(Item item)
        {
            if (!IsValidItem(item) || Contains(item) || !FindFreePosition(item.Size, out var position))
                return false;

            FillInventoryGrid(item, position);
            AddRecord(item, position);

            OnAdded?.Invoke(item, position);
            return true;
        }

        /// <summary>
        /// Returns a free position for a specified item
        /// </summary>
        public bool FindFreePosition(Item item, out Vector2Int position)
        {
            return item == null
                ? throw new ArgumentException("item can't be null", nameof(item))
                : FindFreePosition(item.Size, out position);
        }

        public bool FindFreePosition(Vector2Int size, out Vector2Int position)
        {
            return size.x <= 0 || size.y <= 0
                ? throw new ArgumentException("size must be greater 0", nameof(size))
                : FindFreePosition(size.x, size.y, out position);
        }

        public bool FindFreePosition(int sizeX, int sizeY, out Vector2Int position)
        {
            if (sizeX <= 0 || sizeY <= 0)
                throw new ArgumentException("sizeX and sizeY must be greater than zero");

            for (int y = 0; y <= Height - sizeY; y++)
            {
                for (int x = 0; x <= Width - sizeX; x++)
                {
                    var endX = x + sizeX - 1;
                    var endY = y + sizeY - 1;

                    if (!IsFreeSpace(x, y, endX, endY))
                        continue;

                    position = new Vector2Int(x, y);
                    return true;
                }
            }

            position = default;
            return false;
        }

        private bool IsFreeSpace(int startX, int startY, int endX, int endY)
        {
            if (startX < 0 || startY < 0 || endX < 0 || endY < 0)
                throw new ArgumentException("coordinates must be non-negative");

            if (startX > endX || startY > endY)
                throw new ArgumentException("start coordinates must be less than or equal end coordinates");

            if (endX >= Width || endY >= Height)
                throw new ArgumentException("area exceeds inventory bounds");

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    if (_inventory[x, y] != null)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the specified element exists
        /// </summary>
        public bool Contains(Item item) => IndexOf(item) >= 0;

        /// <summary>
        /// Checks if the specified position is occupied
        /// </summary>
        public bool IsOccupied(Vector2Int position) => IsOccupied(position.x, position.y);

        public bool IsOccupied(int x, int y)
        {
            return x < 0 || x >= Width || y < 0 || y >= Height
                ? throw new IndexOutOfRangeException("position outside of inventory")
                : _inventory[x, y] != null;
        }

        /// <summary>
        /// Checks if the specified position is free
        /// </summary>
        public bool IsFree(Vector2Int position) => !IsOccupied(position);

        public bool IsFree(int x, int y) => !IsOccupied(x, y);

        /// <summary>
        /// Removes specified item
        /// </summary>
        public bool RemoveItem(Item item) => RemoveItemInternal(item, out _);

        public bool RemoveItem(Item item, out Vector2Int position)
        {
            var removeItem = RemoveItemInternal(item, out position);
            if (removeItem)
                OnRemoved?.Invoke(item, position);

            return removeItem;
        }

        private bool RemoveItemInternal(in Item item, out Vector2Int position)
        {
            position = default;

            if (item == null || _count == 0)
                return false;

            int hash = GetItemHash(item);
            int bucket = hash % _capacity;

            int index = _buckets[bucket];
            int prev = UNDEFINED_INDEX;

            while (index >= 0)
            {
                ref Slot slot = ref _slots[index];

                if (slot.exists && ReferenceEquals(slot.item, item))
                {
                    if (prev == UNDEFINED_INDEX)
                        _buckets[bucket] = slot.next;
                    else
                        _slots[prev].next = slot.next;

                    position = slot.start;
                
                    ClearInventoryGridCells(item, position);
                
                    slot.exists = false;
                    slot.item = null;
                    slot.start = default;

                    slot.next = _freeList;
                    _freeList = index;

                    _count--;

                    if (_count == 0)
                    {
                        Array.Fill(_buckets, UNDEFINED_INDEX);
                        Array.Clear(_slots, 0, _lastIndex);
                        _lastIndex = 0;
                        _freeList = UNDEFINED_INDEX;
                    }

                    return true;
                }

                prev = index;
                index = slot.next;
            }

            return false;
        }

        /// <summary>
        /// Returns an item at specified position 
        /// </summary>
        public Item GetItem(Vector2Int position) => GetItem(position.x, position.y);

        public Item GetItem(int x, int y)
        {
            return x < 0 || x >= Width || y < 0 || y >= Height
                ? throw new IndexOutOfRangeException("position outside of inventory")
                : _inventory[x, y];
        }

        public bool TryGetItem(Vector2Int position, out Item item) => TryGetItem(position.x, position.y, out item);

        public bool TryGetItem(int x, int y, out Item item)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                item = null;
                return false;
            }

            item = _inventory[x, y];
            return item != null;
        }

        /// <summary>
        /// Returns positions of a specified item 
        /// </summary>
        public Vector2Int[] GetPositions(Item item)
        {
            return !TryGetPositions(item, out var positions)
                ? throw (item == null
                    ? new NullReferenceException("item can't be null")
                    : new KeyNotFoundException("item not found in inventory"))
                : positions;
        }

        public bool TryGetPositions(Item item, out Vector2Int[] positions)
        {
            positions = null;

            int index = IndexOf(item);
            if (index < 0)
                return false;

            Vector2Int start = _slots[index].start;
            positions = new Vector2Int[item.CellSize];

            int posIndex = 0;
            for (int i = 0; i < item.Size.x; i++)
            {
                for (int j = 0; j < item.Size.y; j++)
                {
                    positions[posIndex++] = new Vector2Int(start.x + i, start.y + j);
                }
            }

            return true;
        }

        /// <summary>
        /// Clears all items 
        /// </summary>
        public void Clear()
        {
            if (_count <= 0)
                return;

            Array.Clear(_inventory, 0, _inventory.Length);
           
            Array.Fill(_buckets, UNDEFINED_INDEX);
            Array.Clear(_slots, 0, _lastIndex);

            _count = 0;
            _lastIndex = 0;
            _freeList = UNDEFINED_INDEX;

            OnCleared?.Invoke();
        }

        /// <summary>
        /// Returns count of items with a specified name
        /// </summary>
        public int GetItemCount(string name)
        {
            int count = 0;

            for (int i = 0; i < _lastIndex; i++)
            {
                ref readonly Slot slot = ref _slots[i];
                if (!slot.exists || slot.item == null)
                    continue;

                if (slot.item.Name == name)
                    count++;
            }

            return count;
        }

        public bool MoveItem(Item item, Vector2Int position)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), "item can't be null");

            int index = IndexOf(item);
            if (index < 0 || !IsValidItem(item) || !IsPositionWithinBounds(position, item.Size))
                return false;

            Vector2Int oldPosition = _slots[index].start;

            ClearInventoryGridCells(item, oldPosition);
            var canPlace = IsFreeSpaceBySize(position.x, position.y, item.Size.x, item.Size.y);

            if (!canPlace)
            {
                FillInventoryGrid(item, oldPosition);
                return false;
            }

            FillInventoryGrid(item, position);
            _slots[index].start = position;

            OnMoved?.Invoke(item, position);
            return true;
        }

        /// <summary>
        /// Rearranges an inventory space with max free slots 
        /// </summary>
        public void OptimizeSpace()
        {
            if (_count == 0)
                return;

            EnsureSortBuffer(_count);

            int length = 0;
            for (int i = 0; i < _lastIndex; i++)
            {
                if (!_slots[i].exists)
                    continue;

                int order = length;
                _sortBuffer[length] = new SortEntry
                {
                    Item = _slots[i].item,
                    Order = order
                };
                length++;
            }

            Array.Sort(_sortBuffer, 0, length, _sortComparer);

            Array.Clear(_inventory, 0, _inventory.Length);
            Array.Fill(_buckets, UNDEFINED_INDEX);
            Array.Clear(_slots, 0, _lastIndex);
            
            _count = 0;
            _lastIndex = 0;
            _freeList = UNDEFINED_INDEX;

            for (int i = 0; i < length; i++)
            {
                var item = _sortBuffer[i].Item;

                if (!FindFreePosition(item.Size, out var pos))
                    throw new ArgumentException("error optimizing space");

                FillInventoryGrid(item, pos);
                AddRecord(item, pos);
            }
        }

        Enumerator GetEnumerator() => new(this);

        /// <summary>
        /// Iterates by all items 
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<Item> IEnumerable<Item>.GetEnumerator() => new Enumerator(this);

        struct Enumerator : IEnumerator<Item>
        {
            private readonly Inventory _inventory;
            private int _slotIndex;
            private Item _current;

            public Enumerator(Inventory inventory)
            {
                _inventory = inventory;
                _slotIndex = UNDEFINED_INDEX;
                _current = null;
            }

            public bool MoveNext()
            {
                while (++_slotIndex < _inventory._lastIndex)
                {
                    if (_inventory._slots[_slotIndex].exists)
                    {
                        _current = _inventory._slots[_slotIndex].item;
                        return true;
                    }
                }

                _current = null;
                return false;
            }

            public void Reset()
            {
                _slotIndex = UNDEFINED_INDEX;
                _current = null;
            }

            public Item Current => _current;
            object IEnumerator.Current => Current;

            public void Dispose() { }
        }

        /// <summary>
        /// Copies items to a specified matrix
        /// </summary>
        public void CopyTo(Item[,] matrix)
        {
            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix), "matrix can't be empty");

            if (matrix.GetLength(0) != Width || matrix.GetLength(1) != Height)
                throw new ArgumentException("matrix size doesn't match the dimensions");

            Array.Copy(_inventory, matrix, _inventory.Length);
        }

        /// <summary>
        /// Returns an inventory matrix in string format
        /// </summary>
        public override string ToString()
        {
            var stringBuilder = new StringBuilder(Width * Height * 2);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var item = _inventory[x, y];

                    char c =
                        item == null ? '.' :
                        string.IsNullOrEmpty(item.Name) ? '?' :
                        item.Name[0];

                    stringBuilder.Append(c).Append(' ');
                }

                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString();
        }

        #region Helpers

        /// <summary>
        /// Adds items to specific inventory slots
        /// </summary>
        private void AddItemsInternal(IEnumerable<KeyValuePair<Item, Vector2Int>> items)
        {
            foreach (var item in items)
            {
                if (item.Key == null)
                    throw new ArgumentException("item cannot be null");

                if (!CanAddItem(item.Key, item.Value.x, item.Value.y))
                    throw new ArgumentException("invalid item or position");

                AddItem(item.Key, item.Value);
            }
        }

        /// <summary>
        /// Adds items to any free inventory slots 
        /// </summary>
        private void AddItemsInternal(IEnumerable<Item> items)
        {
            foreach (var item in items)
            {
                if (!FindFreePosition(item.Size, out var position))
                    throw new ArgumentException("inventory has no free slots");

                AddItem(item, position);
            }
        }

        /// <summary>
        /// Checks whether an item is valid for placing into the inventory
        /// </summary>
        private bool IsValidItem(Item item)
        {
            if (item == null)
                return false;

            if (item.Size.x <= 0 || item.Size.y <= 0)
                throw new ArgumentException("item size must be greater 0");

            return true;
        }

        /// <summary>
        /// Checks whether the given item position and size fit within inventory bounds
        /// </summary>
        private bool IsPositionWithinBounds(Vector2Int position, Vector2Int size)
        {
            return position.x >= 0 && position.y >= 0 &&
                   position.x + size.x <= Width && position.y + size.y <= Height;
        }

        /// <summary>
        /// Checks whether area in the grid is free based on width/height values
        /// </summary>
        private bool IsFreeSpaceBySize(int startX, int startY, int sizeX, int sizeY)
        {
            var endX = startX + sizeX - 1;
            var endY = startY + sizeY - 1;

            return IsFreeSpace(startX, startY, endX, endY);
        }

        /// <summary>
        /// Returns the index of the specified item in the internal items array
        /// </summary>
        private int IndexOf(Item item)
        {
           return FindSlotIndex(item);
        }

        /// <summary>
        /// Ensures that the sort buffer has enough capacity to store the specified number of elements
        /// </summary>
        private void EnsureSortBuffer(int needed)
        {
            if (_sortBuffer != null && _sortBuffer.Length >= needed)
                return;

            _sortBuffer = new SortEntry[Math.Max(needed, START_INVENTORY_SIZE)];
        }

        /// <summary>
        /// Adds a new item record with its starting position to the internal storage
        /// </summary>
        private void AddRecord(Item item, Vector2Int position)
        {
            if (_freeList < 0 && _lastIndex == _capacity)
                IncreaseCapacity();

            int hash = GetItemHash(item);
            int bucket = hash % _capacity;

            int index;
            if (_freeList >= 0)
            {
                index = _freeList;
                _freeList = _slots[index].next;
            }
            else
            {
                index = _lastIndex;
                _lastIndex++;
            }

            _slots[index] = new Slot
            {
                item = item,
                start = position,
                exists = true,
                next = _buckets[bucket]
            };

            _buckets[bucket] = index;
            _count++;
        }

        /// <summary>
        /// Fills the inventory grid with the given item at the specified position,
        /// occupying all cells covered by the item's size
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FillInventoryGrid(Item item, Vector2Int position)
        {
            for (int x = position.x; x < position.x + item.Size.x; x++)
            {
                for (int y = position.y; y < position.y + item.Size.y; y++)
                {
                    _inventory[x, y] = item;
                }
            }
        }

        /// <summary>
        /// Clears all grid cells previously occupied by the specified item, 
        /// starting from the given position
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearInventoryGridCells(Item item, Vector2Int position)
        {
            for (int x = position.x; x < position.x + item.Size.x; x++)
            {
                for (int y = position.y; y < position.y + item.Size.y; y++)
                {
                    _inventory[x, y] = null;
                }
            }
        }

        /// <summary>
        /// Copies items from another inventory, cloning them and placing in the same positions
        /// </summary>
        private void CopyFrom(Inventory inventory)
        {
            for (int i = 0; i < inventory._lastIndex; i++)
            {
                if (!inventory._slots[i].exists)
                    continue;

                Item item = inventory._slots[i].item;
                Vector2Int pos = inventory._slots[i].start;

                AddItem(item, pos);
            }
        }

        #endregion

        private sealed class SortEntryComparer : IComparer<SortEntry>
        {
            public int Compare(SortEntry a, SortEntry b)
            {
                var area = b.Item.CellSize.CompareTo(a.Item.CellSize);
                if (area != 0)
                    return area;

                var width = b.Item.Size.x.CompareTo(a.Item.Size.x);
                if (width != 0)
                    return width;

                var height = b.Item.Size.y.CompareTo(a.Item.Size.y);
                if (height != 0)
                    return height;

                return a.Order.CompareTo(b.Order);
            }
        }

        private struct SortEntry
        {
            public Item Item;
            public int Order;
        }

        #region Hash-table
        
        private struct Slot
        {
            public Item item;
            public Vector2Int start;
            public bool exists;
            public int next;
        }
        
        private void InitTable(int capacity)
        {
            _capacity = CeilToPrime(Math.Max(START_INVENTORY_SIZE, capacity));

            _slots = new Slot[_capacity];
            _buckets = new int[_capacity];
            Array.Fill(_buckets, UNDEFINED_INDEX);

            _count = 0;
            _lastIndex = 0;
            _freeList = UNDEFINED_INDEX;
        }
        
        private int FindSlotIndex(Item item)
        {
            if (item == null || _count == 0)
                return UNDEFINED_INDEX;

            int hash = GetItemHash(item);
            int bucket = hash % _capacity;

            int index = _buckets[bucket];
            while (index >= 0)
            {
                ref readonly Slot slot = ref _slots[index];
                if (slot.exists && ReferenceEquals(slot.item, item))
                    return index;

                index = slot.next;
            }

            return UNDEFINED_INDEX;
        }
        
        private void IncreaseCapacity()
        {
            if (_capacity > int.MaxValue / 2)
                throw new OutOfMemoryException("inventory capacity already reached int.MaxValue");
            
            int newCapacity = CeilToPrime(_capacity * 2);

            Array.Resize(ref _slots, newCapacity);
            Array.Resize(ref _buckets, newCapacity);

            _capacity = newCapacity;
            Array.Fill(_buckets, UNDEFINED_INDEX);
           
            for (int i = 0; i < _lastIndex; i++)
            {
                if (!_slots[i].exists)
                    continue;

                int hash = GetItemHash(_slots[i].item);
                int bucket = hash % _capacity;

                _slots[i].next = _buckets[bucket];
                _buckets[bucket] = i;
            }
        }
        
        private static int CeilToPrime(int value)
        {
            if (value <= 2) 
                return 2;
            
            if ((value & 1) == 0) 
                value++;

            while (!IsPrime(value))
                value += 2;

            return value;
        }
        
        private static bool IsPrime(int value)
        {
            switch (value)
            {
                case <= 1:
                    return false;
                case 2:
                    return true;
            }

            if ((value & 1) == 0) 
                return false;

            int limit = (int)Math.Sqrt(value);
            
            for (int i = 3; i <= limit; i += 2)
            {
                if (value % i == 0)
                    return false;
            }

            return true;
        }
        
        private static int GetItemHash(Item item)
        {
            return RuntimeHelpers.GetHashCode(item) & 0x7FFFFFFF;
        }

        #endregion
    }
}