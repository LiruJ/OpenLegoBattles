using GlobalShared.DataTypes;
using OpenLegoBattles.Rendering;

namespace GameTests
{
    [TestClass]
    public class TileConnectionTests
    {
        [TestMethod]
        public void TestPossibilities()
        {
            // Create the rule. This is for an L shaped corner piece.
            DirectionMask fullMask = DirectionMask.Top | DirectionMask.TopRight | DirectionMask.Right | DirectionMask.BottomRight | DirectionMask.TopLeft;
            DirectionMask emptyMask = DirectionMask.BottomRight | DirectionMask.Bottom | DirectionMask.BottomLeft | DirectionMask.Left | DirectionMask.TopLeft;
            IReadOnlyList<ValueTuple<DirectionMask, DirectionMask>> masksList = new List<ValueTuple<DirectionMask, DirectionMask>>() { (fullMask, emptyMask) };
            ConnectionRule connectionRule = new(new ushort[] { 0 }, masksList);


            // Add all possible masks of the connection rule, ensuring none are added twice and there are only 4 in total.
            HashSet<byte> masks = new(4);
            foreach (DirectionMask possibleMask in connectionRule.AllPossibleTileMasks())
            {
                if (masks.Contains((byte)possibleMask))
                    Assert.Fail("Tried to add same mask twice.");
                masks.Add((byte)possibleMask);
            }
            Assert.AreEqual(4, masks.Count);

            // Check the 4 possibilites to ensure they match the hand-calculated values.
            Assert.IsTrue(masks.Contains(0b1111_0000));
            Assert.IsTrue(masks.Contains(0b1111_0001));
            Assert.IsTrue(masks.Contains(0b1110_0001));
            Assert.IsTrue(masks.Contains(0b1110_0000));
        }
    }
}