# Deploy

Arquitetura de produção, toda em tier gratuito:

```text
Vercel (Next.js)  ->  Google Cloud Run (API .NET)  ->  Neon (PostgreSQL)
```

---

## 1. Neon — o banco

Você já criou. Duas coisas importam.

### Converta a connection string

O Neon entrega no formato URI:

```text
postgresql://usuario:senha@host/neondb?sslmode=require
```

O Npgsql **não entende esse formato**. Ele precisa de chave-valor:

```text
Host=SEU-HOST-pooler.sa-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=SUA-SENHA;SSL Mode=Require;Trust Server Certificate=true;No Reset On Close=true
```

### Por que `No Reset On Close=true`

O endpoint com `-pooler` no nome é PgBouncer em modo transação. Sem esse
parâmetro o Npgsql tenta resetar a sessão ao devolver a conexão ao pool, o
PgBouncer recusa, e você toma erro **intermitente** — o pior tipo de bug para
diagnosticar, porque funciona nove vezes em dez.

> Use sempre o host **com** `-pooler`. O direto não aguenta várias instâncias.

### Teste localmente antes de subir

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;No Reset On Close=true" --project src/MmaLegacy.Api
dotnet run --project src/MmaLegacy.Api
```

Se a API subir e o Swagger responder, a migration e o seed rodaram no Neon.

---

## 2. Cloud Run — a API

### Pré-requisitos

1. Conta Google com faturamento ativado — o cartão é só validação; o tier
   gratuito é permanente e não vira cobrança sozinho.
2. [gcloud CLI](https://cloud.google.com/sdk/docs/install) instalado.

### Primeira vez

```powershell
gcloud auth login
gcloud projects create mma-legacy --name="MMA Legacy"
gcloud config set project mma-legacy
gcloud services enable run.googleapis.com cloudbuild.googleapis.com
```

Ative o faturamento no projeto pelo console antes de seguir.

### Deploy

Rode na raiz de `mma_legacy_api`:

```powershell
gcloud run deploy mma-legacy-api `
  --source . `
  --region southamerica-east1 `
  --allow-unauthenticated `
  --min-instances 0 `
  --max-instances 3 `
  --memory 512Mi `
  --set-env-vars "ConnectionStrings__DefaultConnection=Host=...;Database=neondb;Username=neondb_owner;Password=...;SSL Mode=Require;Trust Server Certificate=true;No Reset On Close=true"
```

Por que cada opção:

| Opção | Motivo |
| --- | --- |
| `southamerica-east1` | São Paulo, mesma região do seu Neon — menos latência por consulta |
| `--min-instances 0` | Escala a zero. É o que mantém a conta em R$ 0 |
| `--max-instances 3` | Teto de segurança: um pico de acesso não vira fatura |
| `--allow-unauthenticated` | É uma API pública de jogo |
| `512Mi` | Suficiente para .NET com este volume |

O primeiro deploy demora ~5 min (constrói a imagem). Os seguintes, ~2 min.

Ao final o comando imprime a URL. Guarde — o front precisa dela.

### Libere o CORS para a Vercel

Depois de publicar o front, volte aqui e informe o domínio:

```powershell
gcloud run services update mma-legacy-api `
  --region southamerica-east1 `
  --update-env-vars "Cors__AllowedOrigins__0=https://SEU-APP.vercel.app"
```

O `__` (dois sublinhados) é como o .NET lê seções aninhadas em variável de
ambiente: `Cors__AllowedOrigins__0` vira `Cors:AllowedOrigins[0]`.

### Confira

```powershell
curl https://SUA-URL.run.app/api/saude
```

---

## 3. Vercel — o front

1. Importe o repositório `mma_legacy_web`.
2. Em **Environment Variables**, adicione:

   ```text
   NEXT_PUBLIC_API_URL = https://SUA-URL.run.app/api
   ```

   O `/api` no fim é obrigatório — o cliente monta as rotas a partir dele.

3. Deploy. A Vercel detecta o Next.js sozinha.

Toda vez que trocar a variável, **refaça o deploy**: `NEXT_PUBLIC_*` é
embutida no bundle em tempo de build, não lida em tempo de execução.

---

## Ordem correta

Há uma dependência circular entre CORS e domínio. A ordem que evita retrabalho:

1. Neon pronto
2. Deploy da API → anote a URL
3. Deploy do front com a URL da API → anote o domínio da Vercel
4. Atualize o CORS da API com o domínio da Vercel

---

## Sobre o cold start

Com `--min-instances 0` a API dorme depois de alguns minutos sem uso, e a
primeira requisição espera de 1 a 3 segundos. O front já contorna isso: ao
abrir a ficha de inscrição ele chama `/api/saude` em segundo plano, então a API
acorda enquanto o jogador digita.

**Não crie um cron para manter a API quente.** Queima a cota gratuita e a
maioria dos termos de uso proíbe. Se um dia o cold start incomodar de verdade,
`--min-instances 1` resolve — mas aí passa a ter custo.

---

## Migrations em produção

A API aplica as migrations pendentes ao subir, inclusive em produção. É uma
escolha consciente: o correto em sistema com dado de usuário é migrar em passo
separado do deploy, porque uma migration ruim derruba a aplicação no ar em vez
de falhar num passo isolado.

Aqui o risco é aceitável — não há dado de terceiros e o projeto é de uma pessoa
só. **Quando existir conta de jogador, mude isso.**
