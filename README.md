# Financial Transaction Processor

Projeto de processamento financeiro baseado em microsserviços, com foco na separação de responsabilidades entre os domínios de cliente e de conta/transação.

## Objetivo

Disponibilizar uma base para:

- cadastro de clientes;
- solicitação de criação de contas;
- processamento de transações financeiras (Credit, Debit, Reserve, Capture, Reversal, Transfer);
- processamento assíncrono por meio do RabbitMQ.

## Arquitetura e decisões técnicas

### Visão geral

A solução está dividida em dois serviços principais:

- CustomerService: responsável pelos dados e operações de cliente;
- AccountService: responsável por conta, saldo e processamento de transações.

A comunicação ocorre por:

- HTTP entre serviços (o AccountService consulta o CustomerService);
- mensageria com RabbitMQ para comandos assíncronos de criação/processamento.

Persistência:

- SQL Server via EF Core em cada serviço;
- banco separado por contexto (isolamento por *bounded context*).

### Decisões arquiteturais

1. Separação por contexto de negócio
- CustomerService e AccountService possuem bancos e modelos próprios.
- Isso reduz acoplamento e facilita evolução independente.

2. Processamento assíncrono com filas
- Endpoints de escrita publicam em fila, e os consumidores processam as mensagens.
- Isso melhora a resiliência e desacopla a camada de API da execução da regra.

3. Regras de transação em pipeline (*Chain of Responsibility*)
- Há handlers especializados por operação (Credit, Debit, Reserve, etc.).
- Isso facilita extensão e manutenção sem alterar regras existentes.

4. Idempotência por reference_id
- Evita processamento duplicado da mesma transação.

5. Health checks e observabilidade
- Endpoints de *liveness* e *readiness*;
- métricas via OpenTelemetry (HTTP, runtime, HttpClient);
- logging estruturado para correlação operacional.

## Frameworks e bibliotecas (justificativa)

- ASP.NET Core Web API: API robusta, madura e integrada ao ecossistema .NET.
- Entity Framework Core (SQL Server): produtividade para persistência e migrations.
- RabbitMQ.Client: mensageria confiável para fluxos assíncronos.
- FluentValidation: validação declarativa e coesa de requests.
- Swashbuckle (Swagger): documentação e exploração de endpoints.
- OpenTelemetry: padrão de mercado para instrumentação de métricas.
- xUnit + Reqnroll + FluentAssertions: cobertura de unidade/integração e cenários BDD.

## Estrutura do projeto

- src/CustomerService
- src/AccountService
- tests/CustomerService/UnitTests
- tests/CustomerService/IntegrationTests
- tests/AccountService/UnitTests
- tests/AccountService/IntegrationTests
- docker/

## Pré-requisitos

Execução local:

- .NET SDK 10
- SQL Server (ou LocalDB)
- RabbitMQ

Execução via container:

- Docker Desktop

## Configuração

As configurações padrão estão em appsettings.json de cada serviço.
No Docker, configurações sensíveis/de conexão são sobrescritas por variáveis de ambiente no docker-compose.

## Exemplo de execução (local)

### 1) Compilar a solução

bash
dotnet build FinancialTransactionProcessor.slnx


### 2) Executar os testes

bash
dotnet test FinancialTransactionProcessor.slnx


### 3) Subir os serviços

Terminal 1:

bash
dotnet run --project src/CustomerService/CustomerService.csproj


Terminal 2:

bash
dotnet run --project src/AccountService/AccountService.csproj


### 4) Swagger

- CustomerService: http://localhost:5019/swagger
- AccountService: http://localhost:5020/swagger

## Exemplo de execução (Docker)

### 1) Subir a stack completa

bash
docker compose -f docker/docker-compose.yml up --build -d


### 2) Verificar o status

bash
docker compose -f docker/docker-compose.yml ps


### 3) Acompanhar logs

bash
docker compose -f docker/docker-compose.yml logs -f customerservice accountservice


### 4) Derrubar a stack

bash
docker compose -f docker/docker-compose.yml down


Observação:

- As migrations são aplicadas automaticamente no startup de cada serviço (com retry).
- Isso garante a criação/atualização das tabelas ao subir no Docker.

## Health checks

- AccountService
  - http://localhost:5020/health/live
  - http://localhost:5020/health/ready
- CustomerService
  - http://localhost:5019/health/live
  - http://localhost:5019/health/ready

## Exemplos de uso da API

### 1) Criar cliente

bash
curl -X POST http://localhost:5019/api/financialtransaction/customers \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Cliente Exemplo",
    "cpfCnpj": "12345678901"
  }'


### 2) Solicitar criação de conta

bash
curl -X POST http://localhost:5020/api/financialtransaction/accounts \
  -H "Content-Type: application/json" \
  -d '{
    "customerCpFCnpj": "12345678901",
    "availableBalance": 0,
    "reservedBalance": 0,
    "creditLimit": 500,
    "accountStatus": "Active"
  }'


### 3) Enfileirar crédito

bash
curl -X POST http://localhost:5020/api/financialtransaction/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "operation": "Credit",
    "account_id": "ACC-001",
    "amount": 100000,
    "currency": "BRL",
    "reference_id": "TXN-CREDIT-001"
  }'


### 4) Enfileirar reserva

bash
curl -X POST http://localhost:5020/api/financialtransaction/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "operation": "Reserve",
    "account_id": "ACC-001",
    "amount": 30000,
    "currency": "BRL",
    "reference_id": "TXN-RESERVE-001"
  }'


### 5) Enfileirar transferência

bash
curl -X POST http://localhost:5020/api/financialtransaction/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "operation": "Transfer",
    "account_id": "ACC-001",
    "destination_account_id": "ACC-002",
    "amount": 50000,
    "currency": "BRL",
    "reference_id": "TXN-TRANSFER-001"
  }'


### 6) Consultar transações processadas

bash
curl -X GET http://localhost:5020/api/financialtransaction/transactions


## Execução de testes por escopo

Todos:

bash
dotnet test FinancialTransactionProcessor.slnx


Somente testes unitários do AccountService:

bash
dotnet test tests/AccountService/UnitTests/AccountService.UnitTests.csproj


Somente testes unitários do CustomerService:

bash
dotnet test tests/CustomerService/UnitTests/CustomerService.UnitTests.csproj


## Notas operacionais

- RabbitMQ Management: http://localhost:15672 (guest/guest)
- Em produção, substitua credenciais padrão.
