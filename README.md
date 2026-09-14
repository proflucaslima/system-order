mkdir stocksystem

cd stocksystem

dotnet new sln -n StockSystem

mkdir src

dotnet new classlib -n Estoque.Domain -o src/Estoque.Domain -f net9.0

dotnet new classlib -n Estoque.Application -o src/Estoque.Application -f 
net9.0

dotnet new classlib -n Estoque.Infra -o src/Estoque.Infra -f net9.0

dotnet new webapi -n Estoque.Api -o src/Estoque.Api -f net9.0 --use
controller





dotnet sln add src/Estoque.Domain/Estoque.Domain.csproj
dotnet sln add src/Estoque.Application/Estoque.Application.csproj
dotnet sln add src/Estoque.Infra/Estoque.Infra.csproj
dotnet sln add src/Estoque.Api/Estoque.Api.csproj
dotnet add src/Estoque.Application reference src/Estoque.Domain
dotnet add src/Estoque.Infra reference src/Estoque.Application
dotnet add src/Estoque.Infra reference src/Estoque.Domain
dotnet add src/Estoque.Api reference src/Estoque.Application
dotnet add src/Estoque.Api reference src/Estoque.Infra
