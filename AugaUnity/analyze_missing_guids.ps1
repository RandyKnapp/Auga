# analyze_missing_guids.ps1
# Для каждого missing GUID собирает уникальные поля сериализации из YAML-блоков
# чтобы можно было сопоставить с классами в Unity.Auga.dll

$missingGuids = @(
    "0cd44c1031e13a943bb63640046fad76",
    "0d0b652f32a2cc243917e4028fa0f046",
    "1344c3c82d62a2a41a3576d8abb8e3ea",
    "1aa08ab6e0800fa44ae55d278d1423e3",
    "2a4db7a114972834c8e4117be1d82ba3",
    "2fafe2cfe61f6974895a912c3755e8f1",
    "30649d3a9faa99c48a7b1166b86bf2a0",
    "306cc8c2b49d7114eaa3623786fc2126",
    "31a19414c41e5ae4aae2af33fee712f6",
    "3245ec927659c4140ac4f8d17403cc18",
    "3312d7739989d2b4e91e6319e9a96d76",
    "37d45424c9ce99a4589f819c134bd7a5",
    "42b067a853974824b92fd5fbbb6eaed3",
    "4e29b1a8efbd4b44bb3f3716e73f07ff",
    "59f8146938fff824cb5fd77236b75775",
    "5ca3ca28bf53e3440a0bd230b21939ff",
    "5f7201a12d95ffc409449d95f23cf332",
    "67db9e8f0e2ae9c40bc1e2b64352a6b4",
    "7a98125502f715b4b83cfb77b434e436",
    "8a8695521f0d02e499659fee002a26c2",
    "9085046f02f69544eb97fd06b6048fe2",
    "93da609ca8603ee488f7f2b42a413947",
    "9541d86e2fd84c1d9990edf0852d74ab",
    "cfabb0440166ab443bba8876756fdfa9",
    "d0b148fe25e99eb48b9724523833bab1",
    "dc42784cf147c0c48a680349fa168899",
    "e19747de3f5aca642ab2be37e372fb86",
    "f4688fdb7df04437aeb418b961361dc5",
    "fe87c0e1cc204ed48ad3b37840f39efc"
)

# Для каждого GUID собираем уникальные наборы свойств
$guidProperties = @{}
foreach ($g in $missingGuids) { $guidProperties[$g] = [System.Collections.Generic.HashSet[string]]::new() }

$prefabDir = Join-Path $PSScriptRoot "Assets"
$prefabs = Get-ChildItem -Path $prefabDir -Filter "*.prefab" -Recurse

foreach ($prefab in $prefabs) {
    $lines = Get-Content $prefab.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        foreach ($guid in $missingGuids) {
            if ($lines[$i] -match "guid: $guid") {
                # Ищем следующие строки свойств (до следующего --- или m_Script)
                $props = @()
                for ($j = $i + 1; $j -lt [Math]::Min($i + 30, $lines.Count); $j++) {
                    $l = $lines[$j].Trim()
                    if ($l -match "^---" -or $l -match "^m_Script:" -or $l -eq "") { break }
                    # Извлекаем имя свойства (первое слово до ':')
                    if ($l -match "^(\w+):") {
                        $propName = $Matches[1]
                        if ($propName -ne "m_ObjectHideFlags" -and $propName -ne "m_PrefabInstance" -and
                            $propName -ne "m_PrefabAsset" -and $propName -ne "m_GameObject" -and
                            $propName -ne "m_Enabled" -and $propName -ne "m_EditorHideFlags") {
                            $props += $propName
                        }
                    }
                }
                if ($props.Count -gt 0) {
                    $key = ($props | Sort-Object | Select-Object -Unique) -join ", "
                    $null = $guidProperties[$guid].Add($key)
                }
            }
        }
    }
}

# Вывод результатов
$output = [System.Text.StringBuilder]::new()
$null = $output.AppendLine("=== Missing GUID -> Serialized Fields Analysis ===")
$null = $output.AppendLine("")

foreach ($guid in ($missingGuids | Sort-Object)) {
    $props = $guidProperties[$guid]
    $null = $output.AppendLine("GUID: $guid")
    foreach ($p in $props) {
        $null = $output.AppendLine("  Fields: $p")
    }
    $null = $output.AppendLine("")
}

$outputPath = Join-Path $PSScriptRoot "guid_analysis.txt"
$output.ToString() | Out-File $outputPath -Encoding UTF8
Write-Host "Saved to: $outputPath"
Write-Host $output.ToString()
