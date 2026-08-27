using Microsoft.OpenApi;
using SystemOrder.Api.Middlewares;
using SystemOrder.Application.Interfaces;
using SystemOrder.Application.Services;
using SystemOrder.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "System Order API",
        Version = "v1",
        Description = """
            API responsável pelo gerenciamento de pedidos.

            Funcionalidades:
            - Criação de pedidos
            - Consulta de pedidos
            - Atualização de pedidos
            - Exclusão de pedidos
            - Cache para consultas

            Autenticação:
            Todos os endpoints exigem uma API Key enviada
            através do header X-API-Key.
            """
    });

    var xmlFile =
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";

    var xmlPath =
        Path.Combine(AppContext.BaseDirectory, xmlFile);

    options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition(
        "ApiKey",
        new OpenApiSecurityScheme
        {
            Name = "X-API-Key",
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Description = "Informe a API Key para acessar os endpoints."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecuritySchemeReference(
                    "ApiKey",
                    document)
            ] = []
        });
});

builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "System Order API v1");

        options.DocumentTitle = "System Order API";
    });
}

app.UseHttpsRedirection();

app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

app.Run();