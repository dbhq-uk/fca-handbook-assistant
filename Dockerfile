# Build and run the ASP.NET Core host (API + Blazor UI). Central package management means the
# whole repo is copied so restore can see Directory.Packages.props and Directory.Build.props.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/FcaHandbookAssistant.Api/FcaHandbookAssistant.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# The .NET container images listen on 8080 by default; Container Apps targets this port.
EXPOSE 8080
ENTRYPOINT ["dotnet", "FcaHandbookAssistant.Api.dll"]
