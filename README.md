# 🥊 MMA Legacy — API

Back-end do **MMA Legacy**: um jogo de draft e simulação de carreira de MMA. O
jogador recebe oito atletas, rouba uma habilidade de cada um e descobre até
onde o lutador que montou chegaria.

Esta API valida o draft, monta o lutador, simula a carreira inteira e devolve o
veredito de legado.

> Front-end em `mma_legacy_web`.

---

## 🚧 Status

MVP funcional. O fluxo completo — criar partida, draftar, simular carreira, ler
o resultado — está implementado e coberto por testes.

Ainda não implementado: draft diário, ranking, card compartilhável e
autenticação.

---

## 🧱 Como o projeto está organizado

Dois projetos apenas. Camadas são pastas, não assemblies: o custo de manter
quatro `.csproj` não se paga em um projeto deste tamanho, e extrair depois é
copiar pastas.

```text
src/MmaLegacy.Api/
├── Domain/            entidades, value objects e regras do jogo
│   ├── Enums/         o vocabulário: habilidades, categorias, estilos, legado
│   ├── Exceptions/    exceções tipadas que viram status HTTP
│   └── Rules/         overall, identificação de estilo, pontuação de legado
├── Simulation/        motor de luta, de carreira, adversários e evolução
├── Data/              DbContext, mapeamentos do EF, migrations e acervo
├── Services/          orquestração: partida, draft, carreira
├── Contracts/         o que entra e sai da API
├── Controllers/       endpoints
└── Infrastructure/    tratamento global de exceções

tests/MmaLegacy.Tests/
├── Domain/            regras do draft e do jogo
├── Simulation/        motor de luta, carreira, evolução e balanceamento
├── Integration/       fluxo completo atravessando serviços e banco
└── Support/           fábricas de cenário e banco de teste
```

Os nomes das pastas seguem a convenção do .NET, em inglês. Todo o resto —
classes, métodos, variáveis, mensagens — está em português.

---

## 🛠️ Como rodar

**Pré-requisitos:** .NET SDK 10 e Docker (ou um PostgreSQL local).

```powershell
docker compose up -d
dotnet run --project src/MmaLegacy.Api
```

Em `Development` a API aplica as migrations e semeia o acervo de atletas
sozinha na inicialização. Não é preciso rodar `dotnet ef` na mão.

Swagger em `http://localhost:5080/swagger`.

Para percorrer o fluxo inteiro sem abrir o navegador, use
[`MmaLegacy.Api.http`](src/MmaLegacy.Api/MmaLegacy.Api.http) — ele encadeia
criar partida, as oito rodadas do draft, a simulação e o resultado.

### Sem Docker

Aponte a connection string em `appsettings.Development.json` para o seu
PostgreSQL. A base padrão é:

```text
Host=localhost;Port=5432;Database=mma_legacy;Username=postgres;Password=postgres
```

### Testes

```powershell
dotnet test
```

Os testes de integração usam SQLite em memória e **não** precisam de Docker nem
de banco no ar.

---

## 🌐 Endpoints

| Método | Rota | O que faz |
| --- | --- | --- |
| `POST` | `/api/partidas` | Cria a partida e sorteia os oito atletas |
| `GET` | `/api/partidas/{partidaId}` | Estado atual da partida |
| `GET` | `/api/partidas/{partidaId}/draft/atual` | O atleta da vez e as habilidades livres |
| `POST` | `/api/partidas/{partidaId}/draft/escolher` | Registra a escolha da rodada |
| `POST` | `/api/partidas/{partidaId}/carreira/simular` | Simula a carreira inteira |
| `GET` | `/api/partidas/{partidaId}/carreira` | Carreira já simulada |
| `GET` | `/api/partidas/{partidaId}/resultado` | Ficha, lutador e carreira em uma leitura |

### Exemplo: criar partida

```json
POST /api/partidas
{
  "nome": "Rafael Diniz",
  "apelido": "The Machine",
  "nacionalidade": "Brasil",
  "categoriaDePeso": "MeioPesado",
  "idadeInicial": 22,
  "baseDeLuta": "MuayThai",
  "seed": 20260805
}
```

O campo `seed` é opcional. Informá-lo reproduz exatamente o mesmo draft e a
mesma carreira — é o que vai sustentar o draft diário.

### Exemplo: escolher habilidade

```json
POST /api/partidas/{partidaId}/draft/escolher
{
  "atletaId": "0e6448de-417f-4aa8-b0e2-550d3ce74368",
  "habilidade": "Potencia"
}
```

Repare no que **não** existe: nenhum campo de nota. O cliente diz apenas de
quem quer e o quê; o valor vem do acervo no servidor.

### Erros

Toda falha volta como `ProblemDetails`:

| Status | Quando |
| --- | --- |
| `400` | Ficha de inscrição inválida |
| `404` | Partida ou atleta inexistente |
| `409` | Habilidade já ocupada, atleta fora da rodada, draft não concluído |
| `500` | Erro não tratado — mensagem genérica, detalhe só no log |

---

## 🧠 Decisões que valem conhecer

**O cliente nunca envia notas.** O contrato de escolha só tem atleta e
habilidade. Mexer nos números pelo navegador não muda o lutador montado.

**A nota fica gravada na rodada**, e não é relida do acervo depois. Assim um
rebalanceamento futuro das notas editoriais não reescreve partidas antigas.

**O resumo da carreira é derivado das lutas**, não somado passo a passo pelo
motor. Contador que se incrementa a cada evento uma hora sai do lugar; contador
recalculado da lista, não.

**A evolução tem teto**: nenhuma habilidade cresce mais de 8 pontos acima da
nota de estreia. Sem esse limite, uma década de ganhos anuais levava qualquer
lutador para perto de 100 e as escolhas do draft viravam detalhe.

**O overall não decide luta nenhuma.** O motor resolve troca em pé, queda,
finalização e nocaute round a round. É por isso que um wrestler de overall 84
vence um nocauteador de overall 90 com defesa de queda ruim.

**Os limiares de legado foram calibrados contra a distribuição real do motor**,
medida sobre 300 carreiras de cada nível de build — não estimados. Um build 70
nunca é campeão, um build 85 vira campeão em cerca de metade das carreiras e um
build 96 chega a "maior de todos os tempos" em cerca de um quinto delas.

---

## ⚖️ Aviso legal

Projeto independente, para fins educacionais e de entretenimento. Sem
associação, parceria ou aprovação do UFC, da TKO Group Holdings ou de qualquer
organização esportiva.

As notas dos atletas são estimativas editoriais usadas exclusivamente dentro da
mecânica do jogo. Não representam avaliações oficiais. Os adversários da
simulação de carreira são fictícios e gerados em tempo de execução.

---

## 👨‍💻 Autor

Rafael Silva Diniz
