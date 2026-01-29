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
        public Item[] Items => _items;
        public int Count => _count;

        private readonly Item[,] _inventory;

        private Item[] _items;
        private Vector2Int[] _startPositions;

        private int _count;

        private static SortEntry[] _sortBuffer;
        private static readonly IComparer<SortEntry> _sortComparer = new SortEntryComparer();

        private const int START_INVENTORY_SIZE = 4;

        public Inventory(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("width or height can't be less 0");

            Width = width;
            Height = height;

            _inventory = new Item[Width, Height];

            _items = new Item[START_INVENTORY_SIZE];
            _startPositions = new Vector2Int[START_INVENTORY_SIZE];
            _count = 0;
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

            var cap = Math.Max(START_INVENTORY_SIZE, inventory.Count);
            _items = new Item[cap];
            _startPositions = new Vector2Int[cap];
            _count = 0;

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

            var index = IndexOf(item);
            if (index < 0)
                return false;

            position = _startPositions[index];
            ClearInventoryGridCells(item, position);

            var last = _count - 1;
            if (index != last)
            {
                _items[index] = _items[last];
                _startPositions[index] = _startPositions[last];
            }

            _items[last] = null;
            _startPositions[last] = default;
            _count--;

            return true;
        }

        /// <summary>
        /// Returns an item at specified position 
        /// </summary>
        public Item GetItem(Vector2Int position)
        {
            return GetItem(position.x, position.y);
        }

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

            var index = IndexOf(item);
            if (index < 0)
                return false;

            var start = _startPositions[index];
            positions = new Vector2Int[item.CellSize];

            var i = 0;
            for (int x = 0; x < item.Size.x; x++)
            {
                for (int y = 0; y < item.Size.y; y++)
                {
                    positions[i++] = new Vector2Int(start.x + x, start.y + y);
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
            Array.Clear(_items, 0, _count);
            Array.Clear(_startPositions, 0, _count);
            _count = 0;

            OnCleared?.Invoke();
        }

        /// <summary>
        /// Returns count of items with a specified name
        /// </summary>
        public int GetItemCount(string name)
        {
            var count = 0;

            for (int i = 0; i < _count; i++)
            {
                if (_items[i].Name == name)
                    count++;
            }

            return count;
        }

        public bool MoveItem(Item item, Vector2Int position)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), "item can't be null");

            var index = IndexOf(item);
            if (index < 0 || !IsValidItem(item) || !IsPositionWithinBounds(position, item.Size))
                return false;

            var oldPosition = _startPositions[index];

            ClearInventoryGridCells(item, oldPosition);
            var canPlace = IsFreeSpaceBySize(position.x, position.y, item.Size.x, item.Size.y);

            if (!canPlace)
            {
                FillInventoryGrid(item, oldPosition);
                return false;
            }

            FillInventoryGrid(item, position);
            _startPositions[index] = position;
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

            for (int i = 0; i < _count; i++)
            {
                _sortBuffer[i] = new SortEntry { Item = _items[i], Order = i };
            }

            Array.Sort(_sortBuffer, 0, _count, _sortComparer);

            Array.Clear(_inventory, 0, _inventory.Length);
            Array.Clear(_items, 0, _count);
            Array.Clear(_startPositions, 0, _count);

            var oldCount = _count;
            _count = 0;

            for (int i = 0; i < oldCount; i++)
            {
                var item = _sortBuffer[i].Item;

                if (!FindFreePosition(item.Size, out var pos))
                    throw new ArgumentException("error optimizing space");

                FillInventoryGrid(item, pos);
                AddRecord(item, pos);
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Iterates by all items 
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<Item> IEnumerable<Item>.GetEnumerator()
        {
            return new Enumerator(this);
        }
        
        public struct Enumerator : IEnumerator<Item>
        {
            private readonly Inventory _inventory;
            private int _index;

            public Enumerator(Inventory inventory)
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
            if (item == null)
                return -1;

            for (int i = 0; i < _count; i++)
            {
                if (ReferenceEquals(_items[i], item))
                    return i;
            }

            return -1;
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
            EnsureCapacity(_count + 1);
            _items[_count] = item;
            _startPositions[_count] = position;
            _count++;
        }

        /// <summary>
        /// Ensures that internal item and position arrays have sufficient capacity
        /// </summary>
        private void EnsureCapacity(int needed)
        {
            if (_items == null)
            {
                _items = new Item[Math.Max(START_INVENTORY_SIZE, needed)];
                _startPositions = new Vector2Int[_items.Length];
                return;
            }

            if (needed <= _items.Length)
                return;

            //TODO: Проверить, что int.MaxValue
            var newCap = _items.Length * 2;
            if (newCap < needed)
                newCap = needed;

            Array.Resize(ref _items, newCap);
            Array.Resize(ref _startPositions, newCap);
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
            for (int i = 0; i < inventory._count; i++)
            {
                var clonedItem = inventory._items[i].Clone(); //убрать клонирование
                var position = inventory._startPositions[i];
                AddItem(clonedItem, position);
            }
        }

        #endregion
    }
}