FROM mcr.microsoft.com/dotnet/sdk:9.0.101 AS build
WORKDIR /app
COPY . /app
RUN dotnet publish Dalaran.Server/Dalaran.Server.fsproj -c release -o /dist

FROM mcr.microsoft.com/dotnet/aspnet:9.0.0
EXPOSE 8080
WORKDIR /app
COPY --from=build /dist ./
ENTRYPOINT ["dotnet", "Dalaran.Server.dll"]