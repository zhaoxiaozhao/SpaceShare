# 友邻座 API - 微信云托管构建
# 构建上下文应为 backend/ 目录（cloudbaserc 已配置）
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY src/FriendlySeat.Domain/FriendlySeat.Domain.csproj src/FriendlySeat.Domain/
COPY src/FriendlySeat.Application/FriendlySeat.Application.csproj src/FriendlySeat.Application/
COPY src/FriendlySeat.Infrastructure/FriendlySeat.Infrastructure.csproj src/FriendlySeat.Infrastructure/
COPY src/FriendlySeat.Api/FriendlySeat.Api.csproj src/FriendlySeat.Api/
RUN dotnet restore src/FriendlySeat.Api/FriendlySeat.Api.csproj

COPY src/ src/
RUN dotnet publish src/FriendlySeat.Api/FriendlySeat.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app/publish .
# 微信云托管要求监听 PORT 环境变量指定端口（默认 80）
ENV PORT=80
ENV ASPNETCORE_URLS=http://+:${PORT}
EXPOSE 80
ENTRYPOINT ["dotnet", "FriendlySeat.Api.dll"]
