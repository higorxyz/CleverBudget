# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar tudo
COPY . .

# Restaurar dependências
RUN dotnet restore

# Publicar aplicação
RUN dotnet publish CleverBudget.Api/CleverBudget.Api.csproj -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Criar diretório persistente para o SQLite
RUN mkdir -p /data

# Copiar arquivos publicados
COPY --from=build /app/out .

# Definir connection string absoluta para o SQLite
ENV ConnectionStrings__DefaultConnection="Data Source=/data/cleverbudget.db"

# Garantir que a aplicação escute em todas as interfaces na porta fornecida pelo Railway
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
EXPOSE ${PORT}

# Rodar a aplicação
ENTRYPOINT ["dotnet", "CleverBudget.Api.dll"]
