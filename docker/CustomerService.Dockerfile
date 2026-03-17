FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FinancialTransactionProcessor.slnx ./
COPY src/CustomerService/CustomerService.csproj src/CustomerService/
COPY src/AccountService/AccountService.csproj src/AccountService/
RUN dotnet restore src/CustomerService/CustomerService.csproj

COPY . .
RUN dotnet publish src/CustomerService/CustomerService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CustomerService.dll"]
