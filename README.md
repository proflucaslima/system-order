ESTOQUE API - GUIA PRÁTICO DE IMPLEMENTAÇÃO
Professor: Lucas Lima
Stack: .NET 8 / ASP.NET Core Web API
Arquitetura: API -> Application -> Domain <- Infrastructure



============================================================
COMO USAR ESTE MATERIAL
============================================================

Projeto USADO DE REFERENCIA PARA ARQUITETURA 4 CAMADAS COM IMPLEMENTAÇÃO DE AUTH/JWT/METRICS EM OUTRO REPO LINK https://github.com/proflucaslima/projeto-arquitetura-4-camadas

Este arquivo foi escrito como um roteiro de aula e de implementação.
A ideia NÃO é apenas copiar código. Em cada etapa você deve entender:

1) O QUE estamos adicionando.
2) POR QUE essa funcionalidade existe.
3) POR QUE ela está sendo implementada dessa forma.
4) O QUE acontece internamente quando a API recebe uma requisição.
5) COMO testar se a implementação está funcionando.
6) O QUE mudaria em uma aplicação de produção.

IMPORTANTE PARA A AULA:
- Quando aparecer // dentro do código, é um comentário C# e não é executado.
- Quando aparecer # nos comandos, é um comentário de terminal em shells compatíveis.
- Os exemplos de usuário/senha e lista em memória são didáticos.
- Segurança real não deve depender de usuário/senha hardcoded.
- JWT não é criptografia: o conteúdo do token pode ser lido. A assinatura garante integridade/autenticidade.
- HTTPS continua obrigatório mesmo quando usamos JWT.

FLUXO MENTAL QUE O ALUNO DEVE ENTENDER:

Cliente -> HTTPS -> CORS -> Rate Limit -> Autenticação -> Autorização
       -> Controller -> Application -> Domain/Infra -> Resposta
       -> Auditoria/Métricas/Logs

Autenticação = "Quem é você?"
Autorização  = "O que você pode fazer?"
Observabilidade = "O que está acontecendo com a aplicação?"
Auditoria = "Quem fez o quê e quando?"

============================================================
0. ONDE VOCÊ ESTÁ AGORA
============================================================

EXPLICAÇÃO:
Você começou separando responsabilidades em quatro projetos. Isso reduz acoplamento e evita colocar regra de negócio, acesso a banco e detalhes HTTP no mesmo lugar. A direção das referências é importante: o domínio deve permanecer o mais independente possível.

POR QUE FAZER ASSIM?
- API cuida do mundo HTTP: controllers, autenticação, Swagger, CORS e middlewares.
- Application coordena casos de uso.
- Domain representa regras e conceitos do negócio.
- Infra implementa detalhes externos, como banco, cache, filas e serviços.

Durante este roteiro, vários exemplos ficarão na API para facilitar a aula. Sempre que isso acontecer, o texto vai indicar como seria a versão mais madura em produção.


Você já criou a solução e as quatro camadas:

mkdir stocksystem
cd stocksystem

dotnet new sln -n StockSystem
mkdir src

dotnet new classlib -n Estoque.Domain -o src/Estoque.Domain -f net8.0
dotnet new classlib -n Estoque.Application -o src/Estoque.Application -f net8.0
dotnet new classlib -n Estoque.Infra -o src/Estoque.Infra -f net8.0
dotnet new webapi -n Estoque.Api -o src/Estoque.Api -f net8.0 --use-controllers

dotnet sln add src/Estoque.Domain/Estoque.Domain.csproj
dotnet sln add src/Estoque.Application/Estoque.Application.csproj
dotnet sln add src/Estoque.Infra/Estoque.Infra.csproj
dotnet sln add src/Estoque.Api/Estoque.Api.csproj

dotnet add src/Estoque.Application reference src/Estoque.Domain
dotnet add src/Estoque.Infra reference src/Estoque.Application
dotnet add src/Estoque.Infra reference src/Estoque.Domain
dotnet add src/Estoque.Api reference src/Estoque.Application
dotnet add src/Estoque.Api reference src/Estoque.Infra

Objetivo a partir daqui:

1) JWT
2) Roles
3) Claims
4) Policies
5) HTTPS
6) CORS
7) Segredos e configuração
8) Proteções comuns
9) Rate limit
10) Auditoria
11) Swagger/OpenAPI
12) Versionamento
13) Filtros, ordenação e paginação
14) Padrão de erros / ProblemDetails
15) Contratos
16) Health Checks
17) Métricas Prometheus
18) Revisão final de qualidade e prontidão

IMPORTANTE:
- Este material usa credenciais de DEMONSTRAÇÃO via User Secrets.
- Em produção, usuários/senhas devem vir de um Identity Provider, banco seguro,
  Microsoft Entra ID, Auth0, Keycloak, ASP.NET Core Identity etc.
- Nunca coloque chave JWT real, senha ou connection string sensível no Git.

============================================================
1. ABRIR O PROJETO E VALIDAR
============================================================

POR QUE COMEÇAR COM RESTORE E BUILD?
Antes de adicionar novas funcionalidades, precisamos garantir que o ponto de partida compila. Se adicionarmos dez mudanças e só depois executarmos o build, fica muito mais difícil descobrir qual alteração causou o erro.

- dotnet restore baixa/restaura as dependências NuGet declaradas nos projetos.
- dotnet build compila toda a solução e revela erros de referência, sintaxe e pacotes.
- code . abre a pasta atual no Visual Studio Code.

REGRA DE AULA: após cada bloco grande de alteração, execute dotnet build.


No terminal, dentro de stocksystem:

code .

dotnet restore
dotnet build

Se o build estiver OK, continue.

============================================================
2. INSTALAR OS PACOTES
============================================================

POR QUE PRECISAMOS DE PACOTES?
O ASP.NET Core já possui muitos recursos nativos, mas algumas integrações são distribuídas como pacotes NuGet. Instalar somente o que usamos também deixa claro para os alunos quais capacidades foram adicionadas ao projeto.

JWT Bearer valida tokens enviados no cabeçalho Authorization.
Swashbuckle gera documentação OpenAPI e a interface Swagger UI.
Asp.Versioning ajuda a manter versões da API de forma explícita.
prometheus-net expõe métricas em um formato que o Prometheus consegue coletar.

Depois de instalar, fazemos restore/build para validar compatibilidade imediatamente.

OBSERVAÇÃO PARA .NET 8:
- O pacote Microsoft.AspNetCore.Authentication.JwtBearer precisa acompanhar a versão do ASP.NET Core.
- Por isso usamos --version 8.*. Assim evitamos instalar por engano uma versão mais nova, como 9.x ou 10.x, que pode não ser compatível com net8.0.


Execute na raiz da solução:

# JWT
dotnet add src/Estoque.Api package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.*

# Swagger/OpenAPI com UI
dotnet add src/Estoque.Api package Swashbuckle.AspNetCore

# Versionamento de API
dotnet add src/Estoque.Api package Asp.Versioning.Mvc
dotnet add src/Estoque.Api package Asp.Versioning.Mvc.ApiExplorer

# Métricas Prometheus
dotnet add src/Estoque.Api package prometheus-net.AspNetCore

Depois:

dotnet restore
dotnet build

============================================================
3. CRIAR A ESTRUTURA DE PASTAS DA API
============================================================

POR QUE CRIAR PASTAS?
Pastas não criam arquitetura sozinhas, mas ajudam a tornar responsabilidades visíveis. Em uma aula, isso é especialmente útil porque  conseguimos localizar rapidamente onde fica cada preocupação transversal.

Auth = geração/autenticação de tokens.
Configuration = classes que representam configurações.
Contracts = modelos HTTP de entrada/saída.
Middleware = componentes que interceptam toda requisição.
Health = verificações de saúde.
Metrics = métricas customizadas.
Swagger = configuração de documentação.

Evite criar uma pasta chamada Utils para colocar "qualquer coisa". Prefira nomes que expliquem responsabilidade.


Crie estas pastas dentro de src/Estoque.Api:

Auth
Configuration
Controllers
Contracts
Extensions
Filters
Metrics
Middleware
Health
Swagger

No Linux/macOS/Git Bash:

mkdir -p src/Estoque.Api/Auth \
         src/Estoque.Api/Configuration \
         src/Estoque.Api/Controllers \
         src/Estoque.Api/Contracts \
         src/Estoque.Api/Extensions \
         src/Estoque.Api/Filters \
         src/Estoque.Api/Metrics \
         src/Estoque.Api/Middleware \
         src/Estoque.Api/Health \
         src/Estoque.Api/Swagger

============================================================
4. CONFIGURAÇÃO DO JWT
============================================================

O QUE ESTAMOS FAZENDO?
Vamos representar a seção Jwt do appsettings por uma classe fortemente tipada. Em vez de espalhar chamadas como Configuration["Jwt:Issuer"] por toda a aplicação, centralizamos a estrutura esperada em JwtSettings.

POR QUE IOptions?
O padrão Options do .NET faz binding de configuração para uma classe e integra naturalmente com injeção de dependência. Isso reduz strings mágicas e deixa a configuração mais fácil de validar e testar.

Issuer = quem emitiu o token.
Audience = para quem o token foi emitido.
Key = chave usada para assinar/validar o token no exemplo simétrico.
ExpirationMinutes = tempo de vida.

A chave NÃO ficará no appsettings versionado.


Crie o arquivo:
src/Estoque.Api/Configuration/JwtSettings.cs

CONTEÚDO:

namespace Estoque.Api.Configuration;

// sealed: não esperamos herdar desta classe. Ela representa somente configuração.
public sealed class JwtSettings
{
    // Nome exato da seção que será lida do appsettings/User Secrets.
    // Ter a string em um único lugar evita repetir "Jwt" em vários arquivos.
    public const string SectionName = "Jwt";

    // Identifica quem emitiu o token. Na validação, rejeitamos tokens
    // emitidos por outro emissor.
    public string Issuer { get; init; } = string.Empty;

    // Identifica para qual sistema/cliente o token foi criado.
    public string Audience { get; init; } = string.Empty;

    // Segredo usado para assinatura HMAC neste exemplo.
    // IMPORTANTE: o valor real vem de User Secrets/variável de ambiente,
    // não do Git.
    public string Key { get; init; } = string.Empty;

    // Tempo de vida curto reduz a janela de uso de um token roubado.
    public int ExpirationMinutes { get; init; } = 60;
}

------------------------------------------------------------
4.1. appsettings.json SEM SEGREDO
------------------------------------------------------------

Edite src/Estoque.Api/appsettings.json para algo semelhante a:

{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Issuer": "Estoque.Api",
    "Audience": "Estoque.Client",
    "ExpirationMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "http://localhost:5173"
    ]
  }
}

NÃO coloque Jwt:Key aqui.

============================================================
5. GESTÃO DE SEGREDOS COM USER SECRETS
============================================================

O QUE É UM SEGREDO?
É qualquer valor que não deveria aparecer em código-fonte ou repositório: senha, chave JWT, connection string com credencial, client secret e API key.

POR QUE USER SECRETS?
No ambiente de desenvolvimento, ele permite manter valores fora do projeto/Git. O .NET combina essas configurações com appsettings e variáveis de ambiente.

IMPORTANTE: User Secrets é conveniência de desenvolvimento, não um cofre de produção. Em Azure, por exemplo, prefira Managed Identity + Key Vault quando aplicável.

A senha do DemoUser existe apenas para demonstrar o fluxo de login. Em produção, senha deve ser armazenada com hash forte e salt por um provedor de identidade/Identity.


Entre no projeto da API:

cd src/Estoque.Api

dotnet user-secrets init

Cadastre uma chave JWT forte para DESENVOLVIMENTO:

dotnet user-secrets set "Jwt:Key" "MINHA-CHAVE-DE-DESENVOLVIMENTO-COM-PELO-MENOS-32-CARACTERES"

Cadastre usuário de demonstração:

dotnet user-secrets set "DemoUser:Username" "admin"
dotnet user-secrets set "DemoUser:Password" "Admin@123"
dotnet user-secrets set "DemoUser:Role" "Admin"

Confira:

dotnet user-secrets list

Volte para a raiz:

cd ../..

CONCEITO:
- appsettings.json = configuração não sensível.
- appsettings.Development.json = configuração específica de desenvolvimento.
- User Secrets = segredo local do desenvolvedor.
- Variáveis de ambiente / Key Vault = melhores opções para ambientes reais.

============================================================
6. CRIAR O SERVIÇO DE TOKEN JWT
============================================================

O QUE É O JWT NESTE EXEMPLO?
Após o login, a API cria um token assinado contendo informações do usuário. Nas próximas requisições, o cliente envia esse token no cabeçalho Authorization: Bearer <token>.

POR QUE UM SERVIÇO SEPARADO?
Gerar token é uma responsabilidade específica. Se colocarmos toda a lógica dentro do controller, ele cresce e fica difícil de testar/manter. A interface ITokenService permite depender de uma abstração.

ATENÇÃO: o payload de um JWT pode ser decodificado por qualquer pessoa que tenha o token. Portanto, nunca coloque senha ou segredo dentro das claims.


Crie:
src/Estoque.Api/Auth/ITokenService.cs

CONTEÚDO:

namespace Estoque.Api.Auth;

// A interface descreve O QUE precisamos fazer sem acoplar quem usa o serviço
// aos detalhes de COMO um JWT é montado.
public interface ITokenService
{
    // Recebe os dados necessários para formar a identidade e devolve o JWT
    // serializado como string, pronto para ser enviado ao cliente.
    string GenerateToken(
        string username,
        string role,
        IEnumerable<string> permissions);
}

------------------------------------------------------------

Crie:
src/Estoque.Api/Auth/TokenService.cs

CONTEÚDO:

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Estoque.Api.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Estoque.Api.Auth;

public sealed class TokenService : ITokenService
{
    // Mantemos as configurações necessárias para gerar o token.
    private readonly JwtSettings _settings;

    // IOptions<JwtSettings> é resolvido pela injeção de dependência.
    public TokenService(IOptions<JwtSettings> options)
    {
        // Value contém a seção Jwt já convertida para JwtSettings.
        _settings = options.Value;
    }

    public string GenerateToken(
        string username,
        string role,
        IEnumerable<string> permissions)
    {
        // Claims são informações sobre a identidade.
        // Não coloque informações secretas aqui: o payload pode ser decodificado.
        var claims = new List<Claim>
        {
            // "sub" = subject. Identifica o principal sujeito do token.
            new(JwtRegisteredClaimNames.Sub, username),

            // Nome único no exemplo didático. Em produção, normalmente use um ID estável.
            new(JwtRegisteredClaimNames.UniqueName, username),

            // ClaimTypes.Name facilita acessar context.User.Identity.Name/FindFirst.
            new(ClaimTypes.Name, username),

            // Role será usada por [Authorize(Roles = "Admin")].
            new(ClaimTypes.Role, role)
        };

        // Cada permissão vira uma claim separada.
        // Ex.: permission=estoque.read e permission=estoque.write.
        claims.AddRange(
            permissions.Select(permission => new Claim("permission", permission))
        );

        // HMAC usa a mesma chave para assinar e validar.
        // Convertendo a string configurada para bytes criamos a chave criptográfica.
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.Key)
        );

        // Define chave + algoritmo de assinatura.
        // A assinatura impede que alguém altere claims sem invalidar o token.
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        // Agora montamos o token completo com emissor, público, claims e expiração.
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
            signingCredentials: credentials
        );

        // WriteToken serializa o objeto para o formato compacto xxxxx.yyyyy.zzzzz.
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

============================================================
7. CONTRATOS DE LOGIN
============================================================

CONTRATO HTTP é o formato que a API promete receber ou devolver. Usamos records porque esses objetos carregam dados e não precisam de comportamento complexo.

Separar contrato de entidade de domínio evita acoplar a API ao modelo interno. Se amanhã o domínio mudar, o contrato público não precisa mudar automaticamente. Isso também evita o problema de over-posting, quando o cliente consegue enviar campos internos que nunca deveriam ser editáveis.


Crie:
src/Estoque.Api/Contracts/LoginRequest.cs

CONTEÚDO:

namespace Estoque.Api.Contracts;

// Request = aquilo que o cliente envia para a API.
// record é adequado para um objeto simples de transporte de dados.
public sealed record LoginRequest(
    string Username,
    string Password);

------------------------------------------------------------

Crie:
src/Estoque.Api/Contracts/LoginResponse.cs

CONTEÚDO:

namespace Estoque.Api.Contracts;

// Response = aquilo que a API devolve após autenticação bem-sucedida.
public sealed record LoginResponse(
    // Token que será usado nas próximas requisições.
    string AccessToken,

    // Padrão HTTP utilizado no header Authorization: Bearer <token>.
    string TokenType,

    // Informa ao cliente a duração configurada neste exemplo.
    int ExpiresInMinutes
);

POR QUE CONTRATOS?
- Não expomos diretamente entidades de domínio.
- A API controla o formato de entrada e saída.
- Podemos evoluir o contrato sem quebrar o domínio.

============================================================
8. AUTH CONTROLLER
============================================================

FLUXO DO LOGIN:
1) Cliente envia username/password.
2) A API valida as credenciais.
3) Define role/permissões.
4) Gera JWT.
5) Retorna token ao cliente.
6) O cliente usa Bearer token nas próximas chamadas.

POR QUE [AllowAnonymous]?
O endpoint de login precisa ser acessível antes de o usuário possuir token. Em uma API com política global de autenticação, essa anotação libera explicitamente esse endpoint.

A comparação simples de senha aqui é APENAS DIDÁTICA. Em produção não compare senha em texto puro nem salve senha em User Secrets como base de usuários.


Crie:
src/Estoque.Api/Controllers/AuthController.cs

CONTEÚDO:

using Estoque.Api.Auth;
using Estoque.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.Api.Controllers;

// [ApiController] ativa comportamentos próprios de API, como binding/validação HTTP.
[ApiController]
// Endpoint base: /api/auth
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;

    public AuthController(
        IConfiguration configuration,
        ITokenService tokenService)
    {
        _configuration = configuration;
        _tokenService = tokenService;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    // Login precisa ser acessível antes de existir uma identidade autenticada.
    [AllowAnonymous]
    public IActionResult Login(LoginRequest request)
    {
        // Para a aula, as credenciais vêm da configuração/User Secrets.
        // Em produção, consulte um Identity Provider ou armazenamento seguro.
        var expectedUsername = _configuration["DemoUser:Username"];
        var expectedPassword = _configuration["DemoUser:Password"];
        var role = _configuration["DemoUser:Role"] ?? "User";

        // Se usuário OU senha estiver incorreto, devolvemos 401.
        // Evitamos dizer qual dos dois está errado para não ajudar enumeração de usuários.
        if (request.Username != expectedUsername ||
            request.Password != expectedPassword)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Credenciais inválidas",
                Detail = "Usuário ou senha inválidos.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        // No exemplo, traduzimos role em permissões. Em um sistema real,
        // isso poderia vir do banco/IdP e ser modelado com muito mais cuidado.
        var permissions = role == "Admin"
            ? new[] { "estoque.read", "estoque.write", "estoque.delete" }
            : new[] { "estoque.read" };

        // Só geramos o token depois que as credenciais foram validadas.
        var token = _tokenService.GenerateToken(
            request.Username,
            role,
            permissions
        );

        return Ok(new LoginResponse(
            token,
            "Bearer",
            60
        ));
    }
}

============================================================
9. JWT + ROLES + CLAIMS + POLICIES
============================================================

PENSE ASSIM:
Role = categoria ampla do usuário. Ex.: Admin.
Claim = característica/fato da identidade. Ex.: permission=estoque.write.
Policy = regra de autorização que o sistema nomeia e reutiliza.

POR QUE POLICY É PODEROSA?
Porque o controller pergunta "este usuário atende CanWriteStock?" em vez de conhecer todos os detalhes da regra. Depois podemos mudar a policy para exigir claim + role + requisito customizado sem alterar todos os endpoints. Isso reduz acoplamento entre endpoint e regra de segurança.


Diferença rápida:

ROLE
- Perfil amplo.
- Exemplo: Admin, Manager, User.
- Uso: [Authorize(Roles = "Admin")]

CLAIM
- Informação dentro da identidade/token.
- Exemplo: permission=estoque.write.
- Uma identidade pode ter várias claims.

POLICY
- Regra de autorização criada pelo sistema.
- Pode combinar Role, Claim e outras condições.
- Exemplo: policy CanWriteStock exige claim permission=estoque.write.

Vamos configurar isso no Program.cs na etapa 20.

EXEMPLOS DE USO:

[Authorize]
=> qualquer usuário autenticado.

[Authorize(Roles = "Admin")]
=> somente role Admin.

[Authorize(Policy = "CanReadStock")]
=> exige permission=estoque.read.

[Authorize(Policy = "CanWriteStock")]
=> exige permission=estoque.write.

============================================================
10. CRIAR UM CONTROLLER DE EXEMPLO PROTEGIDO
============================================================

Este controller demonstra quatro preocupações ao mesmo tempo: autorização, versionamento, consulta e contratos HTTP. A lista estática existe só para tornar o exemplo executável sem banco.

A query segue uma ordem importante:
FILTRAR -> ORDENAR -> CONTAR -> PAGINAR.
Em um banco real, tente manter isso como IQueryable até o ponto de materialização para que o banco execute a consulta, em vez de carregar tudo em memória.

pageSize limitado protege a API contra uma requisição pedindo milhões de registros.


Crie:
src/Estoque.Api/Controllers/StockController.cs

CONTEÚDO:

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/stock")]
public sealed class StockController : ControllerBase
{
    private static readonly List<StockItemResponse> Items =
    [
        new(1, "Notebook", "Eletronicos", 15, 4500m),
        new(2, "Mouse", "Eletronicos", 50, 120m),
        new(3, "Teclado", "Eletronicos", 30, 250m),
        new(4, "Cadeira", "Moveis", 8, 1200m),
        new(5, "Mesa", "Moveis", 10, 900m)
    ];

    [HttpGet]
    [Authorize(Policy = "CanReadStock")]
    public IActionResult Get(
        [FromQuery] string? name,
        [FromQuery] string? category,
        [FromQuery] string sortBy = "id",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Parâmetros de paginação inválidos",
                Detail = "page deve ser >= 1 e pageSize deve estar entre 1 e 100.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        IEnumerable<StockItemResponse> query = Items;

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(x =>
                x.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x =>
                x.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        query = (sortBy.ToLowerInvariant(), sortDirection.ToLowerInvariant()) switch
        {
            ("name", "desc") => query.OrderByDescending(x => x.Name),
            ("name", _)      => query.OrderBy(x => x.Name),
            ("price", "desc") => query.OrderByDescending(x => x.Price),
            ("price", _)       => query.OrderBy(x => x.Price),
            ("quantity", "desc") => query.OrderByDescending(x => x.Quantity),
            ("quantity", _)       => query.OrderBy(x => x.Quantity),
            (_, "desc") => query.OrderByDescending(x => x.Id),
            _ => query.OrderBy(x => x.Id)
        };

        var totalItems = query.Count();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var data = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            page,
            pageSize,
            totalItems,
            totalPages,
            data
        });
    }

    [HttpPost]
    [Authorize(Policy = "CanWriteStock")]
    public IActionResult Create(StockItemRequest request)
    {
        var nextId = Items.Count == 0 ? 1 : Items.Max(x => x.Id) + 1;

        var item = new StockItemResponse(
            nextId,
            request.Name,
            request.Category,
            request.Quantity,
            request.Price
        );

        Items.Add(item);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = item.Id },
            item
        );
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "CanReadStock")]
    public IActionResult GetById(int id)
    {
        var item = Items.FirstOrDefault(x => x.Id == id);

        if (item is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Item não encontrado",
                Detail = $"Nenhum item com id {id} foi encontrado.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete(int id)
    {
        var item = Items.FirstOrDefault(x => x.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        Items.Remove(item);
        return NoContent();
    }
}

public sealed record StockItemRequest(
    string Name,
    string Category,
    int Quantity,
    decimal Price
);

public sealed record StockItemResponse(
    int Id,
    string Name,
    string Category,
    int Quantity,
    decimal Price
);

NOTA:
- A lista em memória é apenas para aula.
- Em uma aplicação real, a consulta ficaria na Application/Infra e usaria banco.

============================================================
11. CORS
============================================================

CORS é uma proteção aplicada pelos navegadores. Ele controla se um JavaScript carregado de uma origem pode chamar outra origem. Origem considera protocolo + host + porta.

CORS NÃO é autenticação e NÃO impede chamadas via curl/Postman. Portanto, nunca trate CORS como mecanismo de segurança da API por si só.

Usamos lista explícita de origens para aplicar menor privilégio: somente front-ends conhecidos recebem permissão do navegador.


O que é CORS?
- Regra do navegador para controlar chamadas entre origens diferentes.
- Exemplo:
  Front-end: http://localhost:4200
  API:       https://localhost:7001
- São origens diferentes.

EVITE em produção:

AllowAnyOrigin()
AllowAnyHeader()
AllowAnyMethod()

quando você souber quais clientes precisam acessar.

Vamos criar uma policy chamada Frontend no Program.cs.

============================================================
12. HTTPS
============================================================

HTTPS usa TLS para proteger os dados em trânsito. JWT assinado não substitui HTTPS. Sem HTTPS, um atacante que intercepte o tráfego pode roubar o token e reutilizá-lo enquanto estiver válido.

UseHttpsRedirection força o cliente a migrar de HTTP para HTTPS quando possível. Em produção, TLS pode terminar no próprio ASP.NET, em reverse proxy, ingress, Application Gateway, load balancer etc.; a arquitetura precisa preservar corretamente os headers encaminhados.


A API deve redirecionar HTTP para HTTPS:

app.UseHttpsRedirection();

Para conferir/corrigir o certificado de desenvolvimento:

# opcional: limpar certificados locais antigos
dotnet dev-certs https --clean

# criar e confiar no certificado local
dotnet dev-certs https --trust

No Linux, o comportamento do --trust depende da distribuição/ambiente.

POR QUE HTTPS?
- Criptografa tráfego cliente-servidor.
- Protege token JWT durante transporte.
- Reduz risco de interceptação.

============================================================
13. RATE LIMITING
============================================================

Rate limiting define quantas requisições um cliente pode fazer em determinado período. Isso reduz abuso, brute force e consumo acidental de recursos.

Usamos um limite global mais amplo e um limite de login mais restritivo porque tentativa de autenticação é um alvo comum de força bruta.

Em produção atrás de proxy/load balancer, não confie cegamente em RemoteIpAddress: configure Forwarded Headers corretamente para obter o IP real do cliente e avalie chaves de partição por usuário/API key quando fizer sentido.


Objetivo:
- Evitar abuso da API.
- Reduzir brute force.
- Reduzir excesso de requisições.
- Ajudar a proteger recursos.

Vamos configurar no Program.cs:
- 100 requisições por minuto por IP para a API em geral.
- Uma policy mais restritiva para login.

============================================================
14. HEADERS DE SEGURANÇA
============================================================

Headers de segurança instruem clientes, especialmente navegadores, a adotar comportamentos mais seguros. Mesmo em uma API JSON, ter uma postura defensiva consistente ajuda.

O middleware é usado porque queremos aplicar os headers centralmente em várias respostas, sem repetir código em cada controller.


Crie:
src/Estoque.Api/Middleware/SecurityHeadersMiddleware.cs

CONTEÚDO:

namespace Estoque.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Este middleware roda para cada requisição que passa pelo pipeline.
        // Em vez de repetir headers em todos os controllers, centralizamos aqui.
        context.Response.Headers.TryAdd(
            "X-Content-Type-Options",
            "nosniff"
        );

        context.Response.Headers.TryAdd(
            "X-Frame-Options",
            "DENY"
        );

        context.Response.Headers.TryAdd(
            "Referrer-Policy",
            "no-referrer"
        );

        context.Response.Headers.TryAdd(
            "Permissions-Policy",
            "camera=(), microphone=(), geolocation=()"
        );

        // Chama o próximo middleware/endpoint. Sem isso, a requisição pararia aqui.
        await _next(context);
    }
}

OBSERVAÇÃO:
APIs normalmente não renderizam HTML, mas headers seguros ajudam a definir uma postura defensiva.
Para aplicações web completas, CSP (Content-Security-Policy) deve ser desenhada conforme o front-end.

============================================================
15. AUDITORIA
============================================================

LOG TÉCNICO e LOG DE AUDITORIA não são exatamente a mesma coisa. Log técnico ajuda a investigar funcionamento/erro; auditoria registra ações relevantes de usuários e sistemas.

O middleware mede duração e registra identidade, rota, método, status, IP e TraceId. Ele fica depois da autenticação no pipeline porque assim context.User já foi preenchido.

Nunca transforme auditoria em vazamento de dados: registre o mínimo necessário e defina retenção/acesso.


Auditoria responde perguntas como:
- Quem fez a chamada?
- Qual endpoint foi acessado?
- Qual método HTTP?
- Qual status foi retornado?
- Qual IP originou a chamada?
- Quanto tempo levou?

Crie:
src/Estoque.Api/Middleware/AuditMiddleware.cs

CONTEÚDO:

using System.Diagnostics;
using System.Security.Claims;

namespace Estoque.Api.Middleware;

public sealed class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(
        RequestDelegate next,
        ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var username =
                context.User.FindFirstValue(ClaimTypes.Name)
                ?? "anonymous";

            _logger.LogInformation(
                "AUDIT User={User} Method={Method} Path={Path} Status={Status} IP={IP} DurationMs={DurationMs} TraceId={TraceId}",
                username,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                context.Connection.RemoteIpAddress?.ToString(),
                stopwatch.ElapsedMilliseconds,
                context.TraceIdentifier
            );
        }
    }
}

NUNCA grave em log:
- Senha.
- Token JWT completo.
- Número de cartão.
- Segredos.
- Dados pessoais desnecessários.

============================================================
16. TRATAMENTO GLOBAL DE ERROS + PROBLEM DETAILS
============================================================

Sem um padrão, cada endpoint pode devolver um formato diferente de erro. Isso obriga o front-end a criar tratamentos especiais. ProblemDetails segue o formato RFC 7807/9457 e cria respostas previsíveis.

O middleware global captura exceções não tratadas, registra detalhes internamente e devolve uma mensagem segura ao cliente. O TraceId conecta a resposta do cliente ao log do servidor.

Nunca devolva stack trace ou ex.Message indiscriminadamente em produção.


Vamos usar ProblemDetails para padronizar erros HTTP.

O formato fica semelhante a:

{
  "type": "https://httpstatuses.com/500",
  "title": "Erro interno",
  "status": 500,
  "detail": "Ocorreu um erro inesperado.",
  "instance": "/api/v1/stock",
  "traceId": "..."
}

Crie:
src/Estoque.Api/Middleware/GlobalExceptionMiddleware.cs

CONTEÚDO:

using Microsoft.AspNetCore.Mvc;

namespace Estoque.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled error. TraceId={TraceId}",
                context.TraceIdentifier
            );

            var problem = new ProblemDetails
            {
                Title = "Erro interno do servidor",
                Detail = "Ocorreu um erro inesperado.",
                Status = StatusCodes.Status500InternalServerError,
                Instance = context.Request.Path
            };

            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode =
                StatusCodes.Status500InternalServerError;

            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}

POR QUE NÃO DEVOLVER ex.Message?
- Pode vazar detalhes internos.
- Pode revelar SQL, paths, nomes de tabelas ou estrutura da aplicação.

============================================================
17. HEALTH CHECK CUSTOMIZADO
============================================================

Health check não é uma tela de monitoramento; é um endpoint simples para plataforma/orquestrador verificar se a aplicação está funcionando.

Em sistemas reais, diferencie readiness (está pronta para receber tráfego?) de liveness (processo está vivo?). Dependências como banco podem entrar em readiness sem necessariamente causar restart por liveness.


Health check serve para plataforma/orquestrador descobrir se a API está saudável.

Crie:
src/Estoque.Api/Health/SelfHealthCheck.cs

CONTEÚDO:

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Estoque.Api.Health;

public sealed class SelfHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            HealthCheckResult.Healthy("API está respondendo.")
        );
    }
}

Em produção você também adicionaria checks de:
- Banco.
- Redis.
- RabbitMQ.
- Dependências externas críticas.

============================================================
18. MÉTRICAS COM PROMETHEUS
============================================================

Logs contam eventos individuais. Métricas agregam números ao longo do tempo. Ex.: taxa de requisições, duração, erros e quantidade de itens criados.

Counter só cresce e é adequado para totais acumulados. O Prometheus coleta /metrics periodicamente. Não use labels com valores de alta cardinalidade, como userId único, porque isso pode explodir a quantidade de séries.


Com prometheus-net teremos:
- métricas HTTP automáticas.
- endpoint /metrics.
- possibilidade de criar métricas customizadas.

Crie:
src/Estoque.Api/Metrics/StockMetrics.cs

CONTEÚDO:

using Prometheus;

namespace Estoque.Api.Metrics;

public static class StockMetrics
{
    public static readonly Counter CreatedItems = Metrics.CreateCounter(
        "estoque_items_created_total",
        "Quantidade total de itens criados no estoque."
    );
}

Depois, no Create do StockController, antes do return, adicione:

Estoque.Api.Metrics.StockMetrics.CreatedItems.Inc();

Então o final do método Create ficará aproximadamente:

Items.Add(item);
Estoque.Api.Metrics.StockMetrics.CreatedItems.Inc();

return CreatedAtAction(...);

============================================================
19. SWAGGER COM JWT + VERSIONAMENTO
============================================================

OpenAPI é a especificação do contrato da API. Swagger UI é uma interface que lê essa especificação e permite explorar/testar endpoints.

Adicionar Bearer ao Swagger NÃO protege a API; apenas ensina a interface a enviar o header Authorization. A proteção real continua sendo AddAuthentication/AddAuthorization + [Authorize].

Geramos um documento por versão para o consumidor saber qual contrato está usando.


Crie:
src/Estoque.Api/Swagger/ConfigureSwaggerOptions.cs

CONTEÚDO:

using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Estoque.Api.Swagger;

public sealed class ConfigureSwaggerOptions
    : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(
        IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                new OpenApiInfo
                {
                    Title = "Estoque API",
                    Version = description.ApiVersion.ToString(),
                    Description = "API de exemplo para aula de back-end seguro e pronto para operação."
                }
            );
        }

        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Informe somente o token JWT."
            }
        );

        options.AddSecurityRequirement(
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            }
        );
    }
}

============================================================
20. SUBSTITUIR O PROGRAM.CS
============================================================

O Program.cs é o ponto de composição da aplicação. Primeiro registramos serviços no container de DI; depois construímos o app; por fim configuramos o pipeline HTTP.

DUAS FASES PARA O ALUNO MEMORIZAR:
1) builder.Services... = o que a aplicação SABE/PODE usar.
2) app.Use... / app.Map... = como cada requisição PASSA pelo pipeline.

A ORDEM DOS MIDDLEWARES IMPORTA. Authentication precisa executar antes de Authorization, por exemplo, porque autorização depende da identidade criada pela autenticação.


Agora substitua o conteúdo de:
src/Estoque.Api/Program.cs

LEITURA DO PROGRAM.CS ANTES DE COPIAR:
- Add... registra uma capacidade/serviço no container.
- Use... coloca um middleware no pipeline.
- Map... publica um endpoint.
- Configure<T> faz binding de configuração para uma classe.
- AddSingleton cria uma única instância durante toda a vida da aplicação.
- AddAuthentication configura COMO validar identidade.
- AddAuthorization configura as REGRAS que usam essa identidade.

por:

using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Estoque.Api.Auth;
using Estoque.Api.Configuration;
using Estoque.Api.Health;
using Estoque.Api.Middleware;
using Estoque.Api.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// 1) CONTROLLERS + PADRÃO DE ERRO
// ---------------------------------------------------------
builder.Services.AddControllers();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;
    };
});

// ---------------------------------------------------------
// 2) CONFIGURAÇÃO JWT
// ---------------------------------------------------------
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName)
);

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException("Configuração Jwt ausente.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException(
        "Jwt:Key não configurada. Use User Secrets ou variável de ambiente."
    );
}

if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key deve possuir pelo menos 32 bytes para este exemplo."
    );
}

builder.Services.AddSingleton<ITokenService, TokenService>();

// ---------------------------------------------------------
// 3) AUTENTICAÇÃO JWT
// ---------------------------------------------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Key)
            ),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// ---------------------------------------------------------
// 4) AUTORIZAÇÃO: POLICIES
// ---------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "CanReadStock",
        policy => policy.RequireClaim("permission", "estoque.read")
    );

    options.AddPolicy(
        "CanWriteStock",
        policy => policy.RequireClaim("permission", "estoque.write")
    );

    options.AddPolicy(
        "CanDeleteStock",
        policy => policy
            .RequireRole("Admin")
            .RequireClaim("permission", "estoque.delete")
    );
});

// ---------------------------------------------------------
// 5) CORS
// ---------------------------------------------------------
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ---------------------------------------------------------
// 6) RATE LIMITING
// ---------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext =>
        {
            var partitionKey =
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }
            );
        }
    );

    options.AddFixedWindowLimiter("LoginPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.AutoReplenishment = true;
    });
});

// ---------------------------------------------------------
// 7) VERSIONAMENTO
// ---------------------------------------------------------
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;

        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// ---------------------------------------------------------
// 8) SWAGGER
// ---------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<
    IConfigureOptions<SwaggerGenOptions>,
    ConfigureSwaggerOptions>();

// ---------------------------------------------------------
// 9) HEALTH CHECKS
// ---------------------------------------------------------
builder.Services
    .AddHealthChecks()
    .AddCheck<SelfHealthCheck>("self");

var app = builder.Build();

// ---------------------------------------------------------
// PIPELINE DE MIDDLEWARES
// A ORDEM IMPORTA!
// ---------------------------------------------------------

// Captura exceções inesperadas o mais cedo possível.
// Quanto mais cedo estiver no pipeline, maior a chance de capturar falhas dos componentes seguintes.
app.UseMiddleware<GlobalExceptionMiddleware>();

// Headers defensivos.
app.UseMiddleware<SecurityHeadersMiddleware>();

// Redireciona HTTP -> HTTPS.
app.UseHttpsRedirection();

// Swagger somente em Development.
if (app.Environment.IsDevelopment())
{
    var versionProvider = app.Services
        .GetRequiredService<IApiVersionDescriptionProvider>();

    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        foreach (var description in versionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant()
            );
        }
    });
}

// CORS antes dos endpoints.
app.UseCors("Frontend");

// Rate limiting.
app.UseRateLimiter();

// Autenticação lê/valida o Bearer token e preenche HttpContext.User.
app.UseAuthentication();

// Autorização usa HttpContext.User para verificar [Authorize], Roles e Policies.
// Por isso ela vem DEPOIS de UseAuthentication().
app.UseAuthorization();

// Auditoria depois da autenticação, para conseguir registrar o usuário.
app.UseMiddleware<AuditMiddleware>();

// Métricas automáticas de HTTP.
app.UseHttpMetrics();

app.MapControllers();

// Health endpoint.
app.MapHealthChecks("/health").AllowAnonymous();

// Endpoint de métricas Prometheus.
app.MapMetrics("/metrics").AllowAnonymous();

app.Run();

============================================================
21. APLICAR RATE LIMIT ESPECÍFICO NO LOGIN
============================================================

O login merece uma regra mais rígida que os endpoints comuns. O atributo/policy aplica a proteção somente à rota de autenticação, além do limite global. Isso ilustra "defesa em profundidade": múltiplas camadas de proteção com objetivos diferentes.


Abra AuthController.cs.

Adicione:

using Microsoft.AspNetCore.RateLimiting;

No método Login, adicione:

[EnableRateLimiting("LoginPolicy")]

Ele deve ficar assim:

[HttpPost("login")]
[AllowAnonymous]
[EnableRateLimiting("LoginPolicy")]
public IActionResult Login(LoginRequest request)
{
    ...
}

============================================================
22. COMPILAR
============================================================

Compile agora para descobrir erros antes dos testes HTTP. Erro de build é mais simples de resolver do que misturar problema de compilação com problema de configuração em runtime.


Na raiz:

dotnet restore
dotnet build

Se houver erro, resolva antes de continuar.

============================================================
23. RODAR A API
============================================================

Ao executar dotnet run, observe as URLs exibidas. O profile de launchSettings pode escolher portas diferentes. Use a URL HTTPS para os testes de segurança.

Se a aplicação encerrar na inicialização dizendo Jwt:Key ausente, isso é intencional: preferimos falhar cedo em vez de iniciar com segurança quebrada.


Execute:

dotnet run --project src/Estoque.Api

O terminal mostrará as URLs, por exemplo:

http://localhost:5000
https://localhost:7000

Use a URL HTTPS mostrada pelo SEU terminal.

Swagger normalmente:

https://localhost:SUA_PORTA/swagger

Health:

https://localhost:SUA_PORTA/health

Metrics:

https://localhost:SUA_PORTA/metrics

============================================================
24. TESTAR LOGIN
============================================================

O objetivo é validar o primeiro elo da cadeia: credencial -> token. Depois decodifique visualmente o JWT apenas para fins de aprendizado e observe sub, role e permission. Lembre: decodificar não significa validar assinatura.


No Swagger:

POST /api/auth/login

Body:

{
  "username": "admin",
  "password": "Admin@123"
}

Resposta aproximada:

{
  "accessToken": "eyJ...",
  "tokenType": "Bearer",
  "expiresInMinutes": 60
}

Copie SOMENTE o accessToken.

Clique em Authorize no Swagger.
Cole o token.

Como configuramos Scheme=Bearer, normalmente o Swagger adiciona o prefixo Bearer.

============================================================
25. TESTAR AUTORIZAÇÃO
============================================================

Teste três situações diferentes: sem token -> 401; token válido sem permissão -> 403; token válido com permissão -> sucesso. Essa diferença é fundamental para entender autenticação x autorização.


GET /api/v1/stock

Sem token:
=> 401 Unauthorized

Com token válido e permission estoque.read:
=> 200 OK

DELETE /api/v1/stock/1

Com role Admin:
=> permitido.

A diferença principal:

401 = você NÃO está autenticado ou token é inválido.
403 = você está autenticado, mas NÃO tem permissão.

============================================================
26. TESTAR FILTROS, ORDENAÇÃO E PAGINAÇÃO
============================================================

Esses parâmetros evitam endpoints que sempre devolvem todos os registros. Filtro reduz conjunto; ordenação torna resultado previsível; paginação controla volume. Em banco real, crie índices de acordo com padrões de consulta e evite ordenação arbitrária em qualquer campo sem whitelist.


Exemplos:

GET /api/v1/stock?name=mouse

GET /api/v1/stock?category=Eletronicos

GET /api/v1/stock?sortBy=price&sortDirection=desc

GET /api/v1/stock?page=1&pageSize=2

Combinando:

GET /api/v1/stock?category=Eletronicos&sortBy=price&sortDirection=desc&page=1&pageSize=2

BOA PRÁTICA:
- Nunca devolva milhões de registros em um GET.
- Sempre pense em paginação para listas grandes.
- Valide campos permitidos para ordenação.

============================================================
27. TESTAR CORS
============================================================

O melhor teste de CORS é feito a partir de um navegador/front-end rodando em uma origem permitida e outra não permitida. Postman/curl não reproduzem a política do navegador da mesma maneira, por isso podem "funcionar" mesmo quando o browser bloquearia.


A policy permite as origens definidas em:

Cors:AllowedOrigins

Por exemplo:
- http://localhost:4200
- http://localhost:5173

IMPORTANTE:
CORS não é mecanismo de autenticação.
CORS é uma política aplicada pelo navegador.
Seu back-end ainda precisa de autenticação e autorização.

============================================================
28. TESTAR RATE LIMIT
============================================================

Faça várias chamadas em sequência e observe HTTP 429 Too Many Requests. Esse teste mostra que o servidor está recusando excesso de tráfego antes que ele consuma indefinidamente recursos internos.


Faça muitas chamadas rapidamente.

Ao exceder o limite, a API deve responder:

HTTP 429 Too Many Requests

Login está mais restrito que os endpoints gerais.

============================================================
29. TESTAR AUDITORIA
============================================================

Observe o console/log após chamar endpoints autenticados. Verifique User, Method, Path, Status, DurationMs e TraceId. Depois discuta com os alunos quais dados NÃO deveriam ser registrados.


Faça uma chamada autenticada.

No terminal, procure logs semelhantes a:

AUDIT User=admin Method=GET Path=/api/v1/stock Status=200 IP=... DurationMs=... TraceId=...

O TraceId é muito importante para suporte e observabilidade.

============================================================
30. TESTAR HEALTH CHECK
============================================================

Um retorno saudável confirma que o endpoint está disponível. Em produção, Kubernetes/Container Apps/App Service podem consultar esse endpoint para decisões de roteamento e recuperação.


Acesse:

GET /health

Esperado:

HTTP 200

Em Kubernetes, por exemplo, esse endpoint pode ser usado em probes.

============================================================
31. TESTAR MÉTRICAS
============================================================

Abra /metrics e procure métricas HTTP e o counter customizado. Crie um item e consulte novamente para perceber o contador aumentando. Isso ajuda a diferenciar telemetria de negócio de simples log textual.


Acesse:

GET /metrics

Procure métricas como:

http_requests_received_total
http_request_duration_seconds

Após criar item no estoque, procure:

estoque_items_created_total

Prometheus pode coletar esse endpoint e Grafana pode criar dashboards.

============================================================
32. PROTEÇÕES CONTRA VULNERABILIDADES COMUNS
============================================================

Segurança não é uma funcionalidade única chamada "JWT". É um conjunto de controles: validação de entrada, parametrização de SQL, autenticação, autorização, TLS, segredo fora do código, limite de requisições, logs seguros e atualização de dependências.

A ideia desta seção é revisar ameaças conhecidas e conectar cada uma com um controle implementado.


CHECKLIST:

[ ] 1. Injection
- Nunca concatenar SQL com dados do usuário.
- Com EF Core, prefira LINQ parametrizado.
- Com Dapper, use parâmetros.

ERRADO:

$"SELECT * FROM Users WHERE Name = '{name}'"

MELHOR:

connection.Query<User>(
    "SELECT * FROM Users WHERE Name = @Name",
    new { Name = name }
);

[ ] 2. Broken Authentication
- Token com expiração.
- Chave forte.
- HTTPS.
- Limite de tentativas de login.
- Preferir provedor de identidade em produção.

[ ] 3. Broken Access Control
- Não basta [Authorize].
- Proteja cada operação com Role/Claim/Policy conforme necessidade.

[ ] 4. Sensitive Data Exposure
- Não logar senha/token.
- Não commitar segredo.
- HTTPS.
- Key Vault/secret manager em produção.

[ ] 5. Security Misconfiguration
- Swagger de preferência restrito/desabilitado em produção.
- Não retornar stack trace ao cliente.
- CORS com origens conhecidas.
- Headers seguros.

[ ] 6. Mass Assignment / Over-posting
- Use Request DTO/Contract.
- Não receba entidade do banco diretamente no controller.

[ ] 7. DoS / abuso
- Rate limiting.
- Paginação.
- Timeout para chamadas externas.
- Limite de tamanho de payload quando necessário.

============================================================
33. VERSIONAMENTO DE API
============================================================

APIs públicas evoluem. Se você altera um contrato incompatível sem versão, pode quebrar clientes existentes. Versionamento por URL deixa a versão muito visível: /api/v1/stock. Não crie v2 para qualquer mudança pequena; use uma nova versão quando houver mudança incompatível de contrato/comportamento.


V1 atual:

/api/v1/stock

Para demonstrar uma V2, crie um novo controller ou adicione outra versão.

Exemplo simples:

[ApiController]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/stock")]
public sealed class StockV2Controller : ControllerBase
{
    [HttpGet("summary")]
    public IActionResult Summary()
    {
        return Ok(new
        {
            version = "2.0",
            message = "Contrato novo sem quebrar a V1"
        });
    }
}

CONCEITO:
- Não quebre clientes antigos sem necessidade.
- Evolua contratos usando nova versão quando a mudança for incompatível.

============================================================
34. PADRÃO DE CONTRATOS HTTP
============================================================

Use status HTTP semanticamente: 200 leitura/sucesso comum, 201 criação, 204 sucesso sem corpo, 400 entrada inválida, 401 não autenticado, 403 autenticado sem acesso, 404 recurso inexistente, 409 conflito, 429 excesso, 500 falha inesperada.

Consistência reduz complexidade no consumidor da API.


Sugestão de códigos:

GET coleção bem-sucedido       -> 200 OK
GET item encontrado            -> 200 OK
POST criado                    -> 201 Created
PUT/PATCH bem-sucedido         -> 200 OK ou 204 No Content
DELETE bem-sucedido            -> 204 No Content
Entrada inválida               -> 400 Bad Request
Sem autenticação               -> 401 Unauthorized
Sem permissão                  -> 403 Forbidden
Não encontrado                 -> 404 Not Found
Conflito                       -> 409 Conflict
Rate limit                     -> 429 Too Many Requests
Erro inesperado                -> 500 Internal Server Error

============================================================
35. O QUE DEVE FICAR EM CADA CAMADA
============================================================

Use esta seção para reforçar a arquitetura. Segurança HTTP, Swagger e middlewares pertencem naturalmente à API. Casos de uso e interfaces ficam em Application. Regra de negócio pura fica em Domain. Banco/cache/mensageria ficam em Infra.

A arquitetura existe para controlar dependências, não para criar pastas por estética.


ESTOQUE.API
- Controllers.
- Autenticação/Autorização.
- Middlewares HTTP.
- Swagger.
- CORS.
- Rate limit.
- Health endpoints.
- Entrada/saída HTTP.

ESTOQUE.APPLICATION
- Casos de uso.
- Commands/Queries se usar CQRS.
- Interfaces de repositório.
- DTOs internos quando fizer sentido.
- Regras de orquestração.

ESTOQUE.DOMAIN
- Entidades.
- Value Objects.
- Regras de negócio centrais.
- Exceções/erros de domínio, se aplicável.
- Sem dependência de banco ou ASP.NET.

ESTOQUE.INFRA
- EF Core / Dapper.
- Implementação de repositórios.
- Integrações externas.
- Cache.
- Mensageria.
- Persistência.

REGRA MENTAL:
O Domain não deve conhecer a Infra nem a API.

============================================================
36. REVISÃO DE QUALIDADE
============================================================

Antes de dizer "está pronto", verifique segurança, comportamento, observabilidade e operação. Um endpoint que funciona no Swagger não significa que o backend está pronto para produção. Checklist transforma critérios implícitos em uma revisão repetível.


Antes de considerar o back-end pronto:

[ ] dotnet build sem erro
[ ] dotnet test passando
[ ] warnings analisados
[ ] endpoint de login testado
[ ] endpoints protegidos retornam 401 sem token
[ ] endpoints com policy retornam 403 sem permissão
[ ] HTTPS funcionando
[ ] CORS restritivo
[ ] segredos fora do Git
[ ] Swagger documentado
[ ] versionamento funcionando
[ ] filtros e ordenação validados
[ ] paginação limitada
[ ] erros padronizados
[ ] stack trace não exposto
[ ] logs/auditoria sem dados sensíveis
[ ] /health funcionando
[ ] /metrics funcionando
[ ] rate limit devolvendo 429 quando excedido
[ ] contratos de request/response separados do domínio

============================================================
37. ARQUIVO .GITIGNORE E SEGREDOS
============================================================

O objetivo é impedir que artefatos locais e segredos sejam versionados acidentalmente. Mesmo assim, .gitignore não substitui disciplina: se um segredo já foi commitado, removê-lo do arquivo atual não apaga o histórico. Nesse caso, rotacione/revogue o segredo e trate o histórico adequadamente.


Confira se você tem .gitignore.

Se não tiver:

dotnet new gitignore

User Secrets NÃO ficam dentro do projeto, então não devem ir para o Git.

Antes do commit:

git status

git diff

PROCURE por:
- senhas
- tokens
- chaves
- connection strings

============================================================
38. COMANDOS ÚTEIS PARA A AULA
============================================================

Esta seção serve como "cola" operacional. O aluno deve conseguir validar build, executar, listar secrets e verificar dependências sem procurar no restante do documento.


Build:

dotnet build

Run:

dotnet run --project src/Estoque.Api

Limpar:

dotnet clean

Restaurar:

dotnet restore

Ver pacotes:

dotnet list src/Estoque.Api package

Ver segredos locais:

cd src/Estoque.Api
dotnet user-secrets list
cd ../..

============================================================
39. ORDEM RECOMENDADA PARA DEMONSTRAR EM AULA
============================================================

A sequência foi pensada pedagogicamente: primeiro identidade/autorização, depois proteção do transporte e tráfego, depois contrato/versionamento e por último observabilidade/prontidão. Assim cada conceito se apoia no anterior.


1. Mostrar arquitetura em quatro camadas.
2. Mostrar API funcionando SEM segurança.
3. Explicar autenticação x autorização.
4. Criar JWT.
5. Decodificar JWT e mostrar claims.
6. Colocar [Authorize].
7. Demonstrar Role.
8. Demonstrar Claim.
9. Demonstrar Policy.
10. Mostrar 401 x 403.
11. Mover segredo para User Secrets.
12. Explicar HTTPS.
13. Configurar CORS.
14. Configurar Rate Limiting.
15. Adicionar headers seguros.
16. Adicionar middleware global de erro.
17. Adicionar auditoria.
18. Configurar Swagger + Bearer.
19. Configurar versão /api/v1.
20. Demonstrar filtro/ordenação/paginação.
21. Mostrar ProblemDetails.
22. Mostrar /health.
23. Mostrar /metrics.
24. Revisar checklist de produção.

============================================================
40. RESUMO FINAL
============================================================

Quando terminar, a API terá:

SEGURANÇA
- JWT.
- Roles.
- Claims.
- Policies.
- HTTPS.
- CORS.
- Segredos fora do código.
- Rate limiting.
- Headers defensivos.

GOVERNANÇA E SUPORTE
- Auditoria.
- TraceId.
- Erros padronizados.

CONTRATO E EVOLUÇÃO
- Swagger/OpenAPI.
- Versionamento.
- Request/Response Contracts.
- Filtros.
- Ordenação.
- Paginação.

OPERAÇÃO
- Health Check.
- Métricas Prometheus.
- Logs.

ARQUITETURA
- API responsável por HTTP.
- Application responsável por casos de uso.
- Domain responsável pelas regras centrais.
- Infrastructure responsável por tecnologia/persistência.

FIM DO GUIA.


============================================================
40. RESUMO MENTAL DA REQUISIÇÃO COMPLETA
============================================================

Imagine que o Angular/React chama:
GET https://localhost:7001/api/v1/stock?page=1&pageSize=10
Authorization: Bearer <jwt>

O fluxo simplificado é:

1) HTTPS protege o tráfego em trânsito.
2) CORS decide se um navegador de determinada origem pode acessar a resposta.
3) Rate Limiter verifica se o cliente excedeu o limite.
4) JWT Bearer Authentication lê o header Authorization.
5) A assinatura, issuer, audience e expiração do token são validados.
6) Se válido, HttpContext.User recebe as claims.
7) Authorization avalia a policy CanReadStock.
8) A policy procura permission=estoque.read.
9) Se autorizado, o controller executa.
10) O controller filtra/ordena/pagina os dados do exemplo.
11) O ASP.NET serializa a resposta em JSON.
12) AuditMiddleware registra quem chamou, status e duração.
13) Prometheus registra métricas HTTP.
14) Se ocorrer uma exceção inesperada, o middleware global devolve ProblemDetails seguro.

Esse é o principal fluxo que o aluno deve conseguir explicar ao final da aula.

============================================================
41. O QUE EU MUDARIA ANTES DE PRODUÇÃO
============================================================

Este laboratório ensina os conceitos, mas antes de produção eu revisaria:

- Trocar DemoUser por ASP.NET Core Identity, Microsoft Entra ID, Keycloak, Auth0 ou outro IdP adequado.
- Preferir assinatura assimétrica/IdP quando a arquitetura exigir múltiplos serviços validando tokens.
- Implementar refresh token apenas se o desenho de autenticação exigir, com rotação/revogação.
- Guardar segredos em serviço apropriado e usar identidade gerenciada quando possível.
- Persistir auditoria em destino centralizado/protegido e definir retenção.
- Configurar forwarded headers ao rodar atrás de proxy/ingress.
- Separar health probes de liveness e readiness.
- Adicionar health checks das dependências críticas.
- Validar DTOs de entrada com DataAnnotations ou FluentValidation.
- Usar EF Core/Dapper com consultas parametrizadas.
- Criar testes unitários e de integração para autenticação/autorização.
- Revisar CORS por ambiente.
- Não publicar Swagger externamente sem decisão explícita.
- Restringir /metrics para rede/monitoramento quando aplicável.
- Adicionar OpenTelemetry/Application Insights conforme a plataforma.
- Executar análise de dependências e manter pacotes atualizados.
- Fazer threat modeling e revisão OWASP API Security Top 10.

FIM DO GUIA COMENTADO.
