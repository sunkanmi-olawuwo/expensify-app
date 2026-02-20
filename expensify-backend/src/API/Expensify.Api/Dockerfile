FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["nuget.config", "./"]
COPY ["src/API/Expensify.Api/Expensify.Api.csproj", "src/API/Expensify.Api/"]
COPY ["src/API/Expensify.Api.Client/Expensify.Api.Client.csproj", "src/API/Expensify.Api.Client/"]
COPY ["src/Common/Expensify.Common.Application/Expensify.Common.Application.csproj", "src/Common/Expensify.Common.Application/"]
COPY ["src/Common/Expensify.Common.Domain/Expensify.Common.Domain.csproj", "src/Common/Expensify.Common.Domain/"]
COPY ["src/Common/Expensify.Common.Infrastructure/Expensify.Common.Infrastructure.csproj", "src/Common/Expensify.Common.Infrastructure/"]
COPY ["src/Common/Expensify.Common.Presentation/Expensify.Common.Presentation.csproj", "src/Common/Expensify.Common.Presentation/"]
COPY ["src/Modules/Users/Expensify.Modules.Users.Application/Expensify.Modules.Users.Application.csproj", "src/Modules/Users/Expensify.Modules.Users.Application/"]
COPY ["src/Modules/Users/Expensify.Modules.Users.Domain/Expensify.Modules.Users.Domain.csproj", "src/Modules/Users/Expensify.Modules.Users.Domain/"]
COPY ["src/Modules/Users/Expensify.Modules.Users.Infrastructure/Expensify.Modules.Users.Infrastructure.csproj", "src/Modules/Users/Expensify.Modules.Users.Infrastructure/"]
COPY ["src/Modules/Users/Expensify.Modules.Users.IntegrationEvents/Expensify.Modules.Users.IntegrationEvents.csproj", "src/Modules/Users/Expensify.Modules.Users.IntegrationEvents/"]
COPY ["src/Modules/Users/Expensify.Modules.Users.Presentation/Expensify.Modules.Users.Presentation.csproj", "src/Modules/Users/Expensify.Modules.Users.Presentation/"]
RUN dotnet restore "./src/API/Expensify.Api/Expensify.Api.csproj"
COPY . .
WORKDIR "/src/src/API/Expensify.Api"
RUN dotnet build "./Expensify.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:NoSwagGen=true

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Expensify.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:NoSwagGen=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Expensify.Api.dll"]