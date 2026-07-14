using UnityEngine;

namespace LiveWire
{
    public static class ProceduralAudio
    {
        const int SampleRate = 44100;

        public static AudioClip GenerateTick()
        {
            float duration = 0.06f;
            int samples = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 90f);
                float wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 1500f * t));
                data[i] = wave * env * 0.35f;
            }
            return BuildClip("Tick", data);
        }

        public static AudioClip GenerateExplosion()
        {
            float duration = 1.8f;
            int samples = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[samples];
            float lowpass = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 2.2f);
                float noise = (Random.value * 2f - 1f);
                lowpass = Mathf.Lerp(lowpass, noise, 0.2f);
                float rumble = Mathf.Sin(2f * Mathf.PI * (45f - t * 18f) * t);
                data[i] = Mathf.Clamp((lowpass * 0.85f + rumble * 0.45f) * env, -1f, 1f) * 0.95f;
            }
            return BuildClip("Explosion", data);
        }

        public static AudioClip GenerateShock()
        {
            float duration = 0.18f;
            int samples = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 20f);
                float noise = Random.value * 2f - 1f;
                float buzz = Mathf.Sin(2f * Mathf.PI * 220f * t) * Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 60f * t));
                data[i] = (noise * 0.6f + buzz * 0.4f) * env * 0.8f;
            }
            return BuildClip("Shock", data);
        }

        public static AudioClip GenerateSuccess()
        {
            float duration = 0.6f;
            int samples = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float freq = Mathf.Lerp(440f, 880f, t / duration);
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.4f;
            }
            return BuildClip("Success", data);
        }

        public static AudioClip GenerateAlmost()
        {
            float duration = 0.4f;
            int samples = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float wobble = Mathf.Sin(2f * Mathf.PI * 8f * t) * 40f;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));
                data[i] = Mathf.Sin(2f * Mathf.PI * (220f + wobble) * t) * env * 0.35f;
            }
            return BuildClip("Almost", data);
        }

        public static AudioClip GenerateHiss()
        {
            float duration = 1.5f;
            int samples = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[samples];
            float hp = 0f;
            float prev = 0f;
            for (int i = 0; i < samples; i++)
            {
                float noise = Random.value * 2f - 1f;
                hp = noise - prev + 0.95f * hp;
                prev = noise;
                data[i] = hp * 0.35f;
            }
            AudioClip clip = AudioClip.Create("Hiss", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip GenerateClick()
        {
            float duration = 0.04f;
            int samples = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 180f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 2200f * t) * env * 0.25f;
            }
            return BuildClip("Click", data);
        }

        public static AudioClip GenerateConnect()
        {
            float duration = 0.25f;
            int samples = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 8f);
                float sweep = Mathf.Lerp(600f, 1400f, t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * sweep * t) * env * 0.4f;
            }
            return BuildClip("Connect", data);
        }

        public static AudioClip GenerateAlert()
        {
            float duration = 0.55f;
            int samples = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float phase = t % 0.18f;
                float env = Mathf.Exp(-phase * 14f) * Mathf.Clamp01(1f - t / duration);
                float wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 1200f * t));
                data[i] = wave * env * 0.55f;
            }
            return BuildClip("Alert", data);
        }

        public static AudioClip GenerateTension()
        {
            float duration = 1.5f;
            int samples = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float freq = Mathf.Lerp(80f, 360f, t / duration);
                float env = Mathf.Clamp01(t / 0.3f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 30f * t));
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.25f;
            }
            AudioClip clip = AudioClip.Create("Tension", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip GenerateRelief()
        {
            float duration = 1.4f;
            int samples = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float a = Mathf.Sin(2f * Mathf.PI * 523.25f * t);
                float b = Mathf.Sin(2f * Mathf.PI * 659.25f * t);
                float c = Mathf.Sin(2f * Mathf.PI * 783.99f * t);
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));
                data[i] = (a + b + c) * 0.15f * env;
            }
            return BuildClip("Relief", data);
        }

        public static AudioClip GenerateElectricHiss()
        {
            float duration = 2.0f;
            int samples = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[samples];
            float hp = 0f;
            float prev = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float crackle = Random.value > 0.993f ? (Random.value * 2f - 1f) * 0.7f : 0f;
                float noise = Random.value * 2f - 1f;
                hp = noise - prev + 0.92f * hp;
                prev = noise;
                float buzz = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.1f;
                data[i] = (hp * 0.28f + crackle + buzz) * 0.5f;
            }
            AudioClip clip = AudioClip.Create("ElectricHiss", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip BuildClip(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
