# 📊 API de Controle de Investimentos

## 🎯 Visão Geral

Sistema backend para gerenciamento de carteira de investimentos (ações BR e cripto), com integração a APIs externas para cotações em tempo real.

---

## 🛠️ Stack Tecnológica

- **.NET 8** - C# Web API
- **Entity Framework Core** - ORM
- **PostgreSQL** - Banco de dados
- **JWT** - Autenticação
- **BCrypt** - Hash de senhas
- **FluentValidation** - Validações
- **Swagger** - Documentação
- **Railway** - Hospedagem (Free tier)

### APIs Externas
- **Brapi**: Ações brasileiras (B3) - `https://brapi.dev/api/quote/{ticker}`
- **CoinGecko**: Criptomoedas - `https://api.coingecko.com/api/v3/simple/price`

---

## 📐 Arquitetura

```
InvestmentAPI/
├── Controllers/          # Endpoints
├── Services/            # Lógica de negócio
├── Repositories/        # Acesso a dados
├── Models/              # Entidades
├── DTOs/                # Request/Response
├── Validators/          # FluentValidation
├── Middleware/          # Tratamento de erros
├── Helpers/             # Utils
└── Data/                # EF Core + Migrations
```

**Camadas:**
- **Controller** → valida JWT, chama Service
- **Service** → regras de negócio, chama Repository
- **Repository** → acesso ao banco (EF Core)
- **Models** → entidades do banco
- **DTOs** → contratos de entrada/saída

---

## 🗄️ Modelagem do Banco

### Diagrama de Relacionamentos

```
User (1) ──→ (N) Asset (1) ──→ (N) Transaction
                  │
                  └─→ AssetQuote (cache)
```

### Tabelas

#### **Users**
| Campo        | Tipo         | Descrição              |
|--------------|--------------|------------------------|
| Id           | Guid (PK)    | Identificador          |
| Name         | varchar(100) | Nome do usuário        |
| Email        | varchar(150) | Email (UNIQUE)         |
| PasswordHash | varchar(255) | Senha (BCrypt)         |
| CreatedAt    | DateTime     | Data de criação        |

---

#### **Assets**
| Campo       | Tipo          | Descrição                   |
|-------------|---------------|-----------------------------|
| Id          | Guid (PK)     | Identificador               |
| UserId      | Guid (FK)     | Dono do ativo               |
| Ticker      | varchar(20)   | Código (PETR4, BTC, etc)    |
| Type        | enum          | Stock / Crypto / FII        |
| Quantity    | decimal(18,8) | Quantidade possuída         |
| AvgBuyPrice | decimal(18,2) | Preço médio de compra       |
| CreatedAt   | DateTime      | Data de criação             |
| UpdatedAt   | DateTime      | Última atualização          |

**Regras:**
- Único ticker por usuário
- Quantity e AvgBuyPrice recalculados a cada transação

---

#### **Transactions**
| Campo      | Tipo          | Descrição                  |
|------------|---------------|----------------------------|
| Id         | Guid (PK)     | Identificador              |
| AssetId    | Guid (FK)     | Ativo relacionado          |
| Type       | enum          | Buy / Sell                 |
| Quantity   | decimal(18,8) | Qtd comprada/vendida       |
| Price      | decimal(18,2) | Preço unitário             |
| TotalValue | decimal(18,2) | Quantity * Price           |
| Date       | DateTime      | Data da transação          |
| Notes      | varchar(500)  | Observações (opcional)     |
| CreatedAt  | DateTime      | Data de registro           |

**Regras:**
- Compra: atualiza Quantity e AvgBuyPrice do Asset
- Venda: diminui Quantity (não pode vender mais do que tem)

---

#### **AssetQuotes** (Cache)
| Campo        | Tipo          | Descrição           |
|--------------|---------------|---------------------|
| Id           | Guid (PK)     | Identificador       |
| Ticker       | varchar(20)   | Código (UNIQUE)     |
| CurrentPrice | decimal(18,2) | Preço atual         |
| Currency     | varchar(3)    | BRL / USD           |
| Source       | varchar(50)   | Brapi / CoinGecko   |
| LastUpdate   | DateTime      | Última atualização  |

**Cache:** Expira em 5 minutos

---

## 📡 Endpoints (Rotas)

### 🔑 Autenticação

**POST /api/auth/register**
- Registra novo usuário
- Body: `{ name, email, password, confirmPassword }`
- Validações: email único, senha forte (8+ chars, maiúscula, número)

**POST /api/auth/login**
- Autentica e retorna JWT token (validade: 7 dias)
- Body: `{ email, password }`
- Response: `{ token, email, name }`

---

### 💼 Assets (Ativos)

**POST /api/assets** 🔒
- Adiciona ativo na carteira
- Body: `{ ticker, type, quantity, avgBuyPrice }`
- Consulta cotação atual via API externa
- Response: asset + currentPrice + profitLoss

**GET /api/assets** 🔒
- Lista todos os ativos do usuário
- Query: `?type=Stock|Crypto|FII` (opcional)
- Response: array de assets com cotações atualizadas

**GET /api/assets/{id}** 🔒
- Detalhes do ativo + histórico de transações
- Response: asset completo + lista de transactions

**DELETE /api/assets/{id}** 🔒
- Remove ativo (cascade: deleta transactions também)

---

### 💸 Transactions (Transações)

**POST /api/transactions** 🔒
- Registra compra/venda
- Body: `{ assetId, type, quantity, price, date, notes? }`
- **Compra:** recalcula AvgBuyPrice e aumenta Quantity
- **Venda:** diminui Quantity (valida se tem disponível)

**GET /api/transactions** 🔒
- Lista transações do usuário
- Query: `?assetId=...&type=Buy|Sell&startDate=...&endDate=...`

---

### 📊 Portfolio

**GET /api/portfolio/summary** 🔒
- Resumo geral da carteira
- Response:
  - `totalInvested`, `currentValue`, `totalProfitLoss`
  - Diversificação por tipo (%, valor)
  - Top performers

**GET /api/portfolio/performance** 🔒
- Performance detalhada por ativo
- Alocação percentual de cada asset

---

### 📈 Dashboard

**GET /api/dashboard** 🔒
- Dashboard completo
- Agregação de: summary + contadores + transações recentes + diversificação

🔒 = Requer `Authorization: Bearer {token}`

---

## 🔄 Fluxos Principais

### Fluxo de Autenticação

```
1. User → POST /api/auth/register
   ↓
   Sistema cria User (PasswordHash com BCrypt)
   ↓
   201 Created

2. User → POST /api/auth/login
   ↓
   Valida email + senha
   ↓
   Gera JWT token (exp: 7 dias, claims: userId, email, name)
   ↓
   200 OK { token }

3. User acessa recursos protegidos
   ↓
   Header: Authorization: Bearer {token}
   ↓
   Middleware valida token e extrai UserId
   ↓
   Request processado
```

---

### Fluxo de Adicionar Ativo

```
User → POST /api/assets { ticker: "PETR4", type: "Stock", quantity: 100, avgBuyPrice: 38.50 }
   ↓
Controller valida JWT
   ↓
Service valida:
   - Ticker já existe para o user? → 400 Bad Request
   - Dados válidos? (FluentValidation)
   ↓
QuoteService busca cotação atual:
   - Verifica cache (AssetQuotes)
   - Se expirado (>5min): consulta Brapi
   - Atualiza cache
   ↓
Repository cria Asset no banco
   ↓
Service calcula profitLoss
   ↓
Response: Asset + currentPrice + profitLoss + profitLossPercentage
```

---

### Fluxo de Transação (Compra)

```
User → POST /api/transactions { assetId, type: "Buy", quantity: 50, price: 40.00 }
   ↓
Service valida:
   - Asset existe e pertence ao user?
   - Dados válidos?
   ↓
Cria Transaction no banco
   ↓
Atualiza Asset:
   - Nova quantidade = quantidade atual + 50
   - Novo preço médio = (valor total investido + valor novo) / nova quantidade
   ↓
Salva Asset atualizado
   ↓
Response: Transaction criada
```

---

### Fluxo de Transação (Venda)

```
User → POST /api/transactions { assetId, type: "Sell", quantity: 30, price: 42.00 }
   ↓
Service valida:
   - Asset existe?
   - Tem quantidade suficiente? (30 <= quantidade atual)
   ↓
Cria Transaction no banco
   ↓
Atualiza Asset:
   - Nova quantidade = quantidade atual - 30
   - Preço médio permanece o mesmo
   ↓
Salva Asset atualizado
   ↓
Response: Transaction criada
```

---

### Fluxo de Portfolio Summary

```
User → GET /api/portfolio/summary
   ↓
Service busca todos os Assets do user
   ↓
Para cada Asset:
   - QuoteService busca cotação atual (cache ou API)
   - Calcula: totalInvested, currentValue, profitLoss
   ↓
Agrega dados:
   - Soma total investido
   - Soma valor atual total
   - Agrupa por tipo (Stock, Crypto, FII)
   - Calcula % de cada tipo
   - Ordena por performance
   ↓
Response: Summary completo
```

---

## 🧮 Lógica de Cálculos

### Preço Médio (após compra)
```
Novo preço médio = (Quantidade atual × Preço médio atual) + (Quantidade comprada × Preço compra)
                   ───────────────────────────────────────────────────────────────────────────
                                      Quantidade atual + Quantidade comprada

Exemplo:
Tinha: 50 × R$ 37,00 = R$ 1.850
Comprou: 50 × R$ 40,00 = R$ 2.000
Novo preço médio: (1.850 + 2.000) / 100 = R$ 38,50
```

### Lucro/Prejuízo
```
Total investido = Quantidade × Preço médio
Valor atual = Quantidade × Preço atual
Lucro/Prejuízo = Valor atual - Total investido
Percentual = (Lucro/Prejuízo / Total investido) × 100

Exemplo:
100 ações × R$ 38,50 = R$ 3.850 (investido)
Preço atual: R$ 40,20
Valor atual: 100 × R$ 40,20 = R$ 4.020
Lucro: R$ 170
Percentual: (170 / 3.850) × 100 = 4,42%
```

### Alocação
```
Alocação (%) = (Valor do ativo / Valor total da carteira) × 100

Exemplo:
Ações: R$ 4.020
Cripto: R$ 14.000
Total: R$ 18.020
Alocação de ações: (4.020 / 18.020) × 100 = 22,31%
```

---

## 🚀 Plano de Desenvolvimento (5 semanas)

### Semana 1: Setup + Autenticação
- Criar projeto .NET 8 Web API
- Instalar pacotes (EF Core, JWT, FluentValidation, Swagger)
- Criar Models (User, Asset, Transaction, AssetQuote)
- Configurar AppDbContext + primeira migration
- Implementar AuthService (registro, login, BCrypt)
- Criar AuthController
- Configurar JWT no Program.cs
- **Entrega:** Registro e login funcionando no Swagger

### Semana 2: CRUD de Assets
- Criar AssetService e AssetRepository
- Implementar QuoteService (integração Brapi + CoinGecko)
- Criar sistema de cache (AssetQuotes)
- Implementar AssetController (POST, GET, DELETE)
- Adicionar validações (FluentValidation)
- **Entrega:** CRUD de assets com cotações atualizadas

### Semana 3: Transactions
- Criar TransactionService e TransactionRepository
- Implementar lógica de compra (recalcular AvgBuyPrice)
- Implementar lógica de venda (validar quantidade)
- Criar TransactionController (POST, GET)
- Adicionar filtros (assetId, type, date)
- **Entrega:** Sistema completo de transações funcionando

### Semana 4: Portfolio + Dashboard
- Criar PortfolioService
- Implementar cálculos (summary, performance, alocação)
- Criar PortfolioController
- Criar DashboardController (agregação de dados)
- Adicionar middleware de erros global
- **Entrega:** Dashboard completo com todas as métricas

### Semana 5: Deploy + Documentação
- Configurar variáveis de ambiente
- Deploy no Railway (app + PostgreSQL)
- Rodar migrations em produção
- Criar README completo
- Documentar endpoints no Swagger (XML comments)
- Testar API em produção
- **Entrega:** Projeto pronto e documentado

---

## ✅ Checklist Final

### Funcionalidades
- [ ] Autenticação JWT
- [ ] CRUD de Assets
- [ ] CRUD de Transactions
- [ ] Integração Brapi + CoinGecko
- [ ] Cálculos de rentabilidade
- [ ] Portfolio summary
- [ ] Dashboard

### Qualidade
- [ ] Arquitetura em camadas
- [ ] FluentValidation implementado
- [ ] Tratamento global de erros
- [ ] Código limpo e organizado
- [ ] DTOs separados de Models

### Segurança
- [ ] Senhas com BCrypt
- [ ] JWT configurado
- [ ] Endpoints protegidos
- [ ] Secrets não commitados

### Documentação
- [ ] README completo
- [ ] Swagger documentado
- [ ] Exemplos de uso

### Deploy
- [ ] API em produção (Railway)
- [ ] PostgreSQL funcionando
- [ ] Migrations aplicadas

---

## 💡 Conceitos Demonstrados

✅ Arquitetura em camadas  
✅ Entity Framework Core  
✅ JWT + Autenticação  
✅ FluentValidation  
✅ Integração com APIs REST  
✅ Cálculos financeiros  
✅ Tratamento de erros  
✅ Swagger/OpenAPI  
✅ Deploy em cloud  

---

## 📚 Recursos

- [ASP.NET Core Docs](https://learn.microsoft.com/aspnet/core)
- [EF Core Docs](https://learn.microsoft.com/ef/core)
- [Brapi Docs](https://brapi.dev/docs)
- [CoinGecko API](https://www.coingecko.com/en/api/documentation)
- [Railway](https://railway.app/)

---

## 🎯 Próximos Passos (Pós-MVP)

**Features:**
- Background service para atualizar cotações
- Notificações de preço-alvo
- Importação CSV
- Gráficos de evolução
- Calendário de dividendos

**Melhorias técnicas:**
- Testes unitários
- Cache Redis
- Rate limiting
- Paginação
- Docker

**Bora começar! 🚀**
