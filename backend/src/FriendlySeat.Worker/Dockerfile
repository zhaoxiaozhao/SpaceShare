FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY FriendlySeat.slnx ./
COPY src/FriendlySeat.Domain/FriendlySeat.Domain.csproj src/FriendlySeat.Domain/
COPY src/FriendlySeat.Application/FriendlySeat.Application.csproj src/FriendlySeat.Application/
COPY src/FriendlySeat.Infrastructure/FriendlySeat.Infrastructure.csproj src/FriendlySeat.Infrastructure/
COPY src/FriendlySeat.Api/FriendlySeat.Api.csproj src/FriendlySeat.Api/
COPY src/FriendlySeat.Admin.Api/FriendlySeat.Admin.Api.csproj src/FriendlySeat.Admin.Api/
COPY src/FriendlySeat.Worker/FriendlySeat.Worker.csproj src/FriendlySeat.Worker/
RUN dotnet restore src/FriendlySeat.Worker/FriendlySeat.Worker.csproj

COPY src/ src/
COPY . .
RUN dotnet publish src/FriendlySeat.Worker/FriendlySeat.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FriendlySeat.Worker.dll"]
