namespace LiveWire
{
    public enum LocalDaEscola
    {
        SalaDeAula,
        Banheiro,
        Biblioteca,
        Laboratorio,
        Refeitorio,
        Quadra,
        Corredor,
        Secretaria,
        Personalizado
    }

    public enum ModoDeObjetivoDaFase
    {
        Explicito,
        Oculto,
        Personalizado
    }

    public static class FaseDaEscolaTexto
    {
        public static string ObterNome(LocalDaEscola local) => local switch
        {
            LocalDaEscola.SalaDeAula => "sala de aula",
            LocalDaEscola.Banheiro => "banheiro",
            LocalDaEscola.Biblioteca => "biblioteca",
            LocalDaEscola.Laboratorio => "laboratorio",
            LocalDaEscola.Refeitorio => "refeitorio",
            LocalDaEscola.Quadra => "quadra",
            LocalDaEscola.Corredor => "corredor",
            LocalDaEscola.Secretaria => "secretaria",
            LocalDaEscola.Personalizado => "local definido",
            _ => "local desconhecido",
        };

        public static string ObterNomeEmCaixaAlta(LocalDaEscola local)
        {
            return ObterNome(local).ToUpperInvariant();
        }
    }
}
