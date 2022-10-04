FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY . /source
WORKDIR /source/MateProxy
RUN dotnet publish -c Release -o /app --use-current-runtime --self-contained false

FROM mcr.microsoft.com/dotnet/aspnet:6.0
COPY --from=build /app /app
WORKDIR /app
ENTRYPOINT ["dotnet", "MateProxy.dll"]
EXPOSE 80/tcp
