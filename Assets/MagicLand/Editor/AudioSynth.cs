using System.IO;
using UnityEditor;
using UnityEngine;

namespace MagicLand.EditorTools
{
    /// <summary>
    /// Synthesizes the nature sounds (seamless wind loop and a few bird calls) into WAV assets,
    /// so the project does not depend on downloaded audio.
    /// </summary>
    public static class AudioSynth
    {
        const int Rate = 44100;

        public static AudioClip Wind(string folder)
        {
            const float loopSeconds = 24f, fadeSeconds = 2f, warmupSeconds = 1f;
            int n = (int)(loopSeconds * Rate), fade = (int)(fadeSeconds * Rate), warm = (int)(warmupSeconds * Rate);
            var raw = new float[2][];
            var rnd = new System.Random(3);

            for (int ch = 0; ch < 2; ch++)
            {
                raw[ch] = new float[n + fade];
                float low1 = 0f, low2 = 0f, bandLow = 0f, bandHigh = 0f;
                float aLow = OnePole(200f), aBandLow = OnePole(450f), aBandHigh = OnePole(1800f);
                float phase = ch * 0.9f;

                for (int i = -warm; i < n + fade; i++)
                {
                    float white = (float)rnd.NextDouble() * 2f - 1f;
                    low1 += aLow * (white - low1);
                    low2 += aLow * (low1 - low2);
                    bandHigh += aBandHigh * (white - bandHigh);
                    bandLow += aBandLow * (white - bandLow);
                    if (i < 0)
                        continue;

                    // Gust envelope built from whole cycles over the loop length so it wraps seamlessly.
                    float t = (i % n) / (float)Rate;
                    float w = Mathf.PI * 2f / loopSeconds;
                    float gust = 0.55f + 0.25f * Mathf.Sin(w * t + phase) + 0.13f * Mathf.Sin(3f * w * t + 1.7f + phase)
                                 + 0.07f * Mathf.Sin(7f * w * t + 0.4f);
                    gust = Mathf.Max(0.1f, gust);
                    float flutter = 0.65f + 0.2f * Mathf.Sin(2f * Mathf.PI * 4f * t + phase) + 0.15f * Mathf.Sin(2f * Mathf.PI * 2.75f * t);

                    float rumble = low2 * 8f;
                    float leaves = (bandHigh - bandLow) * 0.3f * flutter;
                    raw[ch][i] = rumble * gust + leaves * gust * gust;
                }
            }

            var stereo = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                for (int ch = 0; ch < 2; ch++)
                {
                    float s = raw[ch][i];
                    if (i < fade)
                    {
                        // Equal-power crossfade with the tail so the end flows into the start.
                        float x = i / (float)fade;
                        s = s * Mathf.Sin(x * Mathf.PI * 0.5f) + raw[ch][n + i] * Mathf.Cos(x * Mathf.PI * 0.5f);
                    }
                    stereo[i * 2 + ch] = s;
                }
            }

            Normalize(stereo, 0.5f);
            return Save(folder + "/Wind.wav", stereo, 2, true);
        }

        public static AudioClip[] Birds(string folder)
        {
            return new[]
            {
                Save(folder + "/Bird Tweet.wav", Echo(Tweet()), 1, false),
                Save(folder + "/Bird Trill.wav", Echo(Trill()), 1, false),
                Save(folder + "/Bird Song.wav", Echo(Song()), 1, false),
            };
        }

        // Bird calls are kept low-pitched, pure and slow so they stay soothing.
        static float[] Tweet()
        {
            var data = new float[(int)(1.1f * Rate)];
            float phase = 0f;
            for (int k = 0; k < 2; k++)
            {
                int start = (int)((0.05f + k * 0.35f) * Rate);
                int len = (int)(0.2f * Rate);
                for (int i = 0; i < len; i++)
                {
                    float x = i / (float)len;
                    float f = Mathf.Lerp(2700f, 2200f, x);
                    phase += 2f * Mathf.PI * f / Rate;
                    float env = Mathf.Pow(Mathf.Sin(Mathf.PI * x), 3f);
                    data[start + i] += Mathf.Sin(phase) * env;
                }
            }
            return data;
        }

        static float[] Trill()
        {
            int len = (int)(1.1f * Rate);
            var data = new float[len + (int)(0.1f * Rate)];
            float phase = 0f;
            for (int i = 0; i < len; i++)
            {
                float x = i / (float)len;
                float t = i / (float)Rate;
                float f = 2300f + 120f * Mathf.Sin(2f * Mathf.PI * 9f * t) - 200f * x;
                phase += 2f * Mathf.PI * f / Rate;
                float pulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 9f * t);
                float env = Mathf.Pow(Mathf.Sin(Mathf.PI * x), 2f) * (0.4f + 0.6f * pulse * pulse);
                data[i] = Mathf.Sin(phase) * env;
            }
            return data;
        }

        static float[] Song()
        {
            float[] notes = { 1900f, 2250f, 2100f, 2450f, 2200f };
            const float noteLen = 0.2f, gap = 0.09f;
            var data = new float[(int)((notes.Length * (noteLen + gap) + 0.1f) * Rate)];
            float phase = 0f;
            for (int k = 0; k < notes.Length; k++)
            {
                int start = (int)(k * (noteLen + gap) * Rate);
                int len = (int)(noteLen * Rate);
                for (int i = 0; i < len; i++)
                {
                    float x = i / (float)len;
                    float f = notes[k] * (1f + 0.03f * Mathf.Sin(Mathf.PI * x));
                    phase += 2f * Mathf.PI * f / Rate;
                    float env = Mathf.Pow(Mathf.Sin(Mathf.PI * x), 2f);
                    data[start + i] = Mathf.Sin(phase) * env;
                }
            }
            return data;
        }

        /// <summary>Soft, mostly low-frequency water rush, as a seamless mono loop.</summary>
        public static AudioClip Waterfall(string folder)
        {
            const float loopSeconds = 12f, fadeSeconds = 1.5f;
            int n = (int)(loopSeconds * Rate), fade = (int)(fadeSeconds * Rate), warm = Rate;
            var raw = new float[n + fade];
            var rnd = new System.Random(8);
            float aLow = OnePole(500f), aMid = OnePole(1400f), aHigh = OnePole(2600f);
            float low1 = 0f, low2 = 0f, mid = 0f, high = 0f;

            for (int i = -warm; i < n + fade; i++)
            {
                float white = (float)rnd.NextDouble() * 2f - 1f;
                low1 += aLow * (white - low1);
                low2 += aLow * (low1 - low2);
                mid += aMid * (white - mid);
                high += aHigh * (white - high);
                if (i < 0)
                    continue;
                float t = (i % n) / (float)Rate;
                float w = Mathf.PI * 2f / loopSeconds;
                // Gentle babbling: the brighter band breathes slowly.
                float babble = 0.6f + 0.25f * Mathf.Sin(w * 5f * t) + 0.15f * Mathf.Sin(w * 13f * t + 1.3f);
                raw[i] = low2 * 3.5f + (high - mid) * 0.35f * babble + (mid - low1) * 0.5f;
            }

            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float s = raw[i];
                if (i < fade)
                {
                    float x = i / (float)fade;
                    s = s * Mathf.Sin(x * Mathf.PI * 0.5f) + raw[n + i] * Mathf.Cos(x * Mathf.PI * 0.5f);
                }
                data[i] = s;
            }
            Normalize(data, 0.5f);
            return Save(folder + "/Waterfall.wav", data, 1, true);
        }

        /// <summary>A short, soft pair of low frog croaks for the pond.</summary>
        public static AudioClip FrogCall(string folder)
        {
            const float seconds = 0.9f;
            var data = new float[(int)(seconds * Rate)];
            float phase = 0f;
            for (int croak = 0; croak < 2; croak++)
            {
                int start = (int)((0.08f + croak * 0.36f) * Rate);
                int length = (int)(0.27f * Rate);
                for (int i = 0; i < length; i++)
                {
                    float x = i / (float)length;
                    float frequency = Mathf.Lerp(480f, 350f, x) + Mathf.Sin(x * Mathf.PI * 4f) * 24f;
                    phase += 2f * Mathf.PI * frequency / Rate;
                    float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * x), 1.8f);
                    float pulses = 0.75f + 0.25f * Mathf.Sin(x * Mathf.PI * 14f);
                    data[start + i] += Mathf.Sin(phase) * envelope * pulses;
                }
            }
            Normalize(data, 0.32f);
            return Save(folder + "/Frog Call.wav", data, 1, false);
        }

        /// <summary>Seamless loop of soft crackles over a warm low hum, like embers in a fireplace.</summary>
        public static AudioClip Crackle(string folder)
        {
            const float loopSeconds = 16f, fadeSeconds = 1.5f;
            int n = (int)(loopSeconds * Rate), fade = (int)(fadeSeconds * Rate);
            var stereo = new float[n * 2];
            var rnd = new System.Random(23);

            for (int ch = 0; ch < 2; ch++)
            {
                // Warm hum: doubly low-passed noise, cross-faded with its own tail to loop cleanly.
                var hum = new float[n + fade];
                float low1 = 0f, low2 = 0f, aLow = OnePole(180f);
                for (int i = -Rate; i < n + fade; i++)
                {
                    float white = (float)rnd.NextDouble() * 2f - 1f;
                    low1 += aLow * (white - low1);
                    low2 += aLow * (low1 - low2);
                    if (i >= 0)
                        hum[i] = low2 * 2.2f;
                }
                for (int i = 0; i < n; i++)
                {
                    float s = hum[i];
                    if (i < fade)
                    {
                        float x = i / (float)fade;
                        s = s * Mathf.Sin(x * Mathf.PI * 0.5f) + hum[n + i] * Mathf.Cos(x * Mathf.PI * 0.5f);
                    }
                    stereo[i * 2 + ch] = s;
                }

                // Tiny clicks, mostly quiet, sometimes in a quick little cluster; wrapped around the loop.
                float t = 0f;
                while (true)
                {
                    t += -Mathf.Log(1f - (float)rnd.NextDouble() * 0.999f) / 5f;
                    if (t >= loopSeconds)
                        break;
                    int burst = rnd.NextDouble() < 0.12 ? 3 + rnd.Next(4) : 1;
                    for (int b = 0; b < burst; b++)
                    {
                        int start = (int)((t + b * (0.015f + 0.03f * (float)rnd.NextDouble())) * Rate);
                        float amplitude = 0.08f + 0.6f * Mathf.Pow((float)rnd.NextDouble(), 3f);
                        int length = (int)((0.002f + 0.01f * (float)rnd.NextDouble()) * Rate);
                        float previous = 0f, smooth = 0f, aClick = OnePole(2500f + 3000f * (float)rnd.NextDouble());
                        for (int i = 0; i < length; i++)
                        {
                            float white = (float)rnd.NextDouble() * 2f - 1f;
                            float high = white - previous;
                            previous = white;
                            smooth += aClick * (high - smooth);
                            float envelope = Mathf.Exp(-6f * i / length);
                            int k = (start + i) % n;
                            stereo[k * 2 + ch] += smooth * envelope * amplitude;
                        }
                    }
                }
            }

            Normalize(stereo, 0.5f);
            return Save(folder + "/Crackle.wav", stereo, 2, true);
        }

        /// <summary>Cute little rising "brrr-ip" from the baby dragon, ending in a soft puff of breath.</summary>
        public static AudioClip DragonChirp(string folder)
        {
            const float seconds = 0.75f;
            var data = new float[(int)(seconds * Rate)];
            var rnd = new System.Random(5);
            float phase = 0f;
            int chirp = (int)(0.42f * Rate);
            for (int i = 0; i < chirp; i++)
            {
                float x = i / (float)chirp;
                float frequency = Mathf.Lerp(620f, 1250f, x * x) * (1f + 0.05f * Mathf.Sin(x * Mathf.PI * 2f * 9f));
                phase += 2f * Mathf.PI * frequency / Rate;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Min(1f, x * 1.15f)) * (0.7f + 0.3f * Mathf.Sin(x * Mathf.PI * 2f * 14f));
                data[i] += (Mathf.Sin(phase) + 0.25f * Mathf.Sin(phase * 2f)) * Mathf.Max(0f, envelope);
            }

            int puffStart = (int)(0.4f * Rate), puffLength = (int)(0.3f * Rate);
            float low = 0f, aPuff = OnePole(900f);
            for (int i = 0; i < puffLength; i++)
            {
                float x = i / (float)puffLength;
                low += aPuff * (((float)rnd.NextDouble() * 2f - 1f) - low);
                data[puffStart + i] += low * 1.6f * Mathf.Sin(Mathf.PI * x) * (1f - x);
            }
            Normalize(data, 0.4f);
            return Save(folder + "/Dragon Chirp.wav", data, 1, false);
        }

        /// <summary>Seamless loop of a deep, soft purr: the volcano waking up, warm rather than scary.</summary>
        public static AudioClip Rumble(string folder)
        {
            const float loopSeconds = 8f, fadeSeconds = 1.5f;
            int n = (int)(loopSeconds * Rate), fade = (int)(fadeSeconds * Rate);
            var raw = new float[n + fade];
            var rnd = new System.Random(31);
            float low1 = 0f, low2 = 0f, low3 = 0f, aLow = OnePole(70f);
            for (int i = -Rate; i < n + fade; i++)
            {
                float white = (float)rnd.NextDouble() * 2f - 1f;
                low1 += aLow * (white - low1);
                low2 += aLow * (low1 - low2);
                low3 += aLow * (low2 - low3);
                if (i < 0)
                    continue;
                float t = i / (float)Rate;
                // Slow purring pulse on top of the filtered noise.
                float purr = 0.75f + 0.25f * Mathf.Sin(t * Mathf.PI * 2f * 3f);
                raw[i] = low3 * purr + 0.004f * Mathf.Sin(t * Mathf.PI * 2f * 55f) * purr;
            }
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float s = raw[i];
                if (i < fade)
                {
                    float x = i / (float)fade;
                    s = s * Mathf.Sin(x * Mathf.PI * 0.5f) + raw[n + i] * Mathf.Cos(x * Mathf.PI * 0.5f);
                }
                data[i] = s;
            }
            Normalize(data, 0.45f);
            return Save(folder + "/Volcano Rumble.wav", data, 1, true);
        }

        /// <summary>Soft rising breath of air when the volcano throws up its glowing drops.</summary>
        public static AudioClip Whoosh(string folder)
        {
            const float seconds = 2.4f;
            var data = new float[(int)(seconds * Rate)];
            var rnd = new System.Random(37);
            float low = 0f, band = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float x = i / (float)data.Length;
                float cutoff = Mathf.Lerp(250f, 1400f, Mathf.Sin(x * Mathf.PI * 0.5f));
                float white = (float)rnd.NextDouble() * 2f - 1f;
                low += OnePole(cutoff) * (white - low);
                band += OnePole(cutoff * 0.4f) * (low - band);
                float envelope = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x / 0.35f)) * Mathf.Pow(1f - x, 1.6f);
                data[i] = (low - band) * envelope;
            }
            Normalize(data, 0.4f);
            return Save(folder + "/Whoosh.wav", data, 1, false);
        }

        /// <summary>Twinkling little bells going up a pentatonic scale, like a magic firework.</summary>
        public static AudioClip Chime(string folder)
        {
            const float seconds = 2.2f;
            var data = new float[(int)(seconds * Rate)];
            float[] notes = { 1046.5f, 1174.7f, 1318.5f, 1568f, 1760f, 2093f };
            for (int k = 0; k < notes.Length; k++)
            {
                int start = (int)(k * 0.09f * Rate);
                for (int i = start; i < data.Length; i++)
                {
                    float t = (i - start) / (float)Rate;
                    float envelope = Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-t * 3.2f);
                    float f = notes[k];
                    data[i] += envelope * (Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t * 6f))
                               * (1f - k * 0.08f);
                }
            }
            data = Echo(data);
            Normalize(data, 0.35f);
            return Save(folder + "/Chime.wav", data, 1, false);
        }

        /// <summary>Soft, round "bloop" of a lava bubble popping.</summary>
        public static AudioClip Bloop(string folder)
        {
            const float seconds = 0.28f;
            var data = new float[(int)(seconds * Rate)];
            float phase = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float x = i / (float)data.Length;
                float frequency = Mathf.Lerp(180f, 520f, Mathf.Sqrt(x));
                phase += 2f * Mathf.PI * frequency / Rate;
                float envelope = Mathf.Clamp01(x / 0.08f) * Mathf.Pow(1f - x, 2f);
                data[i] = Mathf.Sin(phase) * envelope;
            }
            Normalize(data, 0.35f);
            return Save(folder + "/Bloop.wav", data, 1, false);
        }

        static float[] Echo(float[] dry)
        {
            int delay = (int)(0.13f * Rate), delay2 = (int)(0.21f * Rate);
            var wet = new float[dry.Length + delay2 * 4];
            for (int i = 0; i < wet.Length; i++)
            {
                float s = i < dry.Length ? dry[i] : 0f;
                if (i >= delay)
                    s += wet[i - delay] * 0.22f;
                if (i >= delay2)
                    s += wet[i - delay2] * 0.15f;
                wet[i] = s;
            }
            Normalize(wet, 0.5f);
            return wet;
        }

        static float OnePole(float cutoff) => 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate);

        static void Normalize(float[] data, float peak)
        {
            float max = 1e-6f;
            foreach (float s in data)
                max = Mathf.Max(max, Mathf.Abs(s));
            float gain = peak / max;
            for (int i = 0; i < data.Length; i++)
                data[i] *= gain;
        }

        static AudioClip Save(string path, float[] samples, int channels, bool loop)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(stream))
            {
                int bytes = samples.Length * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + bytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                w.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                w.Write(16);
                w.Write((short)1);
                w.Write((short)channels);
                w.Write(Rate);
                w.Write(Rate * channels * 2);
                w.Write((short)(channels * 2));
                w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                w.Write(bytes);
                foreach (float s in samples)
                    w.Write((short)Mathf.RoundToInt(Mathf.Clamp(s, -1f, 1f) * 32767f));
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
