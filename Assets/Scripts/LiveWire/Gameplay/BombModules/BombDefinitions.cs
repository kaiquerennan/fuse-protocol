using UnityEngine;

namespace LiveWire
{
    public enum CorBomba
    {
        Vermelho,
        Azul,
        Amarelo,
        Preto,
        Verde
    }

    public enum DificuldadeBomba
    {
        Facil,
        Dificil
    }

    public static class BombDefinitions
    {
        public static readonly CorBomba[] CoresDosFios =
        {
            CorBomba.Vermelho,
            CorBomba.Azul,
            CorBomba.Amarelo,
            CorBomba.Preto
        };

        public static readonly CorBomba[] CoresDoBotao =
        {
            CorBomba.Vermelho,
            CorBomba.Azul
        };

        public static readonly CorBomba[] CoresDaMemoria =
        {
            CorBomba.Vermelho,
            CorBomba.Azul,
            CorBomba.Amarelo,
            CorBomba.Verde
        };

        public static string ObterNome(CorBomba cor) => cor switch
        {
            CorBomba.Vermelho => "VERMELHO",
            CorBomba.Azul => "AZUL",
            CorBomba.Amarelo => "AMARELO",
            CorBomba.Preto => "PRETO",
            CorBomba.Verde => "VERDE",
            _ => "DESCONHECIDO",
        };

        public static Color ObterCorUi(CorBomba cor) => cor switch
        {
            CorBomba.Vermelho => new Color(0.93f, 0.2f, 0.22f),
            CorBomba.Azul => new Color(0.18f, 0.48f, 0.98f),
            CorBomba.Amarelo => new Color(0.98f, 0.82f, 0.18f),
            CorBomba.Preto => new Color(0.1f, 0.12f, 0.16f),
            CorBomba.Verde => new Color(0.2f, 0.9f, 0.45f),
            _ => Color.gray,
        };

        public static Color ObterCorClara(CorBomba cor)
        {
            Color baseColor = ObterCorUi(cor);
            return Color.Lerp(baseColor, Color.white, 0.28f);
        }

        public static Color ObterCorTexto(CorBomba cor) => cor switch
        {
            CorBomba.Amarelo => new Color(0.13f, 0.16f, 0.14f),
            CorBomba.Verde => new Color(0.09f, 0.12f, 0.1f),
            _ => new Color(0.94f, 0.97f, 0.98f),
        };

        public static string ObterNome(DificuldadeBomba dificuldade) => dificuldade switch
        {
            DificuldadeBomba.Facil => "FACIL",
            DificuldadeBomba.Dificil => "DIFICIL",
            _ => "PADRAO",
        };
    }
}
