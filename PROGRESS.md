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

- Assets:
  - POST /api/assets
  - GET /api/assets
  - GET /api/assets/{id}
  - DELETE /api/assets/{id}
- Transactions:
  - POST /api/transactions
  - GET /api/transactions
- Portfolio:
  - GET /api/portfolio/summary
  - GET /api/portfolio/performance
- Dashboard:
  - GET /api/dashboard

### Regras de negocio ainda nao implementadas

- Calculo automatico de preco medio por compra.
- Validacao de venda sem quantidade suficiente.
- Integracao com Brapi e CoinGecko.
- Cache de cotacoes com expiracao de 5 minutos.
- Validacoes com FluentValidation ainda nao integradas no pipeline.

## Checklist de validacao do README

- [x] Entidades principais (Users, Assets, Transactions, AssetQuote) fazem sentido para o dominio.
- [x] Relacoes do modelo de dados sao coerentes com o objetivo da API.
- [x] Stack principal no codigo esta alinhada com SQL Server + JWT + Swagger.
- [ ] Arquitetura em camadas esta refletida no codigo (ainda nao).
- [ ] Endpoints listados no README existem de fato (auth e users parciais; modulo de investimentos pendente).
- [ ] Regras de negocio de portfolio/transacoes estao implementadas.

## Observacoes de coerencia

- O que foi implementado ate agora (auth + usuario autenticado + dominio inicial) e coerente com o objetivo do projeto.
- O README esta correto como visao de produto final, mas hoje funciona mais como roadmap do que documentacao do estado atual.
- Recomenda-se manter este arquivo atualizado ao fim de cada bloco implementado para evitar desvio entre documentacao e codigo.
