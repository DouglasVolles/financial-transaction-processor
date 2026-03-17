FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FinancialTransactionProcessor.slnx ./
COPY src/AccountService/AccountService.csproj src/AccountService/
COPY src/CustomerService/CustomerService.csproj src/CustomerService/
RUN dotnet restore src/AccountService/AccountService.csproj

COPY . .
RUN dotnet publish src/AccountService/AccountService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AccountService.dll"]
