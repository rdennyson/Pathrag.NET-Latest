<#
Runs Entity Framework commands for PathRAG.NET.

The script prompts whether to add a migration or only update the database.
If you choose to create a migration you must provide a name; the script will
stop after `dotnet ef migrations add`. If you decline, the script will ask
whether to run `dotnet ef database update` instead.
#>

Param()

function Read-YesNo($prompt)
{
    while ($true)
    {
        $response = Read-Host "$prompt (Y/N)"
        $normalized = if ($null -eq $response) { "" } else { $response.Trim().ToUpperInvariant() }
        if ($normalized -in @("Y", "YES"))
        {
            return $true
        }
        if ($normalized -in @("N", "NO"))
        {
            return $false
        }
        Write-Host "Please answer Y, N, yes, or no." -ForegroundColor Yellow
    }
}

$solutionDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $solutionDir
try
{
    $dataProject = "src/PathRAG.NET.Data/PathRAG.NET.Data.csproj"
    $startupProject = "src/PathRAG.NET.API/PathRAG.NET.API.csproj"

    $shouldCreateMigration = Read-YesNo "Create a new migration?"
    if ($shouldCreateMigration)
    {
        $migrationName = Read-Host "Enter the migration name"
        if ([string]::IsNullOrWhiteSpace($migrationName))
        {
            Write-Host "Migration name cannot be empty." -ForegroundColor Yellow
            exit 1
        }

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

        Write-Host "Migration added successfully. Run database update manually if needed."
        return
    }

    $shouldUpdate = Read-YesNo "Update the database?"
    if ($shouldUpdate)
    {
        Write-Host "Updating the database..."
        dotnet ef database update `
            --project $dataProject `
            --startup-project $startupProject

        if ($LASTEXITCODE -ne 0)
        {
            Write-Host "Database update failed. Review the errors above." -ForegroundColor Red
            exit $LASTEXITCODE
        }

        Write-Host "Database update succeeded."
    }
    else
    {
        Write-Host "No action performed."
    }
}
finally
{
    Pop-Location
}
