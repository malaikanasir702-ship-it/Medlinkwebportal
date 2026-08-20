Set-Location "D:\HealthPortal-2\HealthPortal\MedLinkPortal"
$output = dotnet ef migrations add InitialPostgres --output-dir Migrations 2>&1
$output | Out-File -FilePath "D:\HealthPortal-2\HealthPortal\MedLinkPortal\migration_output.txt" -Encoding UTF8
Write-Host "Exit code: $LASTEXITCODE"
$output | Select-Object -Last 15 | ForEach-Object { Write-Host $_ }
