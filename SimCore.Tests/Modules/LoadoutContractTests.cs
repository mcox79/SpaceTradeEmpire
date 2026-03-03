using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimCore.Entities;

namespace SimCore.Tests.Modules
{
    [TestFixture]
    public sealed class LoadoutContractTests
    {
        // Standard hero ship slot set mirroring EnsurePlayerFleetV0 in SimBridge.
        // Ordered by slot_id Ordinal asc (c < e < u < w).
        private static List<ModuleSlot> MakeStandardSlots() => new()
        {
            new() { SlotId = "slot_cargo_0",   SlotKind = SlotKind.Cargo },
            new() { SlotId = "slot_engine_0",  SlotKind = SlotKind.Engine },
            new() { SlotId = "slot_utility_0", SlotKind = SlotKind.Utility },
            new() { SlotId = "slot_weapon_0",  SlotKind = SlotKind.Weapon },
        };

        [Test]
        [Category("LoadoutContract")]
        public void HeroShip_StandardSlots_CountAtLeastOne()
        {
            var slots = MakeStandardSlots();
            Assert.That(slots.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        [Category("LoadoutContract")]
        public void HeroShip_StandardSlots_FieldShapes()
        {
            var slots = MakeStandardSlots();
            foreach (var slot in slots)
            {
                Assert.That(slot.SlotId.Length, Is.GreaterThan(0),
                    $"SlotId must be non-empty");
                Assert.That(Enum.IsDefined(typeof(SlotKind), slot.SlotKind), Is.True,
                    $"SlotKind({slot.SlotId}) must be a defined enum value");
                Assert.That(slot.InstalledModuleId, Is.Null,
                    $"Slot({slot.SlotId}) InstalledModuleId default must be null");
            }
        }

        [Test]
        [Category("LoadoutContract")]
        public void HeroShip_StandardSlots_OrderedBySlotIdOrdinalAsc()
        {
            var slots = MakeStandardSlots();
            var ids = slots.Select(s => s.SlotId).ToList();
            var sorted = ids.OrderBy(x => x, StringComparer.Ordinal).ToList();
            Assert.That(ids, Is.EqualTo(sorted),
                $"Slots must be ordered by slot_id Ordinal asc. " +
                $"Actual=[{string.Join(",", ids)}] Expected=[{string.Join(",", sorted)}]");
        }

        [Test]
        [Category("LoadoutContract")]
        public void Fleet_Slots_AttachableToFleet()
        {
            var fleet = new Fleet { Id = "fleet_test", OwnerId = "player" };
            fleet.Slots = MakeStandardSlots();
            Assert.That(fleet.Slots.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(fleet.Slots[0].SlotId.Length, Is.GreaterThan(0));
        }
    }
}
