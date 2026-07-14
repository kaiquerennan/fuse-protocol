using UnityEngine;

namespace LiveWire
{
    public abstract class VistaModulo3D : MonoBehaviour
    {
        public ModuloBomba Modulo { get; private set; }

        public void Vincular(ModuloBomba modulo)
        {
            if (Modulo == modulo) return;
            if (Modulo != null) DesinscreverEventosBase();
            Modulo = modulo;
            if (Modulo != null) InscreverEventosBase();
        }

        protected virtual void OnDestroy()
        {
            if (Modulo != null) DesinscreverEventosBase();
        }

        void InscreverEventosBase()
        {
            Modulo.AoResolver += HandleResolvido;
            Modulo.AoFalhar += HandleFalha;
            Modulo.AoAtualizarStatus += HandleStatus;
            Modulo.AoIniciar += HandleIniciar;
            Modulo.AoMudarInstrucao += HandleInstrucao;
            InscreverEventosEspecificos();
        }

        void DesinscreverEventosBase()
        {
            Modulo.AoResolver -= HandleResolvido;
            Modulo.AoFalhar -= HandleFalha;
            Modulo.AoAtualizarStatus -= HandleStatus;
            Modulo.AoIniciar -= HandleIniciar;
            Modulo.AoMudarInstrucao -= HandleInstrucao;
            DesinscreverEventosEspecificos();
        }

        protected virtual void InscreverEventosEspecificos() { }
        protected virtual void DesinscreverEventosEspecificos() { }
        protected virtual void HandleIniciar() { }
        protected virtual void HandleResolvido(ModuloBomba modulo) { }
        protected virtual void HandleFalha(ModuloBomba modulo, string motivo) { }
        protected virtual void HandleStatus(ModuloBomba modulo, string status) { }
        protected virtual void HandleInstrucao(string instrucao) { }
    }
}
