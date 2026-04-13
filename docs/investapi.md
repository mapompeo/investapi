# 📊 API de Controle de Investimentos

## 🎯 Visão Geral

Sistema backend para gerenciamento de carteira de investimentos (ações BR e cripto), com integração a APIs externas para cotações em tempo real.

---

## 🛠️ Stack Tecnológica

- **.NET 10** - C# Web API
- **ASP.NET Core** - Controllers e pipeline HTTP
- **Entity Framework Core** - ORM
- **PostgreSQL** - Banco de dados
- **JWT Bearer** - Autenticação
- **BCrypt.Net-Next** - Hash de senhas
- **FluentValidation** - Validações
- **Swagger/OpenAPI** - Documentação
- **HttpClientFactory** - Integrações externas
- **Middleware global de exceção** - Tratamento centralizado de erros

Inventário completo: veja o [README](../README.md) para a visão geral do stack e da estrutura.

### APIs Externas

- **Brapi**: Ações brasileiras (B3) - `https://brapi.dev`
- **CoinGecko**: Criptomoedas - `https://api.coingecko.com`

---

## 📐 Arquitetura

- Controllers
- Services
- Repositories
- Models
- DTOs
- Validators
- Middleware

---

## 📡 Endpoints Principais

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/assets`
- `POST /api/assets`
- `GET /api/transactions`
- `POST /api/transactions`
- `GET /api/portfolio/summary`
- `GET /api/portfolio/performance`
- `GET /api/dashboard`

---

## 🚀 Deploy Público

- O Swagger fica disponível em `/swagger`
- A raiz `/` redireciona para o Swagger
- O schema do banco é criado automaticamente no startup com `EnsureCreated()`
- Para produção gratuita, use PostgreSQL free em Neon, Supabase ou Render e hospede a API em Render ou Railway
- Para demonstrar como API, o foco é testar cadastro, login e CRUD diretamente no Swagger

---

## ✅ Objetivo do Projeto

Este projeto existe para demonstrar domínio de APIs REST, autenticação JWT, EF Core, Swagger e integração com serviços externos.