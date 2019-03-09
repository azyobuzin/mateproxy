FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine AS build
COPY . /source
WORKDIR /source/MateProxy
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-alpine
COPY --from=build /app /app
WORKDIR /app
ENTRYPOINT ["dotnet", "MateProxy.dll"]
EXPOSE 80/tcp
