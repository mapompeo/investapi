# InvestAPI - Progresso de Implementacao

Ultima atualizacao: 2026-03-16

## Status Geral

- Projeto em fase inicial com base de dominio pronta, autenticacao JWT ativa e gestao de usuario autenticado implementada.
- README descreve o estado alvo (roadmap) e ainda nao o estado atual.

## O que ja foi concluido

### Estrutura e dominio

- Modelos criados: Users, Assets, Transactions, AssetQuote.
- Relacionamentos e constraints configurados no EF Core em Data/AppDbContext.cs.
- Migrations iniciais presentes em InvestAPI/Migrations.

### Usuarios

- DTOs de usuario criados:
  - UpdateUserDto
  - UserResponseDto
- Controller de usuarios implementado em Controllers/UsersController.cs com:
  - GET /api/users/me
  - GET /api/users/{id}
  - PUT /api/users/{id}
  - DELETE /api/users/{id}
- Atualizacao de senha com BCrypt no endpoint PUT.

### Autenticacao

- Controller de autenticacao implementado em Controllers/AuthController.cs com:
  - POST /api/auth/register
  - POST /api/auth/login
- Emissao de JWT com claims de usuario (sub, email, name).
- Program.cs configurado com AddAuthentication/AddJwtBearer e AddAuthorization.

### Assets

- DTOs de ativos criados:
  - CreateAssetDto
  - AssetResponseDto
- Controller de ativos implementado em Controllers/AssetsController.cs com:
  - POST /api/assets
  - GET /api/assets
  - GET /api/assets/{id}
  - DELETE /api/assets/{id}
- Endpoints de ativos restritos ao usuario autenticado por claim sub.

### Transactions

- DTOs de transacao criados:
  - CreateTransactionDto
  - TransactionResponseDto
- Controller de transacoes implementado em Controllers/TransactionsController.cs com:
  - POST /api/transactions
  - GET /api/transactions
- Regras de negocio ja implementadas em transacoes:
  - Calculo de preco medio em compra.
  - Bloqueio de venda com quantidade insuficiente.
  - Atualizacao da quantidade do ativo apos compra/venda.

### Portfolio

- Controller de portfolio implementado em Controllers/PortfolioController.cs com:
  - GET /api/portfolio/summary
  - GET /api/portfolio/performance
- Performance calculada por ativo com fallback para AvgBuyPrice quando nao ha cotacao cacheada em AssetQuotes.

### Dashboard

- Controller de dashboard implementado em Controllers/DashboardController.cs com:
  - GET /api/dashboard
- Indicadores agregados implementados:
  - Total investido, valor atual, lucro/prejuizo e percentual.
  - Melhor e pior ativo por performance.
  - Alocacao percentual por ticker.
  - Total de transacoes do usuario.

### Cotacoes e cache

- Servico de cotacoes implementado em Services/Quotes com:
  - IQuoteService + DbCachedQuoteService
  - Cliente Brapi para ativos de renda variavel (Stock/FII)
  - Cliente CoinGecko para cripto por id com fallback por simbolo
- Cache persistido em AssetQuotes com expiracao configuravel (padrao 5 minutos via QuoteSettings.CacheMinutes).
- Portfolio e Dashboard passaram a consultar o QuoteService em vez de ler cotacao diretamente da tabela.
- Mapeamento configuravel de ticker para coin id via QuoteSettings.CoinGeckoTickerToId.
- Endpoint manual de refresh de cotacoes implementado em Controllers/QuotesController.cs:
  - POST /api/quotes/refresh
  - POST /api/quotes/refresh/{ticker}

### Validacoes

- FluentValidation integrado no pipeline em Program.cs.
- Validators adicionados para DTOs de:
  - Auth (RegisterDto, LoginDto)
  - Assets (CreateAssetDto)
  - Transactions (CreateTransactionDto)

## Pendencias para alinhar com README

### Infra e configuracao

- Provider definido para SQL Server no projeto (removido pacote PostgreSQL do csproj).
- Swagger ativo em ambiente de desenvolvimento via AddSwaggerGen/UseSwagger/UseSwaggerUI.
- Projeto em net10.0.
- Secret JWT removido do valor real em appsettings.json para uso via ambiente.

### Arquitetura

- README descreve Controllers -> Services -> Repositories.
- Implementacao atual esta em Controllers + DbContext direto (sem Services/Repositories ainda).

### Endpoints planejados no README ainda nao implementados

- Nenhum endpoint principal pendente; foco atual em robustez, validacoes e refinamentos.

### Regras de negocio ainda nao implementadas

- Regras avancadas de consolidacao (por periodo, benchmark, historico de performance) ainda nao implementadas.

## Checklist de validacao do README

- [x] Entidades principais (Users, Assets, Transactions, AssetQuote) fazem sentido para o dominio.
- [x] Relacoes do modelo de dados sao coerentes com o objetivo da API.
- [x] Stack principal no codigo esta alinhada com SQL Server + JWT + Swagger.
- [ ] Arquitetura em camadas esta refletida no codigo (ainda nao).
- [x] Endpoints principais do README existem de fato (auth, users, assets, transactions, portfolio e dashboard implementados).
- [x] Regras de negocio base de transacoes e portfolio estao implementadas.

## Observacoes de coerencia

- O que foi implementado ate agora (auth + usuario autenticado + ativos + transacoes + portfolio + dashboard basicos) e coerente com o objetivo do projeto.
- O README esta correto como visao de produto final, mas hoje funciona mais como roadmap do que documentacao do estado atual.
- Recomenda-se manter este arquivo atualizado ao fim de cada bloco implementado para evitar desvio entre documentacao e codigo.