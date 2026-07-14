# 🧨 Fuse Protocol

> Desarme a bomba antes que o tempo acabe.

**Fuse Protocol** é um jogo de tensão e desativação de bombas em primeira pessoa, ambientado em uma escola. O jogador precisa explorar os corredores, localizar o dispositivo explosivo e desarmá-lo resolvendo um mini-puzzle — tudo isso contra um cronômetro implacável e com apenas três tentativas.

Não há combate. Não há inimigos. Só você, o relógio correndo e a certeza de que errar tem consequência.

---

## 🎮 Sobre o jogo

| | |
|---|---|
| **Gênero** | Puzzle / Tensão / Thriller psicológico |
| **Perspectiva** | Primeira pessoa |
| **Modo** | Single player |
| **Plataforma** | PC (Windows) |
| **Engine** | Unity |
| **Linguagem** | C# |
| **Classificação** | Não recomendado para menores de 16 anos |

---

## 📸 Capturas de tela

### Menu principal
![Menu principal](screenshots/menu.jpg)

### Gameplay — localizando a bomba
A iluminação vermelha sinaliza a proximidade do dispositivo. O cronômetro e o contador de erros ficam sempre visíveis.

![Gameplay](screenshots/hud.jpg)

### Mini-puzzle — Sincronizador de Ondas
Ajuste **frequência** e **amplitude** até sobrepor as duas ondas. Três erros e acabou.

![Puzzle](screenshots/puzzle.jpg)

### Cenário
Escola em estética *low-poly*, com iluminação dramática guiando o olhar do jogador.

![Cenário](screenshots/corredor.jpg)

---

## 🕹️ Como jogar

### Controles

| Ação | Input |
|---|---|
| Mover | `W` `A` `S` `D` |
| Olhar | Mouse |
| Interagir | `E` |
| Manipular o puzzle | Clique + arraste |

### Objetivo

1. **Explore** a escola procurando pela bomba
2. **Siga a luz vermelha** — ela fica mais intensa conforme você se aproxima
3. **Interaja** com o dispositivo (`E`) para abrir o painel de desativação
4. **Sincronize as ondas** ajustando frequência e amplitude até o *lock-on*

### Condições de derrota

- ⏱️ O cronômetro chegar a **00:00**
- ❌ Acumular **3 erros** no puzzle

---

## 🔁 Core gameplay loop

```
    EXPLORAR  ──▶  LOCALIZAR  ──▶  DESARMAR
        ▲                              │
        │                              ▼
    PROGREDIR  ◀──────────────────  AVALIAR
```

Cada ciclo dura entre 30 segundos e 2 minutos.

---

## 📥 Download

**[⬇️ Baixar o jogo (Google Drive)](https://drive.google.com/drive/folders/1839K1bvT1tZp9nhDB1vBt5OFOuZ3rYYo?usp=drive_link)**

## 🎥 Gameplay

- **[Assistir no YouTube](https://youtu.be/HIdspTP8Ehg)**
- **[Assistir no Google Drive](https://drive.google.com/file/d/1XrIjncWoeDrU7dBLW_IrPcI_ZxWOGaZj/view?usp=sharing)**

---

## 🛠️ Desenvolvimento

### Arquitetura

O projeto segue o modelo de componentes do Unity (`MonoBehaviour`), com scripts separados por responsabilidade:

- **Controlador do jogador** — movimentação e câmera em primeira pessoa
- **Gerenciador de fase** — cronômetro, contador de erros e transições de estado
- **Lógica do puzzle** — sincronização das funções senoidais do Sincronizador de Ondas

Não foram utilizadas bibliotecas externas: o projeto apoia-se exclusivamente nas APIs nativas da engine (`Input`, `Canvas/UI`, `Lighting`).

### ⚠️ Sobre o histórico de commits

Este repositório contém **um único commit**. O projeto foi desenvolvido ao longo do semestre **sem uso de controle de versão**, e o repositório foi criado apenas ao final, para disponibilizar o código-fonte.

Registramos isso abertamente: foi o principal erro de processo do projeto e está documentado no *post-mortem* do relatório final. A primeira recomendação que fazemos a nós mesmos para projetos futuros é **criar o repositório na primeira semana, antes da primeira linha de código**.

---

## 🧪 Testes

O jogo foi validado em duas frentes:

- **Teste em papel** (2 participantes) — antes de qualquer codificação, o *core loop* foi jogado com fichas, um mapa em A4 e cartões representando o puzzle
- **Playtest digital** (2 participantes) — sessões de 12 a 15 minutos com a build, sem instruções prévias

Os dois testes convergiram nos mesmos achados, o que gerou três ajustes prioritários:

1. Reforçar a pista de localização da bomba (feedback sonoro de proximidade)
2. Criar um micro-tutorial para os controles do puzzle
3. Adicionar feedback claro a cada erro cometido

---

## 🚧 Trabalhos futuros

- [ ] Novos módulos de puzzle (keypads, sequências de fios, mecanismos físicos)
- [ ] Camada narrativa (o estudante Daniel e o antagonista "A Voz")
- [ ] Desenho sonoro completo — hoje ausente e crítico para um jogo de tensão
- [ ] Sistema de fases com curva de dificuldade balanceada
- [ ] Telas de pausa e vitória

---

## 👥 Equipe

| Nome | Matrícula |
|---|---|
| **Kaique Rennan** | 23.1.8046 |
| **Márcio Paiva** | 23.1.8012 |

Sistemas de Informação

---

## 📚 Referências e inspirações

- **[Counter-Strike](https://store.steampowered.com/app/10/)** (Valve) — a dinâmica de *plant/defuse* como objetivo central
- **[Keep Talking and Nobody Explodes](https://keeptalkinggame.com/)** (Steel Crate Games) — a bomba como conjunto de módulos de puzzle
- **[Phasmophobia](https://store.steampowered.com/app/739630/)** (Kinetic Games) — exploração em primeira pessoa de interiores escuros
- **[Lethal Company](https://store.steampowered.com/app/1966720/)** (Zeekerss) — estética *low-poly* com iluminação pesada
- **MDA Framework** (Hunicke, LeBlanc & Zubek, 2004) — base conceitual de mecânica, dinâmica e estética

---

## 📄 Créditos

Os modelos 3D dos cenários provêm de **assets gratuitos da [Unity Asset Store](https://assetstore.unity.com)**, utilizados sob os termos da *Standard Unity Asset Store EULA*.

A composição das cenas, o *level design*, a configuração de iluminação, a interface (HUD, menus e o painel do Sincronizador de Ondas) e toda a lógica de jogo são de autoria da equipe.

### Uso de Inteligência Artificial

Este projeto contou com apoio de IA generativa (Claude, Anthropic) como assistente de programação em C# e na elaboração da documentação. Todas as decisões conceituais, de design e de arquitetura são de autoria humana, e todo o código foi revisado, testado e integrado pela equipe. A declaração completa, conforme o modelo CRediT-IA, encontra-se no relatório final do projeto.

---

## 📝 Contexto acadêmico

Projeto desenvolvido para a disciplina de desenvolvimento de jogos digitais (2026.1), percorrendo todas as etapas do processo: ideação, *game design document*, narrativa, identidade visual, prototipação em papel, *vertical slice* e demo final.
