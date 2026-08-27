dotnet new sln -n SystemOrder


mkdir src
mkdir tests

dotnet new classlib \
-n SystemOrder.Domain \
-o src/SystemOrder.Domain



dotnet new classlib \
-n SystemOrder.Application \
-o src/SystemOrder.Application


dotnet new classlib \
-n SystemOrder.Infrastructure \
-o src/SystemOrder.Infrastructure

dotnet new webapi \
-n SystemOrder.Api \
-o src/SystemOrder.Api \
--use-controllers


dotnet new xunit \
-n SystemOrder.UnitTests \
-o tests/SystemOrder.UnitTests


dotnet sln add src/SystemOrder.Domain/SystemOrder.Domain.csproj

dotnet sln add src/SystemOrder.Application/SystemOrder.Application.csproj

dotnet sln add src/SystemOrder.Infrastructure/SystemOrder.Infrastructure.csproj

dotnet sln add src/SystemOrder.Api/SystemOrder.Api.csproj

dotnet sln add tests/SystemOrder.UnitTests/SystemOrder.UnitTests.csproj


dotnet sln list

