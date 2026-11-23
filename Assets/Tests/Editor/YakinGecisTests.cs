using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class YakinGecisTests
{
    private Type _geometry;
    private Type _shape;

    [SetUp]
    public void Setup()
    {
        _geometry = Type.GetType("YakinGecisMesafe, Assembly-CSharp", true);
        _shape = _geometry.GetNestedType("Sekil");
    }

    private object Shape(Vector3 center, bool sphere = false, Quaternion? rotation = null, Vector3? halfSize = null)
    {
        object shape = Activator.CreateInstance(_shape);
        _shape.GetField("Gecerli").SetValue(shape, true);
        _shape.GetField("Kure").SetValue(shape, sphere);
        _shape.GetField("Merkez").SetValue(shape, center);
        _shape.GetField("Donus").SetValue(shape, rotation ?? Quaternion.identity);
        _shape.GetField("YariBoyut").SetValue(shape, halfSize ?? Vector3.one);
        _shape.GetField("Yaricap").SetValue(shape, 1f);
        return shape;
    }

    private float Gap(object a, object b)
    {
        return (float)_geometry.GetMethod("Bosluk").Invoke(null, new[] { a, b });
    }

    private void Sweep(object a0, object a1, object b0, object b1, float threshold, bool expectedNear, bool expectedContact)
    {
        object[] args = { a0, a1, b0, b1, threshold, false, false };
        _geometry.GetMethod("GecisiOlc").Invoke(null, args);
        Assert.That((bool)args[5], Is.EqualTo(expectedNear), "Near-pass verdict");
        Assert.That((bool)args[6], Is.EqualTo(expectedContact), "Contact verdict");
    }

    [TestCase(false)] [TestCase(true)]
    public void SurfaceGapIsNotPivotDistance(bool sphere)
    {
        Assert.That(Gap(Shape(Vector3.zero), Shape(new Vector3(0, 0, 2.5f), sphere)), Is.EqualTo(0.5f).Within(0.0001f));
    }

    [Test]
    public void SphereAtBoxCornerUsesRealSurfaceNotBounds()
    {
        Assert.That(Gap(Shape(Vector3.zero), Shape(new Vector3(2, 0, 2), true)), Is.EqualTo(Mathf.Sqrt(2) - 1).Within(0.0001f));
    }

    [Test]
    public void RotatedBoxDistanceIsSymmetricAndNotAabbOverlap()
    {
        object a = Shape(Vector3.zero, false, Quaternion.Euler(0, 45, 0), new Vector3(2, 1, 0.2f));
        object b = Shape(new Vector3(0, 0, 2), false, Quaternion.Euler(0, 45, 0), new Vector3(2, 1, 0.2f));
        float gap = Gap(a, b);
        Assert.That(gap, Is.EqualTo(Mathf.Sqrt(2) - 0.4f).Within(0.002f));
        Assert.That(Gap(b, a), Is.EqualTo(gap).Within(0.0001f));
    }

    [TestCase(2.5f, 1f, true)]
    [TestCase(-2.5f, 1f, true)]
    [TestCase(3.1f, 1f, false)]
    [TestCase(2.5f, 0.4f, false)]
    [TestCase(2.5f, 0.5f, true)]
    public void ThresholdAndBothSides(float z, float threshold, bool near)
    {
        object player = Shape(Vector3.zero);
        Sweep(player, player, Shape(new Vector3(-8, 0, z)), Shape(new Vector3(8, 0, z)), threshold, near, false);
    }

    [TestCase(false)] [TestCase(true)]
    public void HighSpeedPassBetweenFramesIsObserved(bool sphere)
    {
        object player = Shape(Vector3.zero);
        Sweep(player, player, Shape(new Vector3(-100, 0, 2.5f), sphere), Shape(new Vector3(100, 0, 2.5f), sphere), 1f, true, false);
    }

    [TestCase(false)] [TestCase(true)]
    public void CrossingThroughColliderIsContactNotBonus(bool sphere)
    {
        object player = Shape(Vector3.zero);
        Sweep(player, player, Shape(new Vector3(-20, 0, 0), sphere), Shape(new Vector3(20, 0, 0), sphere), 1f, false, true);
    }

    [Test]
    public void PassingWellAboveObstacleIsNotSidePass()
    {
        object player = Shape(new Vector3(0, 5, 0));
        Sweep(player, player, Shape(new Vector3(-8, 0, 0)), Shape(new Vector3(8, 0, 0)), 10f, false, false);
    }

    [Test]
    public void DistantParallelObstacleIsNotNearPass()
    {
        object player = Shape(Vector3.zero);
        object obstacle = Shape(new Vector3(-20, 0, 2.5f));
        Sweep(player, player, obstacle, obstacle, 1f, false, false);
    }
}
