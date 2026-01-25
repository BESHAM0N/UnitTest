using System;
using System.Collections;
using System.Collections.Generic;
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
        public int Count => _inventoryItems.Count;

        private Item[,] _inventory;
        private Dictionary<Item, Vector2Int> _inventoryItems;

        public Inventory(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("width or height can't be less 0");

            Width = width;
            Height = height;

            _inventory = new Item[Width, Height];
            _inventoryItems = new Dictionary<Item, Vector2Int>();
        }

        public Inventory(
            int width,
            int height,
            params KeyValuePair<Item, Vector2Int>[] items
        ) : this(width, height)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            AddItemsWithPosition(items);
        }

        public Inventory(
            int width,
            int height,
            params Item[] items
        ) : this(width, height)
        {
            if (items == null)
                throw new ArgumentNullException( nameof(items));

            AddItemsWithoutPosition(items);
        }

        public Inventory(
            int width,
            int height,
            IEnumerable<KeyValuePair<Item, Vector2Int>> items
        ) : this(width, height)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            AddItemsWithPosition(items);
        }

        public Inventory(
            int width,
            int height,
            IEnumerable<Item> items
        ) : this(width, height)
        {
            if (items == null)
                throw new ArgumentNullException( nameof(items));

            AddItemsWithoutPosition(items);
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
            _inventoryItems = new Dictionary<Item, Vector2Int>(inventory.Count);

            CopyFrom(inventory);
        }

        /// <summary>
        /// Checks for adding an item on a specified position
        /// </summary>
        public bool CanAddItem(Item item, Vector2Int position)
        {
            if (!IsValidItem(item) || Contains(item))
                return false;

            if (!IsPositionWithinBounds(position, item.Size))
                return false;

            return IsFreeSpaceBySize(position.x, position.y, item.Size.x, item.Size.y);
        }

        public bool CanAddItem(Item item, int startX, int startY)
        {
            return CanAddItem(item, new Vector2Int(startX, startY));
        }

        /// <summary>
        /// Adds an item on a specified position
        /// </summary>
        public bool AddItem(Item item, Vector2Int position)
        {
            if (!CanAddItem(item, position))
                return false;

            PlaceItem(item, position);
            OnAdded?.Invoke(item, position);
            return true;
        }

        public bool AddItem(Item item, int startX, int startY)
        {
            return AddItem(item, new Vector2Int(startX, startY));
        }

        /// <summary>
        /// Checks for adding an item on a free position
        /// </summary>
        public bool CanAddItem(Item item)
        {
            if (item == null || Contains(item))
                return false;

            if (item.Size.x <= 0 || item.Size.y <= 0)
                throw new ArgumentException("item size must be greater 0");

            return FindFreePosition(item, out _);
        }

        /// <summary>
        /// Adds an item on a free position
        /// </summary>
        public bool AddItem(Item item)
        {
            if (!IsValidItem(item) || Contains(item))
                return false;

            if (!FindFreePosition(item.Size, out var position))
                return false;

            PlaceItem(item, position);
            OnAdded?.Invoke(item, position);
            return true;
        }

        /// <summary>
        /// Returns a free position for a specified item
        /// </summary>
        public bool FindFreePosition(Item item, out Vector2Int position)
        {
            if(item == null)
                throw new ArgumentException("item can't be null", nameof(item));
            
            return FindFreePosition(item.Size, out position);
        }

        public bool FindFreePosition(Vector2Int size, out Vector2Int position)
        {
            if (size.x <= 0 || size.y <= 0)
                throw new ArgumentException("size must be greater 0", nameof(size));
                
            return FindFreePosition(size.x, size.y, out position);
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
        public bool Contains(Item item)
        {
            return item != null && _inventoryItems.ContainsKey(item);
        }

        /// <summary>
        /// Checks if the specified position is occupied
        /// </summary>
        public bool IsOccupied(Vector2Int position)
        {
            return IsOccupied(position.x, position.y);
        }

        public bool IsOccupied(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                throw new IndexOutOfRangeException("position outside of inventory");

            return _inventory[x, y] != null;
        }

        /// <summary>
        /// Checks if the specified position is free
        /// </summary>
        public bool IsFree(Vector2Int position)
        {
            return !IsOccupied(position);
        }

        public bool IsFree(int x, int y)
        {
            return !IsOccupied(x, y);
        }

        /// <summary>
        /// Removes specified item
        /// </summary>
        public bool RemoveItem(Item item)
        {
            return RemoveItemFromGrid(item, out _);
        }

        public bool RemoveItem(Item item, out Vector2Int position)
        {
            var removeItem = RemoveItemFromGrid(item, out position);
            if (removeItem)
                OnRemoved?.Invoke(item, position);

            return removeItem;
        }
        
        private bool RemoveItemFromGrid(in Item item, out Vector2Int position)
        {
            position = default;

            if (item == null || !_inventoryItems.TryGetValue(item, out position))
                return false;

            var itemSize = item.Size;
            for (var x = position.x; x < position.x + itemSize.x; x++)
            {
                for (var y = position.y; y < position.y + itemSize.y; y++)
                {
                    _inventory[x, y] = null;
                }
            }

            _inventoryItems.Remove(item);
            return true;
        }

        /// <summary>
        /// Returns an item at specified position 
        /// </summary>
        public Item GetItem(Vector2Int position)
        {
            return  GetItem(position.x, position.y);
        }

        public Item GetItem(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                throw new IndexOutOfRangeException("position outside of inventory");

            return _inventory[x, y];
        }

        public bool TryGetItem(Vector2Int position, out Item item)
        {
            return TryGetItem(position.x, position.y, out item);
        }

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
            if (!TryGetPositions(item, out var positions))
                throw item == null
                    ? new NullReferenceException("item can't be null")
                    : new KeyNotFoundException("item not found in inventory");

            return positions;
        }

        public bool TryGetPositions(Item item, out Vector2Int[] positions)
        {
            positions = null;

            if (item == null || !_inventoryItems.TryGetValue(item, out var startPosition))
                return false;

            positions = new Vector2Int[item.Size.x * item.Size.y];
            var itemSize = item.Size;
            var i = 0;

            for (var x = 0; x < itemSize.x; x++)
            {
                for (var y = 0; y < itemSize.y; y++)
                {
                    positions[i++] = new Vector2Int(startPosition.x + x, startPosition.y + y);
                }
            }

            return true;
        }

        /// <summary>
        /// Clears all items 
        /// </summary>
        public void Clear()
        {
            if (_inventoryItems.Count <= 0) 
                return;
            
            Array.Clear(_inventory, 0, _inventory.Length);
            _inventoryItems.Clear();
            OnCleared?.Invoke();
        }

        /// <summary>
        /// Returns count of items with a specified name
        /// </summary>
        public int GetItemCount(string name)
        {
            var count = 0;

            foreach (var item in _inventoryItems.Keys)
            {
                if (item.Name == name)
                    count++;
            }

            return count;
        }

        public bool MoveItem(Item item, Vector2Int position)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), "item can't be null");

            if (!_inventoryItems.TryGetValue(item, out var oldPosition))
                return false;
            
            if (!IsPositionWithinBounds(position, item.Size))
                return false;

            RemoveItemFromGrid(item, out _);

            if (!CanAddItem(item, position))
            {
                PlaceItem(item, oldPosition);
                return false;
            }

            PlaceItem(item, position);
            OnMoved?.Invoke(item, position);
            return true;
        }

        /// <summary>
        /// Rearranges an inventory space with max free slots 
        /// </summary>
        public void OptimizeSpace()
        {
            if (_inventoryItems.Count == 0)
                return;
            
            var sortedItems = new List<Item>(_inventoryItems.Keys);
            
            sortedItems.Sort((a, b) =>
            {
                var areaComparison = (b.Size.x * b.Size.y).CompareTo(a.Size.x * a.Size.y);
                if (areaComparison != 0)
                    return areaComparison;
              
                var widthComparison = b.Size.x.CompareTo(a.Size.x);
                if (widthComparison != 0)
                    return widthComparison;

                return b.Size.x.CompareTo(a.Size.x);
            });
            
            Array.Clear(_inventory, 0, _inventory.Length);
            
            foreach (var item in sortedItems)
            {
                if (!FindFreePosition(item.Size, out var position))
                    throw new ArgumentException("error reorganizing space");
            
                PlaceItem(item, position);
            }
        }

        /// <summary>
        /// Iterates by all items 
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<Item> GetEnumerator()
        {
            return _inventoryItems.Keys.GetEnumerator();
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
            var stringBuilder = new StringBuilder();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var item = _inventory[x, y];
                    stringBuilder.Append(item == null
                        ? ". "
                        : $"{item.Name[0]} ");
                }
                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString();
        }

        #region Helpers

        /// <summary>
        /// Adds items to specific inventory slots
        /// </summary>
        /// <param name="items"></param>
        /// <exception cref="ArgumentException"></exception>
        private void AddItemsWithPosition(IEnumerable<KeyValuePair<Item, Vector2Int>> items)
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
        /// <param name="items"></param>
        /// <exception cref="ArgumentException"></exception>
        private void AddItemsWithoutPosition(IEnumerable<Item> items)
        {
            foreach (var item in items)
            {
                if (!FindFreePosition(item.Size, out var position))
                    throw new ArgumentException("inventory has no free slots");

                AddItem(item, position);
            }
        }

        private bool IsValidItem(Item item)
        {
            if (item == null)
                return false;

            if (item.Size.x <= 0 || item.Size.y <= 0)
                throw new ArgumentException("Item size must be positive and greater than zero");

            return true;
        }

        private bool IsPositionWithinBounds(Vector2Int position, Vector2Int size)
        {
            return position.x >= 0 && position.y >= 0 &&
                   position.x + size.x <= Width && position.y + size.y <= Height;
        }

        private bool IsFreeSpaceBySize(int startX, int startY, int sizeX, int sizeY)
        {
            var endX = startX + sizeX - 1;
            var endY = startY + sizeY - 1;

            return IsFreeSpace(startX, startY, endX, endY);
        }
        
        private void CopyFrom(Inventory inventory)
        {
            foreach (var pair in inventory._inventoryItems)
            {
                var clonedItem = pair.Key.Clone();
                var position = pair.Value;

                AddItem(clonedItem, position);
            }
        }
        
        private void PlaceItem(Item item, Vector2Int position)
        {
            for (int x = position.x; x < position.x + item.Size.x; x++)
            {
                for (int y = position.y; y < position.y + item.Size.y; y++)
                {
                    _inventory[x, y] = item;
                }
            }

            _inventoryItems[item] = position;
        }
        
        #endregion
    }
}