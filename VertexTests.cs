// Fix for CS0117: Replace Assert.True with NUnit.Framework.Assert.That
// Fix for SPELL errors: Correct spelling in comments

using System.Numerics;
using System.Collections.Generic;
using NUnit.Framework;

public class VertexTests
{
    [Test]
    public void Constructor_SetsPosition_And_OutgoingHalfEdgeIsNull()
    {
        var pos = new Vector2(1.5f, -2.25f);
        var v = new Vertex(pos);

        Assert.That(v.Position, Is.EqualTo(pos));
        Assert.That(v.OutgoingHalfEdge, Is.Null);
    }

    [Test]
    public void ToString_FormatsWithTwoDecimals()
    {
        var v = new Vertex(new Vector2(1.2345f, 2.3456f));
        Assert.That(v.ToString(), Is.EqualTo("Vertex(1.23, 2.35)"));
    }

    [Test]
    public void Equals_SameReference_ReturnsTrue()
    {
        var v = new Vertex(new Vector2(0f, 0f));
        Assert.That(v.Equals(v), Is.True); // same instance
    }

    [Test]
    public void Equals_Null_ReturnsFalse()
    {
        var v = new Vertex(new Vector2(0f, 0f));
        Assert.That(v.Equals((Vertex)null), Is.False);
    }

    [Test]
    public void Equals_SamePositionValues_ReturnsTrue()
    {
        var v1 = new Vertex(new Vector2(3.0f, 4.0f));
        var v2 = new Vertex(new Vector2(3.0f, 4.0f));

        Assert.That(v1.Equals(v2), Is.True);
        Assert.That(v2.Equals(v1), Is.True);
    }

    [Test]
    public void Equals_DifferentPositions_ReturnsFalse()
    {
        var v1 = new Vertex(new Vector2(0f, 0f));
        var v2 = new Vertex(new Vector2(1f, 1f));

        Assert.That(v1.Equals(v2), Is.False);
        Assert.That(v2.Equals(v1), Is.False);
    }

    [Test]
    public void Equals_ObjectOverload_NotOverridden_UsesReferenceEquality()
    {
        var v1 = new Vertex(new Vector2(5f, 6f));
        var v2 = new Vertex(new Vector2(5f, 6f));

        Assert.That(v1.Equals((object)v2), Is.False);
    }

    [Test]
    public void HashSet_Behavior_TwoDistinctInstancesWithSamePosition_BothStored()
    {
        var v1 = new Vertex(new Vector2(7f, 8f));
        var v2 = new Vertex(new Vector2(7f, 8f));

        var set = new HashSet<Vertex>();
        set.Add(v1);
        set.Add(v2);

        Assert.That(set.Count, Is.EqualTo(2));
        Assert.That(set.Contains(v1), Is.True);
        Assert.That(set.Contains(v2), Is.True);
    }
}
