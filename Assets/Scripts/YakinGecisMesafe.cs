using UnityEngine;

public static class YakinGecisMesafe
{
    // Numerical contact tolerance, not a gameplay proximity setting.
    private const float ContactEpsilon = 0.0001f;
    private static readonly int[] Edges = { 0,1, 2,3, 4,5, 6,7, 0,2, 1,3, 4,6, 5,7, 0,4, 1,5, 2,6, 3,7 };

    public struct Sekil
    {
        public bool Gecerli;
        public bool Kure;
        public Vector3 Merkez;
        public Quaternion Donus;
        public Vector3 YariBoyut;
        public float Yaricap;

        public Vector3 Eksen(int index)
        {
            return Donus * (index == 0 ? Vector3.right : index == 1 ? Vector3.up : Vector3.forward);
        }

        public Bounds Sinirlar
        {
            get
            {
                Vector3 extent = Kure ? Vector3.one * Yaricap :
                    Abs(Eksen(0)) * YariBoyut.x + Abs(Eksen(1)) * YariBoyut.y + Abs(Eksen(2)) * YariBoyut.z;
                return new Bounds(Merkez, extent * 2f);
            }
        }
    }

    public static Sekil Oku(Collider collider)
    {
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) return default;
        Vector3 scale = Abs(collider.transform.lossyScale);
        var shape = new Sekil { Donus = collider.transform.rotation };
        if (collider is BoxCollider box)
        {
            shape.Merkez = box.transform.TransformPoint(box.center);
            shape.YariBoyut = Vector3.Scale(box.size * 0.5f, scale);
            shape.Gecerli = shape.YariBoyut.x > 0f && shape.YariBoyut.y > 0f && shape.YariBoyut.z > 0f;
        }
        else if (collider is SphereCollider sphere)
        {
            shape.Kure = true;
            shape.Merkez = sphere.transform.TransformPoint(sphere.center);
            shape.Yaricap = sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            shape.Gecerli = shape.Yaricap > 0f;
        }
        return shape;
    }

    public static float Bosluk(Sekil a, Sekil b)
    {
        if (!a.Gecerli || !b.Gecerli) return float.PositiveInfinity;
        if (a.Kure && b.Kure) return Mathf.Max(0f, Vector3.Distance(a.Merkez, b.Merkez) - a.Yaricap - b.Yaricap);
        if (a.Kure) return Mathf.Max(0f, Vector3.Distance(a.Merkez, EnYakinNokta(b, a.Merkez)) - a.Yaricap);
        if (b.Kure) return Mathf.Max(0f, Vector3.Distance(b.Merkez, EnYakinNokta(a, b.Merkez)) - b.Yaricap);
        if (DunyaEksenlerineParalel(a) && DunyaEksenlerineParalel(b))
        {
            Bounds ba = a.Sinirlar;
            Bounds bb = b.Sinirlar;
            Vector3 gap = new Vector3(Mathf.Max(0f, Mathf.Max(ba.min.x - bb.max.x, bb.min.x - ba.max.x)),
                Mathf.Max(0f, Mathf.Max(ba.min.y - bb.max.y, bb.min.y - ba.max.y)),
                Mathf.Max(0f, Mathf.Max(ba.min.z - bb.max.z, bb.min.z - ba.max.z)));
            return gap.magnitude;
        }
        if (KutularOrtusuyor(a, b)) return 0f;

        float distanceSquared = float.PositiveInfinity;
        for (int i = 0; i < 8; i++)
        {
            Vector3 av = Kose(a, i);
            Vector3 bv = Kose(b, i);
            distanceSquared = Mathf.Min(distanceSquared, (av - EnYakinNokta(b, av)).sqrMagnitude);
            distanceSquared = Mathf.Min(distanceSquared, (bv - EnYakinNokta(a, bv)).sqrMagnitude);
        }
        // A rotated box pair's closest features can be two edges, not vertices.
        for (int i = 0; i < Edges.Length; i += 2)
        {
            Vector3 a0 = Kose(a, Edges[i]);
            Vector3 a1 = Kose(a, Edges[i + 1]);
            for (int j = 0; j < Edges.Length; j += 2)
                distanceSquared = Mathf.Min(distanceSquared, SegmentBoslukKaresi(a0, a1, Kose(b, Edges[j]), Kose(b, Edges[j + 1])));
        }
        return Mathf.Sqrt(distanceSquared);
    }

    public static void GecisiOlc(Sekil a0, Sekil a1, Sekil b0, Sekil b1, float threshold, out bool near, out bool contact)
    {
        near = false;
        contact = false;
        if (!a0.Gecerli || !a1.Gecerli || !b0.Gecerli || !b1.Gecerli) return;
        Bounds sweptA = a0.Sinirlar;
        sweptA.Encapsulate(a1.Sinirlar);
        Bounds sweptB = b0.Sinirlar;
        sweptB.Encapsulate(b1.Sinirlar);
        sweptA.Expand(threshold * 2f);
        if (!sweptA.Intersects(sweptB)) return;

        int intervals = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Quaternion.Angle(a0.Donus, a1.Donus), Quaternion.Angle(b0.Donus, b1.Donus)) / 5f));
        for (int i = 0; i < intervals; i++)
        {
            float left = (float)i / intervals;
            float right = (float)(i + 1) / intervals;
            Degerlendir(a0, a1, b0, b1, left, threshold, ref near, ref contact);
            Degerlendir(a0, a1, b0, b1, right, threshold, ref near, ref contact);
            // For translating convex shapes the separation is convex in time.
            // Search the whole transit, so even a full pass between two frames is observed.
            for (int iteration = 0; iteration < 24; iteration++)
            {
                float t1 = Mathf.Lerp(left, right, 1f / 3f);
                float t2 = Mathf.Lerp(left, right, 2f / 3f);
                float d1 = Bosluk(Karistir(a0, a1, t1), Karistir(b0, b1, t1));
                float d2 = Bosluk(Karistir(a0, a1, t2), Karistir(b0, b1, t2));
                if (d1 <= d2) right = t2; else left = t1;
            }
            Degerlendir(a0, a1, b0, b1, (left + right) * 0.5f, threshold, ref near, ref contact);
            if (contact) return;
        }
    }

    private static void Degerlendir(Sekil a0, Sekil a1, Sekil b0, Sekil b1, float t, float threshold, ref bool near, ref bool contact)
    {
        Sekil a = Karistir(a0, a1, t);
        Sekil b = Karistir(b0, b1, t);
        float gap = Bosluk(a, b);
        if (gap <= ContactEpsilon) { contact = true; return; }
        Bounds ba = a.Sinirlar;
        Bounds bb = b.Sinirlar;
        // Only a side pass while the longitudinal and vertical ranges overlap counts.
        bool sidePass = ba.min.x <= bb.max.x && ba.max.x >= bb.min.x && ba.min.y < bb.max.y && ba.max.y > bb.min.y;
        if (sidePass && gap <= threshold) near = true;
    }

    private static Sekil Karistir(Sekil a, Sekil b, float t)
    {
        a.Merkez = Vector3.Lerp(a.Merkez, b.Merkez, t);
        a.Donus = Quaternion.Slerp(a.Donus, b.Donus, t);
        a.YariBoyut = Vector3.Lerp(a.YariBoyut, b.YariBoyut, t);
        a.Yaricap = Mathf.Lerp(a.Yaricap, b.Yaricap, t);
        return a;
    }

    private static bool DunyaEksenlerineParalel(Sekil shape)
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 axis = Abs(shape.Eksen(i));
            if (Mathf.Max(axis.x, Mathf.Max(axis.y, axis.z)) < 0.999999f) return false;
        }
        return true;
    }

    private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    private static Vector3 EnYakinNokta(Sekil box, Vector3 point)
    {
        Vector3 local = Quaternion.Inverse(box.Donus) * (point - box.Merkez);
        local.x = Mathf.Clamp(local.x, -box.YariBoyut.x, box.YariBoyut.x);
        local.y = Mathf.Clamp(local.y, -box.YariBoyut.y, box.YariBoyut.y);
        local.z = Mathf.Clamp(local.z, -box.YariBoyut.z, box.YariBoyut.z);
        return box.Merkez + box.Donus * local;
    }

    private static Vector3 Kose(Sekil box, int index)
    {
        return box.Merkez + box.Donus * new Vector3((index & 1) == 0 ? -box.YariBoyut.x : box.YariBoyut.x,
            (index & 2) == 0 ? -box.YariBoyut.y : box.YariBoyut.y, (index & 4) == 0 ? -box.YariBoyut.z : box.YariBoyut.z);
    }

    private static bool KutularOrtusuyor(Sekil a, Sekil b)
    {
        for (int i = 0; i < 3; i++)
        {
            if (Ayri(a, b, a.Eksen(i)) || Ayri(a, b, b.Eksen(i))) return false;
            for (int j = 0; j < 3; j++) if (Ayri(a, b, Vector3.Cross(a.Eksen(i), b.Eksen(j)))) return false;
        }
        return true;
    }

    private static bool Ayri(Sekil a, Sekil b, Vector3 axis)
    {
        if (axis.sqrMagnitude < 0.000001f) return false;
        float ra = Mathf.Abs(Vector3.Dot(axis, a.Eksen(0))) * a.YariBoyut.x + Mathf.Abs(Vector3.Dot(axis, a.Eksen(1))) * a.YariBoyut.y + Mathf.Abs(Vector3.Dot(axis, a.Eksen(2))) * a.YariBoyut.z;
        float rb = Mathf.Abs(Vector3.Dot(axis, b.Eksen(0))) * b.YariBoyut.x + Mathf.Abs(Vector3.Dot(axis, b.Eksen(1))) * b.YariBoyut.y + Mathf.Abs(Vector3.Dot(axis, b.Eksen(2))) * b.YariBoyut.z;
        return Mathf.Abs(Vector3.Dot(b.Merkez - a.Merkez, axis)) > ra + rb;
    }

    private static float SegmentBoslukKaresi(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
    {
        Vector3 d1 = q1 - p1;
        Vector3 d2 = q2 - p2;
        Vector3 r = p1 - p2;
        float a = Vector3.Dot(d1, d1);
        float e = Vector3.Dot(d2, d2);
        float f = Vector3.Dot(d2, r);
        float s, t;
        if (a <= 0.0000001f && e <= 0.0000001f) return r.sqrMagnitude;
        if (a <= 0.0000001f) { s = 0f; t = Mathf.Clamp01(f / e); }
        else
        {
            float c = Vector3.Dot(d1, r);
            if (e <= 0.0000001f) { t = 0f; s = Mathf.Clamp01(-c / a); }
            else
            {
                float b = Vector3.Dot(d1, d2);
                float denominator = a * e - b * b;
                s = denominator > 0.0000001f ? Mathf.Clamp01((b * f - c * e) / denominator) : 0f;
                t = (b * s + f) / e;
                if (t < 0f) { t = 0f; s = Mathf.Clamp01(-c / a); }
                else if (t > 1f) { t = 1f; s = Mathf.Clamp01((b - c) / a); }
            }
        }
        return ((p1 + d1 * s) - (p2 + d2 * t)).sqrMagnitude;
    }
}
