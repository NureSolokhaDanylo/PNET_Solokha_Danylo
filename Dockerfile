FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5191

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["PNET_Solokha_Danylo.Blazor/PNET_Solokha_Danylo.Blazor.csproj", "PNET_Solokha_Danylo.Blazor/"]
COPY ["PNET_Solokha_Danylo.Application/PNET_Solokha_Danylo.Application.csproj", "PNET_Solokha_Danylo.Application/"]
COPY ["PNET_Solokha_Danylo.Infrastructure/PNET_Solokha_Danylo.Infrastructure.csproj", "PNET_Solokha_Danylo.Infrastructure/"]
COPY ["PNET_Solokha_Danylo.Domain/PNET_Solokha_Danylo.Domain.csproj", "PNET_Solokha_Danylo.Domain/"]
RUN dotnet restore "PNET_Solokha_Danylo.Blazor/PNET_Solokha_Danylo.Blazor.csproj"
COPY . .
WORKDIR "/src/PNET_Solokha_Danylo.Blazor"
RUN dotnet build "PNET_Solokha_Danylo.Blazor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PNET_Solokha_Danylo.Blazor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PNET_Solokha_Danylo.Blazor.dll"]
