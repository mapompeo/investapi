# InvestAPI - Progresso de Implementacao

Ultima atualizacao: 2026-03-13

## Status Geral

- Projeto em fase inicial com base de dominio pronta e CRUD de usuarios implementado.
- README descreve o estado alvo (roadmap) e ainda nao o estado atual.

## O que ja foi concluido

### Estrutura e dominio

- Modelos criados: Users, Assets, Transactions, AssetQuote.
- Relacionamentos e constraints configurados no EF Core em Data/AppDbContext.cs.
- Migrations iniciais presentes em InvestAPI/Migrations.

### Usuarios

- DTOs de usuario criados:
  - CreateUserDto
  - UpdateUserDto
  - UserResponseDto
- Controller de usuarios implementado em Controllers/UsersController.cs com:
  - GET /api/users/{id}
  - GET /api/users
  - POST /api/users
  - PUT /api/users/{id}
  - DELETE /api/users/{id}
- Senha hash com BCrypt no cadastro/atualizacao.
- POST valida email duplicado (retorna 409 Conflict).

## Pendencias para alinhar com README

### Infra e configuracao

- README aponta PostgreSQL, mas o projeto esta configurado para SQL Server em Program.cs e appsettings.json.
- README cita Swagger em /swagger, mas Program.cs usa AddOpenApi/MapOpenApi.
- README aponta .NET 8, mas o projeto esta em net10.0 no csproj.

### Arquitetura

- README descreve Controllers -> Services -> Repositories.
- Implementacao atual esta em Controllers + DbContext direto (sem Services/Repositories ainda).

### Endpoints planejados no README ainda nao implementados

- Auth:
  - POST /api/auth/register
  - POST /api/auth/login
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
- JWT completo (issuer, audience, expiracao, secret em configuracao).
  - Observacao: o pacote JwtBearer ja esta instalado, mas nao foi configurado no Program.cs.

## Checklist de validacao do README

- [x] Entidades principais (Users, Assets, Transactions, AssetQuote) fazem sentido para o dominio.
- [x] Relacoes do modelo de dados sao coerentes com o objetivo da API.
- [ ] Stack descrita no README esta alinhada com o codigo atual (ha divergencias em banco e auth).
- [ ] Arquitetura em camadas esta refletida no codigo (ainda nao).
- [ ] Endpoints listados no README existem de fato (apenas users existe, e nem esta documentado no README).
- [ ] Regras de negocio de portfolio/transacoes estao implementadas.

## Observacoes de coerencia

- O que foi implementado ate agora (CRUD de usuarios + dominio inicial) e coerente com o objetivo do projeto.
- O README esta correto como visao de produto final, mas hoje funciona mais como roadmap do que documentacao do estado atual.
- Recomenda-se manter este arquivo atualizado ao fim de cada bloco implementado para evitar desvio entre documentacao e codigo.
