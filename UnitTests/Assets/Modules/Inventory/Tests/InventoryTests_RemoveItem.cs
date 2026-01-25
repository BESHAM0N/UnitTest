using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public sealed partial class InventoryTests
    {
        [TestCaseSource(nameof(RemoveItem_FailedCases))]
        public void RemoveItem_Failed_ReturnsFalse(KeyValuePair<Item, Vector2Int>[] initialItems, Item item)
        {
            // Arrange
            var inventory = new Inventory(5, 5);
            foreach (KeyValuePair<Item, Vector2Int> pair in initialItems)
                inventory.AddItem(pair.Key, pair.Value);

            // Act
            bool success = inventory.RemoveItem(item, out _);

            // Assert
            Assert.IsFalse(success);
        }

        [TestCaseSource(nameof(RemoveItem_FailedCases))]
        public void RemoveItem_Failed_OutPositionIsZero(KeyValuePair<Item, Vector2Int>[] initialItems, Item item)
        {
            // Arrange
            var inventory = new Inventory(5, 5);
            foreach (KeyValuePair<Item, Vector2Int> pair in initialItems)
                inventory.AddItem(pair.Key, pair.Value);

            // Act
            inventory.RemoveItem(item, out var actualPosition);

            // Assert
            Assert.AreEqual(Vector2Int.zero, actualPosition);
        }

        [TestCaseSource(nameof(RemoveItem_FailedCases))]
        public void RemoveItem_Failed_DoesNotRaiseOnRemovedEvent(KeyValuePair<Item, Vector2Int>[] initialItems,
            Item item)
        {
            // Arrange
            var inventory = new Inventory(5, 5);
            foreach (KeyValuePair<Item, Vector2Int> pair in initialItems)
                inventory.AddItem(pair.Key, pair.Value);

            // Arrange
            bool eventRaised = false;
            inventory.OnRemoved += (_, __) => eventRaised = true;

            // Act
            inventory.RemoveItem(item, out _);

            // Assert
            Assert.IsFalse(eventRaised);
        }

        private static IEnumerable<TestCaseData> RemoveItem_FailedCases()
        {
            yield return new TestCaseData(
                Array.Empty<KeyValuePair<Item, Vector2Int>>(),
                null
            ).SetName("ItemIsNull");

            yield return new TestCaseData(
                new KeyValuePair<Item, Vector2Int>[]
                {
                    new(new Item("X", 2, 2), new Vector2Int(2, 2))
                },
                new Item("Y", 5, 5)
            ).SetName("ItemNotInInventory");
        }


        [TestCaseSource(nameof(RemoveSuccessfulCases))]
        public void RemoveItem_ValidItem_ReturnsTrue(
            int width,
            int height,
            KeyValuePair<Item, Vector2Int>[] initialItems,
            Item itemToRemove,
            Vector2Int _)
        {
            // Arrange
            var inventory = new Inventory(width, height, initialItems);

            // Act
            var success = inventory.RemoveItem(itemToRemove, out _);

            // Assert
            Assert.IsTrue(success);
        }

        [TestCaseSource(nameof(RemoveSuccessfulCases))]
        public void RemoveItem_ValidItem_OutPositionEqualsExpected(
            int width,
            int height,
            KeyValuePair<Item, Vector2Int>[] initialItems,
            Item itemToRemove,
            Vector2Int expectedPosition)
        {
            // Arrange
            var inventory = new Inventory(width, height, initialItems);

            // Act
            inventory.RemoveItem(itemToRemove, out var actualPosition);

            // Assert
            Assert.AreEqual(expectedPosition, actualPosition);
        }

        [TestCaseSource(nameof(RemoveSuccessfulCases))]
        public void RemoveItem_ValidItem_RaisesOnRemovedWithExpectedArgs(
            int width,
            int height,
            KeyValuePair<Item, Vector2Int>[] initialItems,
            Item itemToRemove,
            Vector2Int expectedPosition)
        {
            // Arrange
            var inventory = new Inventory(width, height, initialItems);

            bool eventRaised = false;
            Item removedItem = null;
            Vector2Int removedPosition = default;

            inventory.OnRemoved += (i, p) =>
            {
                eventRaised = true;
                removedItem = i;
                removedPosition = p;
            };

            // Act
            inventory.RemoveItem(itemToRemove, out _);

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.AreEqual(itemToRemove, removedItem);
            Assert.AreEqual(expectedPosition, removedPosition);
        }

        [TestCaseSource(nameof(RemoveSuccessfulCases))]
        public void RemoveItem_ValidItem_DecrementsCountAndRemovesFromInventory(
            int width,
            int height,
            KeyValuePair<Item, Vector2Int>[] initialItems,
            Item itemToRemove,
            Vector2Int _)
        {
            // Arrange
            var inventory = new Inventory(width, height, initialItems);
            var countBefore = inventory.Count;

            // Act
            inventory.RemoveItem(itemToRemove, out _);

            // Assert
            Assert.AreEqual(countBefore - 1, inventory.Count);
            Assert.IsFalse(inventory.Contains(itemToRemove));
        }

        [TestCaseSource(nameof(RemoveSuccessfulCases))]
        public void RemoveItem_ValidItem_FreesAllPreviouslyOccupiedSlots(
            int width,
            int height,
            KeyValuePair<Item, Vector2Int>[] initialItems,
            Item itemToRemove,
            Vector2Int expectedPosition)
        {
            // Arrange
            var inventory = new Inventory(width, height, initialItems);

            // Act
            inventory.RemoveItem(itemToRemove, out _);

            // Assert
            for (int x = expectedPosition.x; x < expectedPosition.x + itemToRemove.Size.x; x++)
            for (int y = expectedPosition.y; y < expectedPosition.y + itemToRemove.Size.y; y++)
                Assert.IsTrue(inventory.IsFree(x, y), $"Slot [{x},{y}] should be free after removing item");
        }
        
        private static IEnumerable<TestCaseData> RemoveSuccessfulCases()
        {
            var item1 = new Item("X", width: 2, height: 2);
            yield return new TestCaseData(
                5, 5,
                new[]
                {
                    new KeyValuePair<Item, Vector2Int>(item1, new Vector2Int(2, 2))
                },
                item1,
                new Vector2Int(2, 2)
            ).SetName("RemoveItem_SingleItem_ReturnsTrue");

            var item2 = new Item("X", width: 3, height: 2);
            var itemD = new Item("D", width: 1, height: 2);

            yield return new TestCaseData(
                5, 5,
                new[]
                {
                    new KeyValuePair<Item, Vector2Int>(item2, new Vector2Int(1, 2)),
                    new KeyValuePair<Item, Vector2Int>(itemD, new Vector2Int(4, 0))
                },
                item2,
                new Vector2Int(1, 2)
            ).SetName("RemoveItem_OneOfTwoItems_ReturnsTrue");
        }
    }
}