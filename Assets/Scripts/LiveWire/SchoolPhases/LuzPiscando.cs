using UnityEngine;

namespace LiveWire
{
    [RequireComponent(typeof(Light))]
    public class LuzPiscando : MonoBehaviour
    {
        [SerializeField] float intensidadeMinima = 0.1f;
        [SerializeField] float intensidadeMaxima = 1.4f;
        [SerializeField] float tempoMinimoEntrePiscadas = 0.05f;
        [SerializeField] float tempoMaximoEntrePiscadas = 0.4f;
        [SerializeField] float chanceDeApagar = 0.18f;
        [SerializeField] float duracaoApagado = 0.6f;

        Light luz;
        float proximoEvento;
        float voltaLigar;
        bool apagada;

        void Awake()
        {
            luz = GetComponent<Light>();
        }

        void Update()
        {
            if (apagada)
            {
                if (Time.time >= voltaLigar)
                {
                    apagada = false;
                    luz.intensity = Random.Range(intensidadeMinima, intensidadeMaxima);
                    proximoEvento = Time.time + Random.Range(tempoMinimoEntrePiscadas, tempoMaximoEntrePiscadas);
                }
                return;
            }

            if (Time.time < proximoEvento) return;

            if (Random.value < chanceDeApagar)
            {
                luz.intensity = 0f;
                apagada = true;
                voltaLigar = Time.time + Random.Range(duracaoApagado * 0.5f, duracaoApagado);
                return;
            }

            luz.intensity = Random.Range(intensidadeMinima, intensidadeMaxima);
            proximoEvento = Time.time + Random.Range(tempoMinimoEntrePiscadas, tempoMaximoEntrePiscadas);
        }
    }
}
