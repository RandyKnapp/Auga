# FindValheim.ps1
# Ищет папку Valheim во всех Steam-библиотеках на этом ПК.
# Выводит путь в stdout — MSBuild подхватывает его как переменную.
#
# Порядок поиска:
#   1. Реестр → путь к Steam → libraryfolders.vdf → все библиотеки → Valheim (AppID 892970)
#   2. Распространённые пути по умолчанию (fallback)

param()

$ValheimAppId = '892970'
$ValheimSubPath = 'steamapps\common\Valheim'

# --- 1. Ищем Steam через реестр ---
$steamPath = $null

$registryPaths = @(
    'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam',
    'HKLM:\SOFTWARE\Valve\Steam',
    'HKCU:\SOFTWARE\Valve\Steam'
)

foreach ($regPath in $registryPaths) {
    try {
        $props = Get-ItemProperty $regPath -ErrorAction Stop
        $candidate = if ($props.InstallPath) { $props.InstallPath } else { $props.SteamPath }
        if ($candidate -and (Test-Path $candidate)) {
            $steamPath = $candidate
            break
        }
    } catch {}
}

# --- 2. Парсим libraryfolders.vdf и ищем все Steam-библиотеки ---
if ($steamPath) {
    $vdfPath = Join-Path $steamPath 'steamapps\libraryfolders.vdf'

    if (Test-Path $vdfPath) {
        $vdf = Get-Content $vdfPath -Raw

        # Извлекаем пути из VDF ("path" "C:\\...")
        $pathMatches = [regex]::Matches($vdf, '"path"\s+"([^"]+)"')
        $libraryPaths = $pathMatches | ForEach-Object {
            $_.Groups[1].Value -replace '\\\\', '\'
        }

        foreach ($lib in $libraryPaths) {
            $valheimPath = Join-Path $lib $ValheimSubPath
            if (Test-Path $valheimPath) {
                Write-Host $valheimPath
                exit 0
            }
        }
    }
}

# --- 3. Fallback: распространённые пути ---
$fallbacks = @(
    'C:\Program Files (x86)\Steam\steamapps\common\Valheim',
    'C:\Program Files\Steam\steamapps\common\Valheim',
    'D:\Steam\steamapps\common\Valheim',
    'D:\SteamLibrary\steamapps\common\Valheim',
    'E:\Steam\steamapps\common\Valheim',
    'E:\SteamLibrary\steamapps\common\Valheim',
    'F:\Steam\steamapps\common\Valheim',
    'F:\SteamLibrary\steamapps\common\Valheim'
)

foreach ($path in $fallbacks) {
    if (Test-Path $path) {
        Write-Host $path
        exit 0
    }
}

# Не найдено — выводим пустую строку, MSBuild покажет Warning
Write-Host ''
exit 0
