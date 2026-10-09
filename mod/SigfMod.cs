// R.E.P.O. Takeover: the playground becomes a R.E.P.O. job. Fragile valuables lie around, the bird's board kicks them
// toward a green EXTRACTION POINT to bank the cash, breaking them costs money, and grumpy robot Gnomes hunt the bird
// (the board can smash them). A little R.E.P.O. drone follows the bird.
using System.Collections;
using System.Collections.Generic;
using Sigf.Kit;
using UnityEngine;

public class SigfMod : MixMod
{
    public static readonly Color Green = new Color(0.25f, 1f, 0.35f);
    public static readonly Color Gold = new Color(1f, 0.8f, 0.2f);
    public static readonly Color Red = new Color(1f, 0.25f, 0.2f);
    public const int Quota = 20000;

    public static int Haul, Round = 1;
    public static Vector3 PadPos;
    public static Vector3 Fwd, Right;
    public static Vector3 Origin;
    public static readonly List<Valuable> Items = new List<Valuable>();
    public static readonly List<Gnome> Gnomes = new List<Gnome>();
    static GameObject drone, droneFace;
    static Material alphaMat;
    static bool quotaShown;

    public override void OnLoad() => G.StartLevel = "playground";

    // ---------- helpers ----------
    public static Material FaceMat(string tex)
    {
        var sh = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Standard");
        var m = new Material(sh);
        m.mainTexture = Mix.Texture(tex);
        return m;
    }

    public static Vector3 Floor(Vector3 p)
    {
        if (Physics.Raycast(p + Vector3.up * 1.5f, Vector3.down, out var h, 6f, ~0, QueryTriggerInteraction.Ignore)) return h.point;
        return p;
    }

    public static GameObject Billboard(string tex, Transform parent, float size)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(q.GetComponent<Collider>());
        q.name = "SigfFace";
        q.GetComponent<Renderer>().material = FaceMat(tex);
        q.transform.SetParent(parent, false);
        q.transform.localScale = Vector3.one * size;
        q.AddComponent<FaceCam>();
        return q;
    }

    public static void Popup(Vector3 pos, string text, Color c, float size = 0.025f)
    {
        var go = new GameObject("SigfPopup");
        go.transform.position = pos;
        var tm = go.AddComponent<TextMesh>();
        tm.text = text; tm.color = c; tm.fontSize = 80; tm.characterSize = size;
        tm.anchor = TextAnchor.MiddleCenter; tm.fontStyle = FontStyle.Bold;
        tm.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        go.GetComponent<MeshRenderer>().material = tm.font.material;
        go.AddComponent<FaceCam>();
        go.AddComponent<Rise>();
        Object.Destroy(go, 1.6f);
    }

    public static void Bank(int amount, Vector3 at)
    {
        Haul += amount;
        Mix.Play(Mix.Sound("cash.wav"), at, 1f, Random.Range(0.95f, 1.1f));
        Mix.Burst(at + Vector3.up * 0.1f, Gold, 18, 2.5f, 0.03f, 1.2f);
        Popup(at + Vector3.up * 0.5f, "+$" + amount.ToString("N0"), Green, 0.03f);
    }

    // ---------- the job ----------
    static void SpawnRound()
    {
        foreach (var v in Items) if (v != null) Object.Destroy(v.gameObject);
        Items.Clear();
        float[] fw = { 1.4f, 1.8f, 2.4f, 2.8f, 3.3f, 1.6f, 2.2f, 3.0f };
        float[] sd = { -1.2f, 1.1f, -0.2f, 1.7f, -1.8f, 0.4f, -0.9f, 0.4f };
        for (int i = 0; i < fw.Length; i++)
        {
            var p = Floor(Origin + Fwd * fw[i] + Right * sd[i]);
            Items.Add(Valuable.Make(i % 4, p));
        }
    }

    static void BuildPad()
    {
        PadPos = Floor(Origin + Fwd * 5.2f + Right * 0.3f);
        var pad = Mix.Shape(PrimitiveType.Cylinder, PadPos + Vector3.up * 0.015f, new Vector3(1.6f, 0.015f, 1.6f), Green, false, "SigfPad");
        Mix.Paint(pad, Green, 2f);
        var beam = Mix.Shape(PrimitiveType.Cylinder, PadPos + Vector3.up * 1.2f, new Vector3(0.05f, 1.2f, 0.05f), Green, false, "SigfBeam");
        Mix.Paint(beam, Green, 3f);
        Mix.Glow(PadPos + Vector3.up * 0.4f, Green, 4f, 2.5f);
        var keep = new GameObject("SigfPadLabel");
        keep.transform.position = PadPos + Vector3.up * 2.6f;
        var tm = keep.AddComponent<TextMesh>();
        tm.text = "EXTRACTION POINT"; tm.color = Green; tm.fontSize = 80; tm.characterSize = 0.04f;
        tm.anchor = TextAnchor.MiddleCenter; tm.fontStyle = FontStyle.Bold;
        tm.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        keep.GetComponent<MeshRenderer>().material = tm.font.material;
        keep.AddComponent<FaceCam>();
    }

    public override void OnReady()
    {
        Origin = G.Pos;
        Fwd = G.Forward;
        Right = Vector3.Cross(Vector3.up, Fwd).normalized;
        Mix.Log("origin " + Origin + " fwd " + Fwd);
        Mix.Say("R.E.P.O. TAKEOVER", 3f, Gold, 0.18f, 72);
        Mix.Say("Kick the valuables to the green zone!", 4f, Color.white, 0.28f, 36);
        BuildPad();
        SpawnRound();
        SpawnGnome(Origin + Right * 3f + Fwd * 2.5f);
        SpawnGnome(Origin - Right * 3f + Fwd * 3.5f);
        drone = new GameObject("SigfDrone");
        droneFace = Billboard("face.png", drone.transform, 0.45f);
        Mix.Glow(Vector3.zero, Color.white, 2f, 1f, drone.transform);

        G.OnTrick(t =>
        {
            foreach (var v in Items)
                if (v != null && (v.transform.position - G.Pos).sqrMagnitude < 9f)
                {
                    v.GetComponent<Rigidbody>().AddForce(Vector3.up * 3f + (PadPos - v.transform.position).normalized * 1.5f, ForceMode.VelocityChange);
                    v.Cur = Mathf.RoundToInt(v.Cur * 1.15f);
                    Popup(v.transform.position + Vector3.up * 0.4f, "TRICK BONUS", Gold, 0.02f);
                }
            Mix.Burst(G.Pos + Vector3.up * 0.3f, Gold, 14, 3f, 0.04f, 1.2f);
        });
        Mix.Every(0.4f, Hud, "hud");
        Mix.Every(2f, () => { if (G.Bailed) G.GetUp(); }, "getup");
    }

    static void SpawnGnome(Vector3 at)
    {
        var g = Gnome.Make(Floor(at));
        Gnomes.Add(g);
        Mix.Play(Mix.Sound("giggle.wav"), g.transform.position, 1f, Random.Range(0.9f, 1.15f));
    }

    static void Hud()
    {
        Mix.Say("HAUL  $" + Haul.ToString("N0") + " / $" + Quota.ToString("N0") + "   ROUND " + Round, 0.6f, Green, 0.07f, 38);
    }

    public override void OnUpdate()
    {
        if (drone != null && G.Body != null)
        {
            var t = G.Pos + Vector3.up * (0.7f + Mathf.Sin(Time.time * 3f) * 0.06f) + Right * -0.35f;
            drone.transform.position = Vector3.Lerp(drone.transform.position, t, Time.deltaTime * 6f);
        }
        for (int i = Items.Count - 1; i >= 0; i--)
        {
            var v = Items[i];
            if (v == null) { Items.RemoveAt(i); continue; }
            var d = v.transform.position - PadPos; d.y = 0;
            if (d.magnitude < 0.8f)
            {
                Bank(v.Cur, v.transform.position);
                Items.RemoveAt(i);
                Object.Destroy(v.gameObject);
            }
        }
        if (Haul >= Quota && !quotaShown)
        {
            quotaShown = true;
            Mix.Say("QUOTA REACHED!", 4f, Gold, 0.3f, 80);
            Mix.Burst(G.Pos + Vector3.up, Gold, 60, 5f, 0.06f, 2.5f);
            G.Screm();
            Mix.After(5f, () => { Round++; Haul = 0; quotaShown = false; SpawnRound(); }, "newround");
        }
        if (Items.Count == 0 && !quotaShown) SpawnRound();
    }

    // ---------- demo ----------
    static void Aim(Vector3 target)
    {
        var d = target - G.Pos; d.y = 0;
        if (d.sqrMagnitude < 0.01f) return;
        G.Teleport(G.Pos, Quaternion.LookRotation(d.normalized));
    }

    static Valuable Nearest()
    {
        Valuable best = null; float bd = 1e9f;
        foreach (var v in Items)
        {
            if (v == null) continue;
            float d = (v.transform.position - G.Pos).sqrMagnitude;
            if (d < bd) { bd = d; best = v; }
        }
        return best;
    }

    public override IEnumerator Demo()
    {
        yield return Mix.Wait(1.5f);
        // moment 1: kick valuables to the extraction point
        for (int i = 0; i < 4; i++)
        {
            var v = Nearest();
            if (v == null) break;
            var to = v.transform.position - G.Pos; to.y = 0;
            // line up so the kick sends it toward the pad
            var dir = (PadPos - v.transform.position); dir.y = 0;
            Aim(v.transform.position - dir.normalized * 0.3f);
            G.Boost(4f);
            yield return Mix.Wait(1.3f);
            if (i == 1) { G.Launch(5f); G.Flip("Kickflip"); yield return Mix.Wait(0.8f); }
        }
        // moment 3: smash a Gnome
        Mix.Say("GNOME ATTACK!", 2f, Red, 0.3f, 60);
        for (int i = 0; i < 4; i++)
        {
            Gnome g = Gnomes.Count > 0 ? Gnomes[0] : null;
            if (g != null) { Aim(g.transform.position); G.Boost(5f); }
            yield return Mix.Wait(1.2f);
        }
        G.Launch(5f); G.Screm();
        yield return Mix.Wait(3f);
    }
}

// ---------- components ----------
public class FaceCam : MonoBehaviour
{
    void LateUpdate()
    {
        var c = Camera.main;
        if (c != null) transform.rotation = Quaternion.LookRotation(transform.position - c.transform.position);
    }
}

public class Rise : MonoBehaviour
{
    void Update() { transform.position += Vector3.up * 0.5f * Time.deltaTime; }
}

public class Valuable : MonoBehaviour
{
    public int Cur, Max;
    float lastHit;

    public static Valuable Make(int kind, Vector3 pos)
    {
        var root = new GameObject("SigfValuable");
        root.transform.position = pos + Vector3.up * 0.3f;
        var goldTex = Mix.Texture("gold.png");
        int value = 1500;
        float h = 0.3f;
        switch (kind)
        {
            case 0: // golden vase
            {
                var a = Mix.Shape(PrimitiveType.Sphere, Vector3.zero, new Vector3(0.3f, 0.3f, 0.3f), Color.white, true, "vase");
                Mix.Paint(a, Color.white, 0f, goldTex);
                var b = Mix.Shape(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.14f, 0.1f, 0.14f), Color.white, false, "neck");
                Mix.Paint(b, Color.white, 0f, goldTex);
                a.transform.SetParent(root.transform, false);
                b.transform.SetParent(root.transform, false);
                b.transform.localPosition = new Vector3(0, 0.2f, 0);
                value = 3200; h = 0.4f;
                break;
            }
            case 1: // painting
            {
                var a = Mix.Shape(PrimitiveType.Cube, Vector3.zero, new Vector3(0.5f, 0.5f, 0.05f), Color.white, true, "painting");
                Mix.Paint(a, Color.white, 0f, Mix.Texture("painting.png"));
                a.transform.SetParent(root.transform, false);
                value = 5200; h = 0.5f;
                break;
            }
            case 2: // gold bars
            {
                for (int i = 0; i < 3; i++)
                {
                    var a = Mix.Shape(PrimitiveType.Cube, Vector3.zero, new Vector3(0.28f, 0.1f, 0.14f), Color.white, i == 0, "bar");
                    Mix.Paint(a, Gold(), 0.3f, goldTex);
                    a.transform.SetParent(root.transform, false);
                    a.transform.localPosition = new Vector3(i == 2 ? 0.02f : (i == 0 ? -0.07f : 0.07f), i == 2 ? 0.1f : 0f, 0);
                }
                value = 4100; h = 0.25f;
                break;
            }
            default: // lucky gold nugget-ball ("Mr. Moneybag")
            {
                var a = Mix.Shape(PrimitiveType.Sphere, Vector3.zero, new Vector3(0.38f, 0.38f, 0.38f), Color.white, true, "bag");
                Mix.Paint(a, new Color(0.9f, 0.7f, 0.25f), 0.2f, goldTex);
                a.transform.SetParent(root.transform, false);
                var f = SigfMod.Billboard("face.png", root.transform, 0.3f);
                f.transform.localPosition = new Vector3(0, 0, 0);
                value = 2400; h = 0.4f;
                break;
            }
        }
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 0.4f;
        rb.drag = 0.4f; rb.angularDrag = 0.5f;
        var v = root.AddComponent<Valuable>();
        v.Cur = v.Max = value;
        // each part's collider reports to the root's Rigidbody
        Mix.Glow(Vector3.zero, new Color(1f, 0.9f, 0.4f), 1.4f, 0.8f, root.transform);
        root.transform.position = pos + Vector3.up * h;
        return v;
    }

    static Color Gold() => new Color(1f, 0.85f, 0.4f);

    void OnCollisionEnter(Collision c)
    {
        if (Time.time - lastHit < 0.15f) return;
        float sp = c.relativeVelocity.magnitude;
        bool board = G.BoardObject != null && c.transform.root == G.BoardObject.transform.root;
        if (board)
        {
            lastHit = Time.time;
            var rb = GetComponent<Rigidbody>();
            var to = SigfMod.PadPos - transform.position; to.y = 0;
            rb.velocity = to.normalized * (2f + Mathf.Min(sp, 5f) * 0.5f) + Vector3.up * 2.5f;
            rb.angularVelocity = Random.insideUnitSphere * 6f;
            Mix.Burst(c.contacts.Length > 0 ? c.contacts[0].point : transform.position, SigfMod.Gold, 6, 2f, 0.03f, 0.8f);
            return;
        }
        if (sp > 3.5f)
        {
            lastHit = Time.time;
            int loss = Mathf.Min(Cur, Mathf.RoundToInt(Max * 0.12f * (sp - 2f)));
            Cur -= loss;
            SigfMod.Popup(transform.position + Vector3.up * 0.4f, "-$" + loss.ToString("N0"), SigfMod.Red, 0.025f);
            Mix.Burst(transform.position, new Color(1f, 0.85f, 0.4f), 8, 2f, 0.04f, 1f);
            Mix.Play(Mix.Sound("smash.wav"), transform.position, 0.5f, Random.Range(1.2f, 1.5f));
            if (Cur <= 0)
            {
                Mix.Burst(transform.position, SigfMod.Gold, 30, 4f, 0.05f, 1.5f);
                SigfMod.Items.Remove(this);
                Destroy(gameObject);
            }
        }
    }
}

public class Gnome : MonoBehaviour
{
    public float Hp = 30f;
    float lastHit, lastLaugh, lastDash;
    Rigidbody rb;
    readonly List<Renderer> rends = new List<Renderer>();

    public static Gnome Make(Vector3 pos)
    {
        var root = new GameObject("SigfGnome");
        root.transform.position = pos + Vector3.up * 0.3f;
        var body = Mix.Shape(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.28f, 0.2f, 0.28f), new Color(0.2f, 0.55f, 0.25f), true, "gbody");
        body.transform.SetParent(root.transform, false);
        var hat = Mix.Shape(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.1f, 0.05f, 0.1f), Color.red, false, "ghat");
        hat.transform.SetParent(root.transform, false);
        hat.transform.localPosition = new Vector3(0, 0.6f, 0);
        var head = SigfMod.Billboard("face.png", root.transform, 0.42f);
        head.transform.localPosition = new Vector3(0, 0.38f, 0);
        var g = root.AddComponent<Gnome>();
        g.rb = root.AddComponent<Rigidbody>();
        g.rb.mass = 0.8f; g.rb.drag = 1.5f;
        g.rb.constraints = RigidbodyConstraints.FreezeRotation;
        foreach (var r in root.GetComponentsInChildren<Renderer>()) g.rends.Add(r);
        return g;
    }

    void FixedUpdate()
    {
        if (G.Body == null) return;
        var d = G.Pos - transform.position; d.y = 0;
        if (d.magnitude > 1.6f) { if (rb.velocity.magnitude < 1.3f) rb.AddForce(d.normalized * 6f); }
        else if (Time.time - lastDash > 3.5f) { lastDash = Time.time; rb.velocity = d.normalized * 2.8f + Vector3.up * 1.2f; }
        if (Time.time - lastLaugh > 4f && d.magnitude < 2.5f)
        {
            lastLaugh = Time.time;
            Mix.Play(Mix.Sound("giggle.wav"), transform.position, 0.8f, Random.Range(0.9f, 1.2f));
        }
    }

    void OnCollisionEnter(Collision c)
    {
        if (Time.time - lastHit < 0.3f) return;
        float sp = c.relativeVelocity.magnitude;
        bool board = G.BoardObject != null && c.transform.root == G.BoardObject.transform.root;
        if (!board || sp < 1.2f) return;
        lastHit = Time.time;
        Hp -= 10f + sp * 5f;
        var dir = (transform.position - G.Pos); dir.y = 0;
        rb.velocity = dir.normalized * (3f + sp * 0.4f) + Vector3.up * 2f;
        Mix.Burst(transform.position + Vector3.up * 0.3f, Color.white, 10, 3f, 0.04f, 0.6f);
        Mix.Play(Mix.Sound("smash.wav"), transform.position, 0.6f, Random.Range(1.6f, 2f));
        SigfMod.Popup(transform.position + Vector3.up * 0.9f, "BONK!", Color.yellow, 0.03f);
        StartCoroutine(Flash());
        if (Hp <= 0f)
        {
            Mix.Burst(transform.position + Vector3.up * 0.3f, SigfMod.Red, 40, 4f, 0.06f, 1.5f);
            Mix.Burst(transform.position + Vector3.up * 0.3f, SigfMod.Gold, 20, 4f, 0.04f, 1.5f);
            SigfMod.Bank(800, transform.position);
            SigfMod.Gnomes.Remove(this);
            var p = G.Pos - SigfMod.Right * Random.Range(-3f, 3f) + SigfMod.Fwd * 3f;
            Mix.After(6f, () => SigfMod.Gnomes.Add(Gnome.Make(SigfMod.Floor(p))), "gnomeRespawn");
            Destroy(gameObject);
        }
    }

    IEnumerator Flash()
    {
        var old = new List<Color>();
        foreach (var r in rends) { old.Add(r != null ? r.material.color : Color.white); if (r != null) r.material.color = Color.white; }
        yield return new WaitForSeconds(0.12f);
        for (int i = 0; i < rends.Count; i++) if (rends[i] != null) rends[i].material.color = old[i];
    }
}
