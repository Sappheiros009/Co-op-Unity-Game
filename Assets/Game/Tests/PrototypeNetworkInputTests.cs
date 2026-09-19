using System;
using NUnit.Framework;
using UnityEngine;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypeNetworkInputTests
    {
        private static PrototypeNetworkPlayerInput Packet(long sequence = 1, string run = "run-a", int stage = 1)
        {
            var input = PrototypePlayerInput.Neutral(90, 20);
            input.hasControl = true; input.motor.move = Vector2.up; input.motor.sprint = true;
            input.interaction.hasControl = true;
            return new PrototypeNetworkPlayerInput { runId = run, stage = stage, sequence = sequence, input = input };
        }
        private static PrototypeNetworkInputBuffer Buffer() => new PrototypeNetworkInputBuffer("run-a", 1, new ulong[] { 12, 47, 83, 99 });

        [Test] public void ConnectionBindingCannotSelectAnotherActor()
        {
            var buffer = Buffer();
            Assert.IsTrue(buffer.Submit(47, Packet(), 1, out _));
            Assert.IsTrue(buffer.TryGetActor(47, out var actor)); Assert.AreEqual(1, actor);
            Assert.IsFalse(buffer.Consume(0, 1).hasControl);
            Assert.AreEqual(Vector2.up, buffer.Consume(1, 1).motor.move);
            Assert.IsFalse(buffer.Submit(48, Packet(), 1, out var reason)); Assert.AreEqual("unknown_connection", reason);
            Assert.AreEqual(0, buffer.LastSequence(0)); Assert.AreEqual(1, buffer.LastSequence(1));
        }
        [Test] public void InputContainsOnlyIntentAndRoundTripsNestedControls()
        {
            var packet = Packet(); packet.input.motor.jump = true; packet.input.motor.climb = true;
            packet.input.interaction.usePressed = true; packet.input.interaction.selectedSlot = 2;
            var json = JsonUtility.ToJson(packet);
            var decoded = JsonUtility.FromJson<PrototypeNetworkPlayerInput>(json);
            Assert.IsTrue(decoded.input.IsValid); Assert.AreEqual(Vector2.up, decoded.input.motor.move);
            Assert.IsTrue(decoded.input.motor.jump); Assert.IsTrue(decoded.input.motor.climb);
            Assert.IsTrue(decoded.input.interaction.usePressed); Assert.AreEqual(2, decoded.input.interaction.selectedSlot);
            Assert.AreEqual(90, decoded.input.yaw); Assert.AreEqual(20, decoded.input.pitch);
            foreach (var prohibited in new[] { "position", "health", "score", "deltaTime", "actorId", "slot" })
                Assert.IsNull(typeof(PrototypeNetworkPlayerInput).GetField(prohibited));
        }
        [Test] public void OneShotEdgesAreConsumedOnceButHeldMovementPersists()
        {
            var buffer = Buffer(); var first = Packet();
            first.input.motor.jump = first.input.pingPressed = first.input.necklacePressed = true;
            first.input.interaction.interactPressed = first.input.interaction.interactHeld = true;
            first.input.interaction.usePressed = first.input.interaction.dropPressed = true;
            first.input.interaction.selectedSlot = 2;
            Assert.IsTrue(buffer.Submit(12, first, 1, out _));
            var next = Packet(2); next.input.interaction.interactHeld = true;
            Assert.IsTrue(buffer.Submit(12, next, 1.01, out _));
            var consumed = buffer.Consume(0, 1.02);
            Assert.IsTrue(consumed.motor.jump); Assert.IsTrue(consumed.interaction.usePressed);
            Assert.IsTrue(consumed.interaction.dropPressed); Assert.AreEqual(2, consumed.interaction.selectedSlot);
            Assert.IsTrue(consumed.pingPressed); Assert.IsTrue(consumed.necklacePressed);
            var held = buffer.Consume(0, 1.03);
            Assert.AreEqual(Vector2.up, held.motor.move); Assert.IsTrue(held.interaction.interactHeld);
            Assert.IsFalse(held.motor.jump); Assert.IsFalse(held.interaction.interactPressed);
            Assert.IsFalse(held.interaction.usePressed); Assert.IsFalse(held.interaction.dropPressed);
            Assert.AreEqual(-1, held.interaction.selectedSlot); Assert.IsFalse(held.pingPressed); Assert.IsFalse(held.necklacePressed);
        }
        [Test] public void StaleDuplicateReorderedAndLargeJumpPacketsCannotReplaceAcceptedInput()
        {
            var buffer = Buffer(); Assert.IsTrue(buffer.Submit(12, Packet(3), 1, out _));
            foreach (var packet in new[] { Packet(3), Packet(2), Packet(2000), Packet(4, "old-run"), Packet(4, stage: 2) })
                Assert.IsFalse(buffer.Submit(12, packet, 1.02, out _));
            Assert.AreEqual(3, buffer.LastSequence(0)); Assert.AreEqual(Vector2.up, buffer.Consume(0, 1.03).motor.move);
        }
        [Test] public void NonFiniteOutOfRangeControlsAndInvalidServerTimeAreRejected()
        {
            var buffer = Buffer();
            var invalid = new[] { Packet(), Packet(), Packet(), Packet(), Packet(), Packet() };
            invalid[0].input.motor.move.x = float.NaN; invalid[1].input.motor.move.y = 2;
            invalid[2].input.yaw = float.PositiveInfinity; invalid[3].input.pitch = 83;
            invalid[4].input.interaction.selectedSlot = 3; invalid[5].input.yaw = -1;
            foreach (var packet in invalid) Assert.IsFalse(buffer.Submit(12, packet, 1, out _));
            Assert.IsFalse(buffer.Submit(12, Packet(), double.NaN, out _));
            Assert.IsFalse(buffer.Submit(12, Packet(), -1, out _));
            Assert.AreEqual(0, buffer.LastSequence(0)); Assert.IsFalse(buffer.Consume(0, 1).hasControl);
            Assert.IsTrue(buffer.Submit(12, Packet(), 2, out _));
            Assert.IsFalse(buffer.Submit(12, Packet(2), 1.9, out _));
        }
        [Test] public void SilenceReleasesHeldControlsAndDoesNotReplayOldEdgesOnResumption()
        {
            var buffer = Buffer(); var first = Packet(); first.input.motor.jump = true;
            Assert.IsTrue(buffer.Submit(12, first, 1, out _));
            Assert.IsFalse(buffer.Consume(0, 1.26).hasControl);
            Assert.IsTrue(buffer.Submit(12, Packet(2), 1.27, out _));
            Assert.IsFalse(buffer.Consume(0, 1.27).motor.jump);
            Assert.IsFalse(buffer.Consume(0, double.PositiveInfinity).hasControl);
        }
        [Test] public void OwnerDepartureDoesNotShrinkRosterOrAffectAnotherPlayersInput()
        {
            var buffer = Buffer(); Assert.IsTrue(buffer.Submit(12, Packet(), 1, out _));
            Assert.IsTrue(buffer.Submit(47, Packet(), 1, out _));
            buffer.Disconnect(12);
            Assert.AreEqual(4, buffer.StartingCount); Assert.AreEqual("run-a", buffer.RunId);
            Assert.IsFalse(buffer.TryGetActor(12, out _)); Assert.IsFalse(buffer.Consume(0, 1.01).hasControl);
            Assert.IsTrue(buffer.Consume(1, 1.01).hasControl);
            Assert.IsFalse(buffer.Submit(12, Packet(2), 1.02, out _));
        }
        [Test] public void StageChangeFlushesInputButKeepsSequenceAndDepartedSlots()
        {
            var buffer = Buffer(); Assert.IsTrue(buffer.Submit(12, Packet(), 1, out _));
            buffer.Disconnect(47); buffer.AdvanceStage(2);
            Assert.AreEqual(4, buffer.StartingCount); Assert.AreEqual(1, buffer.LastSequence(0));
            Assert.IsFalse(buffer.Consume(0, 2).hasControl);
            Assert.IsFalse(buffer.Submit(12, Packet(2), 2, out var reason)); Assert.AreEqual("stale_stage", reason);
            Assert.IsTrue(buffer.Submit(12, Packet(2, stage: 2), 2, out _));
            Assert.IsFalse(buffer.Submit(47, Packet(1, stage: 2), 2, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.AdvanceStage(4));
        }
        [Test] public void RateBudgetIsPerConnectionAndPacketsDoNotPerformSimulation()
        {
            var buffer = Buffer();
            for (var i = 1; i <= PrototypeNetworkInputBuffer.RequestsPerSecond; i++)
                Assert.IsTrue(buffer.Submit(12, Packet(i), 1, out _));
            Assert.IsFalse(buffer.Submit(12, Packet(61), 1, out var reason)); Assert.AreEqual("input_rate_limit", reason);
            Assert.IsTrue(buffer.Submit(47, Packet(), 1, out _));
            Assert.IsTrue(buffer.Submit(12, Packet(61), 2.01, out _));
            Assert.AreEqual(61, buffer.LastSequence(0)); Assert.AreEqual(1, buffer.LastSequence(1));
        }
        [Test] public void ReleasingControlClearsUnconsumedActions()
        {
            var buffer = Buffer(); var first = Packet(); first.input.motor.jump = true;
            Assert.IsTrue(buffer.Submit(12, first, 1, out _));
            var released = Packet(2); released.input = PrototypePlayerInput.Neutral();
            Assert.IsTrue(buffer.Submit(12, released, 1.01, out _));
            var result = buffer.Consume(0, 1.02); Assert.IsFalse(result.hasControl); Assert.IsFalse(result.motor.jump);
        }
        [Test] public void InvalidOrDuplicateStartingRosterIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new PrototypeNetworkInputBuffer("run", 1, new ulong[] { 12 }));
            Assert.Throws<ArgumentException>(() => new PrototypeNetworkInputBuffer("run", 1, new ulong[] { 12, 12 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PrototypeNetworkInputBuffer("run", 7, new ulong[] { 12, 47 }));
        }
    }
}
