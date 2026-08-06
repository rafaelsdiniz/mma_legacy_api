# Imagem da API para o Cloud Run.
#
# Multi-stage: o SDK compila e some, e a imagem final leva só o runtime do
# ASP.NET. A diferença é de ~800 MB para ~110 MB, o que importa direto no tempo
# de cold start — que é o que o jogador sente ao clicar em "Montar meu lutador"
# depois de a aplicação ter dormido.

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS compilacao
WORKDIR /origem

# Os .csproj vêm antes do resto do código de propósito: enquanto as dependências
# não mudarem, o Docker reaproveita a camada de restore e o build fica em
# segundos em vez de minutos.
COPY MmaLegacy.slnx ./
COPY src/MmaLegacy.Api/MmaLegacy.Api.csproj src/MmaLegacy.Api/
COPY tests/MmaLegacy.Tests/MmaLegacy.Tests.csproj tests/MmaLegacy.Tests/
RUN dotnet restore src/MmaLegacy.Api/MmaLegacy.Api.csproj

COPY src/ src/
RUN dotnet publish src/MmaLegacy.Api/MmaLegacy.Api.csproj \
    -c Release \
    -o /publicado \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Usuário sem privilégios: se algum dia a aplicação for comprometida, o atacante
# não cai como root dentro do contêiner.
RUN adduser --disabled-password --no-create-home --uid 1001 mmalegacy
USER mmalegacy

# O Cloud Run encaminha o tráfego para a 8080 por padrão. O ASP.NET não lê a
# variável PORT sozinho, então a porta é fixada aqui.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=0

EXPOSE 8080

COPY --from=compilacao /publicado .

ENTRYPOINT ["dotnet", "MmaLegacy.Api.dll"]
