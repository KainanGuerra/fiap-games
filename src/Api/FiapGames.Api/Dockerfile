FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FiapGames.sln .
COPY src/Api/FiapGames.Api/FiapGames.Api.csproj src/Api/FiapGames.Api/
COPY src/Shared/FiapGames.Shared.Kernel/FiapGames.Shared.Kernel.csproj src/Shared/FiapGames.Shared.Kernel/
COPY src/Shared/FiapGames.Shared.Infrastructure/FiapGames.Shared.Infrastructure.csproj src/Shared/FiapGames.Shared.Infrastructure/
COPY src/Modules/Users/FiapGames.Modules.Users/FiapGames.Modules.Users.csproj src/Modules/Users/FiapGames.Modules.Users/
COPY src/Modules/Games/FiapGames.Modules.Games/FiapGames.Modules.Games.csproj src/Modules/Games/FiapGames.Modules.Games/
COPY tests/FiapGames.Modules.Users.Tests/FiapGames.Modules.Users.Tests.csproj tests/FiapGames.Modules.Users.Tests/
COPY tests/FiapGames.Modules.Games.Tests/FiapGames.Modules.Games.Tests.csproj tests/FiapGames.Modules.Games.Tests/

RUN dotnet restore src/Api/FiapGames.Api/FiapGames.Api.csproj

COPY . .

RUN dotnet publish src/Api/FiapGames.Api/FiapGames.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

USER app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FiapGames.Api.dll"]
