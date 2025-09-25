# 📌 SCS API
API desenvolvida em ASP.NET Core 9.0 para o projeto SCS (Sociedade, Consciência e Solução).
O objetivo é permitir que cidadãos possam reportar e acompanhar problemas em qualquer cidade e estado, promovendo transparência, cidadania e pressão social para que os responsáveis busquem melhorias na vida da população.

# ⚙️ Tecnologias utilizadas
- ASP.NET Core 9.0 – Framework principal da API
- ASP.NET Core Identity – Gerenciamento de usuários e autenticação
- JWT (JSON Web Tokens) – Autenticação baseada em tokens
- Entity Framework Core – ORM para acesso ao banco de dados
- MariaDB – Banco de dados relacional
- ailKit + SMTP – Envio de e-mails (confirmação de conta e recuperação de senha)

# 🚧 Status do projeto
A API ainda está em desenvolvimento (versão inicial).

# ✅ Funcionalidades já implementadas
👤 Contas / Usuários
- Registro e login com token e refresh token
- Confirmação de e-mail via link ou código
- Recuperação e alteração de senha via link ou código
- Obtenção de dados do usuário autenticado
- Atualização de CPF/CNPJ
- Gerenciamento de cargos/roles

📢 Reporte de Problemas 

Criar reporte com:
- Tipo
- Descrição
- Endereço (texto ou localização geográfica)
- Fotos
- Status
- Usuário reportador
- Votos positivos e negativos
- Resposta oficial
- Obter lista de problemas com filtros por:
- Status
- Cidade
- Estado
- Paginação (página e tamanho de página)
- Obter problema específico
- Atualizar status de um problema
- Votar em problemas
- Obter e atualizar resposta oficial

🎯 Objetivo
O SCS nasceu da ideia de criar uma plataforma para reportar e acompanhar problemas urbanos em tempo real, oferecendo aos cidadãos:
- Mais voz e participação nas decisões
- Pressão social para resolução de problemas
- Maior transparência na gestão pública

# ⚠️ Importante: antes de rodar a API, atualize o appsettings.json.

# 📌 Exemplos de requisições
🔑 Registro de usuário
```
POST /api/Account/register

{
  "fullName": "Carlos Henrique",
  "email": "carlos@email.com",
  "password": "SenhaSegura123!",
  "confirmPassword": "SenhaSegura123!",
  "deviceId": "meu-celular-001"
}
```

Resposta (200):
```
{
  "success": true,
  "message": "Usuário registrado com sucesso! Verifique seu e-mail para confirmar a conta."
}
```
🔑 Login
```
POST /api/Account/login

{
  "email": "carlos@email.com",
  "password": "minhasenha!",
  "deviceId": "..."
}
```
```
Resposta (200):
{
  "success": true,
  "message": "Login efetuado com sucesso.",
  "data": {
    "userId": "123abc",
    "fullName": "Carlos Henrique",
    "email": "carlos@email.com",
    "roles": ["User"],
    "token": "......",
    "refreshToken": "......"
  }
}
```
📢 Reportar problema
```
POST /api/Problem/report
Form-Data:
type = "Iluminação"
description = "Poste apagado na rua principal"
latitude = -15.12345
longitude = -48.12345
city = "Padre Bernardo"
state = "GO"
file = imagem.jpg
```
```
Resposta (200):
{
  "success": true,
  "message": "Problema reportado com sucesso.",
  "data": {
    "id": 12,
    "type": "Iluminação",
    "description": "Poste apagado na rua principal",
    "city": "Padre Bernardo",
    "state": "GO",
    "status": 0,
    "imageUrl": "/uploads/problems/imagem3457834sd.jpg",
    "createdAt": "2025-09-25T13:45:00Z",
    "userId": "123abc"
  }
}
```

# 📑 Endpoints da API
| Método   | Endpoint                                       | Autenticação | Descrição                                           |
| -------- | ---------------------------------------------- | ------------ | --------------------------------------------------- |
| **POST** | `/api/Account/register`                        | ❌            | Registrar novo usuário                              |
| **POST** | `/api/Account/request-email-confirmation-code` | ❌            | Solicitar código de confirmação de e-mail           |
| **POST** | `/api/Account/confirm-email-with-code`         | ❌            | Confirmar e-mail usando código                      |
| **GET**  | `/api/Account/confirm-email`                   | ❌            | Confirmar e-mail via link                           |
| **POST** | `/api/Account/login`                           | ❌            | Login com e-mail e senha                            |
| **POST** | `/api/Account/refresh`                         | ❌            | Renovar token de acesso usando refresh token        |
| **POST** | `/api/Account/logout`                          | ✅            | Logout do usuário (revoga refresh token)            |
| **POST** | `/api/Account/change-password`                 | ✅            | Alterar senha com senha atual                       |
| **POST** | `/api/Account/forgot-password`                 | ❌            | Solicitar link e código de redefinição de senha     |
| **POST** | `/api/Account/reset-password`                  | ❌            | Redefinir senha usando token                        |
| **POST** | `/api/Account/request-password-reset-code`     | ❌            | Solicitar código de redefinição de senha            |
| **POST** | `/api/Account/reset-password-with-code`        | ❌            | Redefinir senha usando código                       |
| **GET**  | `/api/Account/get-user-info`                   | ✅            | Obter informações do usuário logado                 |
| **PUT**  | `/api/Account/update-cpfcnpj`                  | ✅            | Atualizar CPF/CNPJ e marcar usuário como verificado |
| **POST** | `/api/Account/add-role`                        | ✅ (Admin)    | Atribuir um cargo/role a um usuário                 |

📢 Problem (Problemas Reportados)
| Método   | Endpoint                              | Autenticação                                        | Descrição                                                        |
| -------- | ------------------------------------- | --------------------------------------------------- | ---------------------------------------------------------------- |
| **POST** | `/api/Problem/report`                 | ✅                                                   | Reportar novo problema (com foto e localização)                  |
| **GET**  | `/api/Problem/get-all`                | ✅                                                   | Obter lista de problemas (com filtros e paginação)               |
| **GET**  | `/api/Problem/{id}`                   | ✅                                                   | Obter detalhes de um problema específico                         |
| **PUT**  | `/api/Problem/{id}`                   | ✅                                                   | Atualizar status de um problema (resolver/recorrente) com imagem |
| **POST** | `/api/Problem/{id}/vote`              | ✅                                                   | Votar em um problema (👍 ou 👎)                                  |
| **GET**  | `/api/Problem/{id}/official-response` | ❌                                                   | Obter resposta oficial de um problema                            |
| **PUT**  | `/api/Problem/{id}/official-response` | ✅ (Desenvolvedor, Admin, AdminRegional, Verificado) | Atualizar resposta oficial                                       |

