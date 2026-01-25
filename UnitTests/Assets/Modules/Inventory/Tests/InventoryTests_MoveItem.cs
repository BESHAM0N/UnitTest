using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public partial class InventoryTests
    {
        [Test]
        public void MoveItem_ItemIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            var inventory = new Inventory(5, 5);

            // Assert
            Assert.Throws<ArgumentNullException>(() => inventory.MoveItem(null, new Vector2Int(1, 1)));
        }
        
        [TestCaseSource(nameof(MoveItem_SuccessfulCases))]
        public void MoveItem_ValidMove_ReturnsTrue(
            int width,
            int height,
            KeyValuePair<Item, Vector2Int>[] initialItems,
            Item item,
            Vector2Int targetPosition)
        {
            // Arrange
            var inventory = new Inventory(width, height, initialItems);

            // Act
            var success = inventory.MoveItem(item, targetPosition);

            // Assert
            Assert.IsTrue(success);
        }

        [TestCaseSource(nameof(MoveItem_SuccessfulCases))]
        public void MoveItem_ValidMove_RaisesOnMovedWithExpectedArgs(
            int width,
            int height,
            KeyValuePair<Item, Vector2Int>[] initialItems,
            Item item,
            Vector2Int targetPosition)
        {
            // Arrange
            var inventory = new Inventory(width, height, initialItems);

            bool eventRaised = false;
            Item movedItem = null;
            Vector2Int movedPosition = default;

            inventory.OnMoved += (i, p) =>
            {
                eventRaised = true;
                movedItem = i;
                movedPosition = p;
            };

            // Act
            inventory.MoveItem(item, targetPosition);

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.AreEqual(item, movedItem);
            Assert.AreEqual(targetPosition, movedPosition);
        }

        [TestCaseSource(nameof(MoveItem_SuccessfulCases))]
        public void MoveItem_ValidMove_ItemIsPresentAtAllItsPositions(
            int width,
            int height,
            KeyValuePair<Item, Vector2Int>[] initialItems,
            Item item,
            Vector2Int targetPosition)
        {
            // Arrange
            var inventory = new Inventory(width, height, initialItems);

            // Act
            inventory.MoveItem(item, targetPosition);

            // Assert
            foreach (var pos in inventory.GetPositions(item))
                Assert.AreEqual(item, inventory.GetItem(pos), $"Expected item at {pos}");
        }

        [TestCaseSource(nameof(MoveItem_SuccessfulCases))]
        public void MoveItem_ValidMove_DoesNotRaiseOnAddedOrOnRemoved(
            int width,
            int height,
            KeyValuePair<Item, Vector2Int>[] initialItems,
            Item item,
            Vector2Int targetPosition)
        {
            // Arrange
            var inventory = new Inventory(width, height, initialItems);

            bool added = false;
            bool removed = false;
            inventory.OnAdded += (_, __) => added = true;
            inventory.OnRemoved += (_, __) => removed = true;

            // Act
            inventory.MoveItem(item, targetPosition);

            // Assert
            Assert.IsFalse(added);
            Assert.IsFalse(removed);
        }

        private static IEnumerable<TestCaseData> MoveItem_SuccessfulCases()
        {
            const int width = 5;
            const int height = 5;

            // 1) простое перемещение 1x1
            var item1 = new Item(1, 1);
            yield return new TestCaseData(
                width,
                height,
                new[] { new KeyValuePair<Item, Vector2Int>(item1, new Vector2Int(0, 0)) },
                item1,
                new Vector2Int(1, 1)
            ).SetName("MoveItem_1x1_ToFreeCell_Succeeds");

            // 2) перемещение большого предмета "пересекается сам с собой" (часть клеток совпадает)
            var item2 = new Item(2, 2);
            yield return new TestCaseData(
                width,
                height,
                new[] { new KeyValuePair<Item, Vector2Int>(item2, new Vector2Int(0, 0)) },
                item2,
                new Vector2Int(1, 1)
            ).SetName("MoveItem_2x2_ShiftByOne_OverlapsOldArea_Succeeds");
        }
        
        
         [TestCaseSource(nameof(MoveItem_FailedCases))]
        public void MoveItem_Failed_ReturnsFalse(
            int width,
            int height,
            KeyValuePair<Item, Vector2Int>[] initialItems,
            Item item,
            Vector2Int targetPosition)
        {
            // Arrange
            var inventory = new Inventory(width, height, initialItems);

            // Act
            var success = inventory.MoveItem(item, targetPosition);

            // Assert
            Assert.IsFalse(success);
        }

        [TestCaseSource(nameof(MoveItem_FailedCases))]
        public void MoveItem_Failed_DoesNotRaiseOnMovedEvent(
            int width,
            int height,
            KeyValuePair<Item, Vector2Int>[] initialItems,
            Item item,
            Vector2Int targetPosition)
        {
            // Arrange
            var inventory = new Inventory(width, height, initialItems);

            bool movedEventRaised = false;
            inventory.OnMoved += (_, __) => movedEventRaised = true;

            // Act
            inventory.MoveItem(item, targetPosition);

            // Assert
            Assert.IsFalse(movedEventRaised);
        }

        [TestCaseSource(nameof(MoveItem_FailedCases))]
        public void MoveItem_Failed_ItemIsNotPlacedAtTargetPosition(
            int width,
            int height,
            KeyValuePair<Item, Vector2Int>[] initialItems,
            Item item,
            Vector2Int targetPosition)
        {
            // Arrange
            var inventory = new Inventory(width, height, initialItems);

            // Act
            inventory.MoveItem(item, targetPosition);

            // Assert
            // Если цель вне границ — ожидаем, что TryGetItem не сможет достать предмет.
            if (targetPosition.x < 0 || targetPosition.y < 0 ||
                targetPosition.x >= width || targetPosition.y >= height)
            {
                var got = inventory.TryGetItem(targetPosition, out _);
                Assert.IsFalse(got);
                return;
            }

            inventory.TryGetItem(targetPosition, out var actualItem);
            Assert.AreNotEqual(item, actualItem);
        }

        public static IEnumerable<TestCaseData> MoveItem_FailedCases()
        {
            foreach (var @case in MoveItem_ItemNotPresent_Cases())
                yield return @case;

            foreach (var @case in MoveItem_AtPositionOutOfBounds_Cases())
                yield return @case;

            foreach (var @case in MoveItem_AtPositionThatNearBounds_FailedCases())
                yield return @case;

            foreach (var @case in MoveItem_WhenIntersectsWithAnother_FailedCases())
                yield return @case;
        }

        private static IEnumerable<TestCaseData> MoveItem_ItemNotPresent_Cases()
        {
            const int width = 5;
            const int height = 5;

            // item отсутствует в инвентаре
            var absentItem = new Item(1, 1);

            yield return new TestCaseData(
                width,
                height, Array.Empty<KeyValuePair<Item, Vector2Int>>(),
                absentItem,
                new Vector2Int(2, 2)
            ).SetName("MoveItem_ItemNotInInventory_ReturnsFalse");
        }

        private static IEnumerable<TestCaseData> MoveItem_AtPositionOutOfBounds_Cases()
        {
            const int width = 5;
            const int height = 5;

            var positions = new[]
            {
                new Vector2Int(-1, -1),
                new Vector2Int(-1,  1),
                new Vector2Int( 1, -1),
                new Vector2Int( 5,  5),
                new Vector2Int( 5,  0),
                new Vector2Int( 0,  5),
            };

            foreach (var pos in positions)
            {
                var item = new Item(1, 1);

                yield return new TestCaseData(
                    width,
                    height,
                    new[] { new KeyValuePair<Item, Vector2Int>(item, new Vector2Int(0, 0)) },
                    item,
                    pos
                ).SetName($"MoveItem_TargetOutOfBounds_{pos}_ReturnsFalse");
            }
        }

        private static IEnumerable<TestCaseData> MoveItem_AtPositionThatNearBounds_FailedCases()
        {
            const int width = 5;
            const int height = 5;

            // большой предмет, который “почти влезает”, но вылезает за край
            var positions = new[]
            {
                new Vector2Int(3, 3),
                new Vector2Int(4, 2),
            };

            foreach (var pos in positions)
            {
                var item = new Item(3, 3);

                yield return new TestCaseData(
                    width,
                    height,
                    new[] { new KeyValuePair<Item, Vector2Int>(item, new Vector2Int(0, 0)) },
                    item,
                    pos
                ).SetName($"MoveItem_ItemDoesNotFitAt_{pos}_ReturnsFalse");
            }
        }

        private static IEnumerable<TestCaseData> MoveItem_WhenIntersectsWithAnother_FailedCases()
        {
            const int width = 3;
            const int height = 3;

            var x = new Item("X", 2, 2);
            var z = new Item("Z", 2, 1);

            yield return new TestCaseData(
                width,
                height,
                new[]
                {
                    new KeyValuePair<Item, Vector2Int>(x, new Vector2Int(0, 0)),
                    new KeyValuePair<Item, Vector2Int>(z, new Vector2Int(0, 2)),
                },
                z,
                new Vector2Int(1, 1)
            ).SetName("MoveItem_IntersectsAnotherItem_ReturnsFalse");
        }
    }
}