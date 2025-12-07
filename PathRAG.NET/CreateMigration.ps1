<#
Runs Entity Framework migrations for the PathRAG.NET solution.

This script prompts for a migration name, creates the migration against the data project,
and then applies it to the database using the API project as the startup project.
#>

Param()

$solutionDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $solutionDir
try
{
    $migrationName = Read-Host "Enter the migration name"
    if ([string]::IsNullOrWhiteSpace($migrationName))
    {
        Write-Host "Migration name cannot be empty." -ForegroundColor Yellow
        exit 1
    }

    $dataProject = "src/PathRAG.NET.Data/PathRAG.NET.Data.csproj"
    $startupProject = "src/PathRAG.NET.API/PathRAG.NET.API.csproj"

    Write-Host "Adding migration '$migrationName'..."
    dotnet ef migrations add $migrationName `
        --project $dataProject `
        --startup-project $startupProject `
        --output-dir Migrations

    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Failed to add the migration. See the log above." -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "Updating the database..."
    dotnet ef database update `
        --project $dataProject `
        --startup-project $startupProject

    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Database update failed. Review the errors above." -ForegroundColor Red
        exit $LASTEXITCODE
    }
}
finally
{
    Pop-Location
}
