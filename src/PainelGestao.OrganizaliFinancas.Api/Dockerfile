FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore PainelGestao.OrganizaliFinancas.slnx
RUN dotnet publish src/PainelGestao.OrganizaliFinancas.Api/PainelGestao.OrganizaliFinancas.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "PainelGestao.OrganizaliFinancas.Api.dll"]
