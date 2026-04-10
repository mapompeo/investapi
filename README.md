# 📊 InvestAPI

<div align="center">

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?style=for-the-badge&logo=postgresql)
![JWT](https://img.shields.io/badge/JWT-000000?style=for-the-badge&logo=JSON%20web%20tokens)

**API REST para gerenciamento inteligente de carteira de investimentos**

[Sobre](#-sobre) • [Stack](#-stack) • [Como Usar](#-como-usar) • [Rotas](#-rotas)

</div>

---

## Sobre

InvestAPI é uma API para organizar uma carteira de investimentos. O projeto permite criar usuário, fazer login, cadastrar ativos, lançar transações, consultar resumo da carteira e atualizar cotações.

As cotações externas vêm da [Brapi](https://brapi.dev) para ações e da [CoinGecko](https://www.coingecko.com) para criptomoedas.

---

## Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- JWT Bearer
- BCrypt.Net-Next
- FluentValidation
- Swagger/OpenAPI
- HttpClientFactory

---

## 🚀 Como Usar

### Pré-requisitos

- .NET 10 SDK
- PostgreSQL instalado ou um banco PostgreSQL remoto

### Instalação Local

```bash
# Clone o repositório
git clone https://github.com/mapompeo/InvestAPI.git
cd InvestAPI

# Configure a connection string
# Edite appsettings.json ou a variável ConnectionStrings__DefaultConnection com sua conexão PostgreSQL

# Rode a aplicação
dotnet run
```

A API fica disponível nas portas definidas em [InvestAPI/Properties/launchSettings.json](InvestAPI/Properties/launchSettings.json) e o Swagger aparece em `/swagger`.

### Variáveis de Ambiente

```bash
ConnectionStrings__DefaultConnection="Host=YOUR_HOST;Port=5432;Database=InvestAPIDb;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
JwtSettings__SecretKey="sua-chave-secreta-com-32-caracteres-minimo"
JwtSettings__Issuer="InvestAPI"
JwtSettings__Audience="InvestAPI-Users"
JwtSettings__ExpirationInDays="7"
```

---

## Rotas

### Autenticação

| Método | Rota                 | Descrição               |
| ------ | -------------------- | ----------------------- |
| POST   | `/api/auth/register` | Registra novo usuário   |
| POST   | `/api/auth/login`    | Autentica e retorna JWT |

### Usuários 🔒

| Método | Rota               | Descrição            |
| ------ | ------------------ | -------------------- |
| GET    | `/api/users/me`    | Dados do usuário logado |
| GET    | `/api/users/{id}`  | Busca um usuário     |
| PUT    | `/api/users/{id}`  | Atualiza um usuário  |
| DELETE | `/api/users/{id}`  | Remove um usuário    |

### Assets 🔒

| Método | Rota               | Descrição            |
| ------ | ------------------ | -------------------- |
| POST   | `/api/assets`      | Adiciona ativo       |
| GET    | `/api/assets`      | Lista ativos         |
| GET    | `/api/assets/{id}` | Detalhes + histórico |
| DELETE | `/api/assets/{id}` | Remove ativo         |

### Transactions 🔒

| Método | Rota                | Descrição             |
| ------ | ------------------- | --------------------- |
| POST   | `/api/transactions` | Registra compra/venda |
| GET    | `/api/transactions` | Lista transações      |

### Portfolio 🔒

| Método | Rota                         | Descrição             |
| ------ | ---------------------------- | --------------------- |
| GET    | `/api/portfolio/summary`     | Resumo da carteira    |
| GET    | `/api/portfolio/performance` | Performance por ativo |
| GET    | `/api/dashboard`             | Dashboard completo    |

### Cotações 🔒

| Método | Rota                     | Descrição               |
| ------ | ------------------------ | ----------------------- |
| POST   | `/api/quotes/refresh`    | Atualiza todas as cotações |
| POST   | `/api/quotes/refresh/{ticker}` | Atualiza uma cotação |

🔒 = Requer autenticação (Bearer token)

---

## 💡 Exemplo de Uso

### 1. Registrar usuário

```bash
POST /api/auth/register
Content-Type: application/json

{
  "name": "João Silva",
  "email": "joao@example.com",
  "password": "SenhaForte123!",
  "confirmPassword": "SenhaForte123!"
}
```

### 2. Fazer login

```bash
POST /api/auth/login
Content-Type: application/json

{
  "email": "joao@example.com",
  "password": "SenhaForte123!"
}
```

**Response:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "joao@example.com",
  "name": "João Silva"
}
```

### 3. Adicionar ativo

```bash
POST /api/assets
Authorization: Bearer {seu-token}
Content-Type: application/json

{
  "ticker": "PETR4",
  "type": "Stock",
  "quantity": 100,
  "avgBuyPrice": 38.50
}
```

**Response:**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "ticker": "PETR4",
  "currentPrice": 40.2,
  "totalInvested": 3850.0,
  "currentValue": 4020.0,
  "profitLoss": 170.0,
  "profitLossPercentage": 4.42
}
```

---

## Como Funciona

1. O usuário cria conta e faz login.
2. A API devolve um token JWT.
3. Com o token, o usuário cria ativos e transações.
4. A API calcula os dados da carteira e busca cotações externas.

## 📚 Documentação Adicional

- **Swagger:** Acesse `/swagger` quando a API estiver rodando

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

---

## 👨‍💻 Autor

**Matheus Pompeo**

[![LinkedIn](https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/matheuspompeo/)
[![GitHub](https://img.shields.io/badge/GitHub-100000?style=for-the-badge&logo=github&logoColor=white)](https://github.com/mapompeo)

---

<div align="center">

**⭐ Se este projeto te ajudou, considere dar uma estrela!**

Made with ❤️ and C#

</div>
