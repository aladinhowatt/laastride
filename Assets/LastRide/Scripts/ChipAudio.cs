using UnityEngine;

/// <summary>Procedural 8-bit sound: engine, horn, text blips and a soft pentatonic loop. No audio assets needed.</summary>
public static class ChipAudio
{
    const int Rate = 22050;

    static AudioClip engine, honk, blip, select, confirm, music;

    static float Square(double t, double freq, double duty = 0.5)
    {
        double ph = (t * freq) % 1.0;
        return ph < duty ? 1f : -1f;
    }

    static float Tri(double t, double freq)
    {
        double ph = (t * freq) % 1.0;
        return (float)(ph < 0.5 ? ph * 4 - 1 : 3 - ph * 4);
    }

    static AudioClip Make(string name, float seconds, System.Func<double, float> gen, bool loop = false)
    {
        int n = Mathf.CeilToInt(seconds * Rate);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(gen(i / (double)Rate), -1f, 1f);
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip Engine()
    {
        if (engine) return engine;
        var rnd = new System.Random(3);
        engine = Make("engine", 0.3f, t =>
        {
            // low putt-putt: a pulse every 0.075 s
            double ph = (t % 0.075) / 0.075;
            float env = (float)Mathf.Exp(-(float)ph * 6f);
            return (Square(t, 58, 0.35) * 0.55f + (float)(rnd.NextDouble() * 2 - 1) * 0.25f) * env * 0.5f;
        }, true);
        return engine;
    }

    public static AudioClip Honk()
    {
        if (honk) return honk;
        honk = Make("honk", 0.38f, t =>
        {
            float env = t < 0.02 ? (float)(t / 0.02) : Mathf.Exp(-(float)(t - 0.02) * 4f);
            return (Square(t, 392, 0.45) * 0.5f + Square(t, 330, 0.45) * 0.5f) * env * 0.35f;
        });
        return honk;
    }

    public static AudioClip Blip(float pitch)
    {
        // short text blip; pitch variants are produced by AudioSource.pitch
        if (blip) return blip;
        blip = Make("blip", 0.05f, t =>
        {
            float env = 1f - (float)(t / 0.05);
            return Square(t, 660, 0.5) * env * 0.25f;
        });
        return blip;
    }

    public static AudioClip Select()
    {
        if (select) return select;
        select = Make("select", 0.07f, t => Square(t, 880, 0.25) * (1f - (float)(t / 0.07)) * 0.25f);
        return select;
    }

    public static AudioClip Confirm()
    {
        if (confirm) return confirm;
        confirm = Make("confirm", 0.22f, t =>
        {
            double f = t < 0.07 ? 523 : t < 0.14 ? 659 : 784;
            float env = 1f - (float)(t / 0.22);
            return Square(t, f, 0.25) * env * 0.28f;
        });
        return confirm;
    }

    /// <summary>A slow, calm loop on a Thai-flavoured pentatonic scale (C D E G A).</summary>
    public static AudioClip Music()
    {
        if (music) return music;
        double[] scale = { 261.63, 293.66, 329.63, 392.00, 440.00 };
        // melody as scale steps (-1 = rest, +5 = octave up)
        int[] mel = { 2, -1, 4, 3, 2, -1, 0, 1, 2, -1, 3, 4, 7, 6, 4, -1,
                      3, -1, 2, 1, 0, -1, 1, 2, 3, 4, 3, -1, 1, -1, 0, -1 };
        int[] bass = { 0, -1, -1, -1, 0, -1, -1, -1, 3, -1, -1, -1, 3, -1, -1, -1,
                       2, -1, -1, -1, 2, -1, -1, -1, 1, -1, -1, -1, 0, -1, -1, -1 };
        double step = 60.0 / 74.0 / 2.0;           // eighth notes at 74 bpm
        float seconds = (float)(step * mel.Length);
        music = Make("music", seconds, t =>
        {
            int idx = Mathf.Min(mel.Length - 1, (int)(t / step));
            double local = t - idx * step;
            float v = 0f;
            if (mel[idx] >= 0)
            {
                int oct = mel[idx] / 5, deg = mel[idx] % 5;
                double f = scale[deg] * Mathf.Pow(2, oct) * 2.0;       // pluck, ranat-like
                float env = Mathf.Exp(-(float)local * 5.5f);
                v += Square(t, f, 0.25) * env * 0.11f;
            }
            if (bass[idx] >= 0)
            {
                double f = scale[bass[idx]] * 0.5;
                float env = Mathf.Exp(-(float)local * 2.2f);
                v += Tri(t, f) * env * 0.16f;
            }
            return v;
        }, true);
        return music;
    }
}
