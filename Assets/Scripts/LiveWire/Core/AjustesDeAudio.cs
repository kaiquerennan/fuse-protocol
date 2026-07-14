using UnityEngine;

namespace LiveWire
{
    public static class AjustesDeAudio
    {
        const string ChaveVolume = "volume_principal";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AplicarVolumeSalvo()
        {
            AudioListener.volume = VolumePrincipal;
        }

        public static float VolumePrincipal
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(ChaveVolume, 1f));
            set
            {
                float v = Mathf.Clamp01(value);
                AudioListener.volume = v;
                PlayerPrefs.SetFloat(ChaveVolume, v);
            }
        }
    }
}
