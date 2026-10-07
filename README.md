# Target Desafio — API (Backend)

API REST em C# com autenticação JWT e SQL Server. Reúne três programas do desafio: **cálculo de comissões**, **movimentação de estoque** e **cálculo de juros por atraso**.

## Tecnologias e versões

| Tecnologia | Versão |
|---|---|
| .NET / ASP.NET Core | 9.0 (LTS) |
| C# | 12 |
| Entity Framework Core (SqlServer + Design) | 8.0.10 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.10 |
| SQL Server | 2019 ou superior (LocalDB, Express ou Docker) |
| dotnet-ef (ferramenta global) | 8.x |

## Pré-requisitos

1. **.NET 9 SDK**: https://dotnet.microsoft.com/download/dotnet/9.0
   Confirme com `dotnet --version` (deve começar com `9.`).
2. **SQL Server**, uma das opções:
   - **LocalDB** (já vem com o Visual Studio; também instalado com o SQL Server Express);
   - **SQL Server Express**: https://www.microsoft.com/sql-server/sql-server-downloads
   
3. **Ferramenta do EF Core**:
```powershell
   dotnet tool install --global dotnet-ef
```
4. (Opcional) **VS Code** com a extensão *C# Dev Kit*.

## Estrutura

```
Prototipo-ERP-Backend-main/
├── Controllers/          # Endpoints (finos, só orquestram)
├── Data/                 # DbContext, seed e vendas.json (dados de exemplo)
├── Domain/               # Entidades (Usuario, Produto, Movimentacao)
├── Dtos/                 # Contratos de entrada e saída da API
│   ├── Auth/
│   │   ├── LoginRequest.cs
│   │   └── LoginResponse.cs
│   ├── Comissoes/
│   │   ├── VendaDto.cs
│   │   ├── VendasRequest.cs
│   │   ├── VendaComissaoDto.cs
│   │   ├── ComissaoVendedorDto.cs
│   │   └── ResultadoComissaoDto.cs
│   ├── Estoque/
│   │   ├── ProdutoDto.cs
│   │   ├── NovaMovimentacaoRequest.cs
│   │   └── MovimentacaoDto.cs
│   └── Juros/
│       ├── JurosRequest.cs
│       └── ResultadoJurosDto.cs
├── Services/             # Regras de negócio (comissão, estoque, juros, token)
├── Infrastructure/       # Exceções de domínio, handler global de erros, JwtOptions
├── Migrations/           # Migrations do EF Core (gerada pelo dotnet ef; deve ser versionada)
├── Properties/
│   └── launchSettings.json
├── appsettings.json
└── Program.cs
```

## Configuração

Edite `appsettings.json`:

| Chave | Descrição |
|---|---|
| `ConnectionStrings:Default` | Conexão com o SQL Server |
| `Jwt:Key` | Chave de assinatura do token (**mínimo 32 caracteres**) |
| `Jwt:Issuer` / `Jwt:Audience` | Emissor e público do token |
| `Jwt:ExpiresMinutes` | Validade do token (padrão 60) |
| `Cors:Origins` | Origens permitidas (padrão `http://localhost:4200`) |

**Connection string para Docker/SQL Server com usuário e senha:**
```
Server=localhost,1433;Database=TargetDesafio;User Id=sa;Password=Senha@Forte123;TrustServerCertificate=True
```

**Não versione a chave JWT.** Use user-secrets em desenvolvimento:
```powershell
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "uma-chave-secreta-longa-com-mais-de-32-caracteres"
```

## Como rodar

```powershell
cd Prototipo-ERP-Backend-main

# 1) Criar a migration inicial (apenas na primeira vez)
dotnet ef migrations add Inicial

# 2) Executar (aplica a migration e faz o seed automaticamente em Development)
dotnet run
```

> [!TIP]
> Se o `dotnet run` falhar com erro de migration (por exemplo, `There is already an object named 'Produtos' in the database`), o banco já existe na sua máquina com um histórico diferente. Apague-o e rode de novo:
>
> ```bash
> dotnet ef database drop --force
> dotnet run
> ```
>
> O banco é recriado com as tabelas e os dados iniciais (usuário `admin` e os 5 produtos).

A API sobe em **http://localhost:4200**.
Para recarregar automaticamente ao salvar arquivos: `dotnet watch run`.

### Dados criados no primeiro start
- Usuário: **`admin`** / senha **`Admin@123`**
- Produtos: 101 Caneta Azul (150), 102 Caderno Universitário (75), 103 Borracha Branca (200), 104 Lápis Preto HB (320), 105 Marcador de Texto Amarelo (90)

## Endpoints

Todos exigem `Authorization: Bearer <token>`, exceto o login.

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/auth/login` | Autentica e retorna o token JWT |
| GET | `/api/comissoes/vendas-exemplo` | Retorna o JSON de vendas de exemplo |
| POST | `/api/comissoes/calcular` | Calcula a comissão por vendedor |
| GET | `/api/estoque/produtos` | Lista produtos e estoque atual |
| GET | `/api/estoque/movimentacoes?codigoProduto=` | Lista as últimas 100 movimentações |
| GET | `/api/estoque/movimentacoes/{id}` | Consulta uma movimentação |
| POST | `/api/estoque/movimentacoes` | Registra entrada/saída e retorna o estoque final |
| POST | `/api/juros/calcular` | Calcula os juros até a data de hoje |

### Exemplos

```http
POST /api/auth/login
{ "login": "admin", "senha": "Admin@123" }
```
```http
POST /api/estoque/movimentacoes
{ "codigoProduto": 101, "tipo": "Saida", "descricao": "Venda", "quantidade": 10 }
```
```http
POST /api/juros/calcular
{ "valor": 1000, "dataVencimento": "2026-09-26" }
```

### Códigos de erro
Os erros seguem o formato RFC 7807 (`ProblemDetails`):

| Status | Quando |
|---|---|
| 400 | Validação do corpo da requisição |
| 401 | Token ausente/expirado ou credenciais inválidas |
| 404 | Produto ou movimentação inexistente |
| 409 | Estoque alterado por outra operação (concorrência) |
| 422 | Regra de negócio violada (ex.: saída maior que o estoque) |
| 500 | Erro interno (detalhes apenas no log) |

## Regras de negócio

**Comissão** (por venda, arredondada em 2 casas):
- abaixo de R$ 100,00: sem comissão
- de R$ 100,00 até R$ 499,99: 1%
- a partir de R$ 500,00: 5%

**Estoque**
- Cada movimentação tem número único (identity), tipo (`Entrada`/`Saida`), descrição e quantidade.
- Saída maior que o saldo é recusada; o estoque nunca fica negativo (também garantido por *check constraint*).
- A concorrência é tratada com `rowversion`.

**Juros**: juros simples de 2,5% ao dia → `valor × 0,025 × dias de atraso`. Título não vencido tem juros zero.

## Front-end
[Clique aqui](https://github.com/MatheusADC/Prototipo-ERP-Frontend)
