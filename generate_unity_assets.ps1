# ============================================================
# generate_unity_assets.ps1
# Generates all Unity .asset (ScriptableObject) files and
# .meta files needed for the Slot Machine project.
# Run this from inside the project root once.
# ============================================================

$scriptDir = $PSScriptRoot
if (-not $scriptDir) { $scriptDir = "c:\Users\mayank\OneDrive\Desktop\project 12" }

# ─── Helper: Write a Unity YAML file ────────────────────────
function Write-UnityYaml($path, $content) {
    Set-Content -Path $path -Value $content -Encoding UTF8
    Write-Host "  Created: $path"
}

# ─── Helper: Generate a deterministic-looking GUID from a seed ──
function New-FakeGUID($seed) {
    $chars = '0123456789abcdef'
    $rng = [System.Random]::new($seed)
    $g = ""
    for ($i = 0; $i -lt 32; $i++) { $g += $chars[$rng.Next(16)] }
    return $g
}

# ─── Helper: Write a .meta file ─────────────────────────────
function Write-MetaFile($assetPath, $guid) {
    $metaContent = @"
fileFormatVersion: 2
guid: $guid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
"@
    Write-UnityYaml "$assetPath.meta" $metaContent
}

# ─── Helper: Write a Sprite .meta file ──────────────────────
function Write-SpriteMeta($assetPath, $guid) {
    $metaContent = @"
fileFormatVersion: 2
guid: $guid
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
  spritePackingTag:
  pSDRemoveMatte: 0
  pSDShowRemoveMatteOption: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"@
    Write-UnityYaml "$assetPath.meta" $metaContent
}

# ─── Symbol ScriptableObject template ───────────────────────
function Write-SymbolAsset($name, $symbolTypeIndex, $payoutMult, $spawnWeight, $isWild, $guid) {
    $wildVal = if ($isWild) { 1 } else { 0 }
    $assetPath = "$scriptDir\Assets\ScriptableObjects\$name.asset"
    $content = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f5f67c52d1564df4a8516d599e4062a9, type: 3}
  m_Name: $name
  m_EditorClassIdentifier:
  symbolType: $symbolTypeIndex
  sprite: {fileID: 0}
  payoutMultiplier: $payoutMult
  spawnWeight: $spawnWeight
  isWild: $wildVal
"@
    Write-UnityYaml $assetPath $content
    Write-MetaFile $assetPath $guid
}

Write-Host "`n=== Generating ScriptableObject Assets ==="
Write-SymbolAsset "SO_Cherry" 0 2 35 $false (New-FakeGUID 101)
Write-SymbolAsset "SO_Bell"   1 3 30 $false (New-FakeGUID 102)
Write-SymbolAsset "SO_Bar"    2 5 20 $false (New-FakeGUID 103)
Write-SymbolAsset "SO_Seven"  3 10 10 $false (New-FakeGUID 104)
Write-SymbolAsset "SO_Wild"   4 0  5 $true  (New-FakeGUID 105)

Write-Host "`n=== Generating Sprite .meta files ==="
$spriteFiles = @{
    "slot-symbol1" = (New-FakeGUID 201)  # Lucky 7
    "slot-symbol2" = (New-FakeGUID 202)  # Cherry
    "slot-symbol3" = (New-FakeGUID 203)  # Bell
    "slot-symbol4" = (New-FakeGUID 204)  # BAR
    "slot-machine1" = (New-FakeGUID 205)
    "slot-machine2" = (New-FakeGUID 206)
    "slot-machine3" = (New-FakeGUID 207)
    "slot-machine4" = (New-FakeGUID 208)
    "slot-machine5" = (New-FakeGUID 209)
    "slot_machine_Middle_box" = (New-FakeGUID 210)
    "slot_machine_buttons-02" = (New-FakeGUID 211)
    "slot_machine_buttons-03" = (New-FakeGUID 212)
    "slot_machine_buttons-04" = (New-FakeGUID 213)
    "popup" = (New-FakeGUID 214)
    "Yes_No_Btn" = (New-FakeGUID 215)
    "bg_gradient" = (New-FakeGUID 216)
}

foreach ($sprite in $spriteFiles.GetEnumerator()) {
    $assetPath = "$scriptDir\Assets\UI\$($sprite.Key).png"
    if (Test-Path $assetPath) {
        Write-SpriteMeta $assetPath $sprite.Value
    }
}

Write-Host "`n=== Generating C# Script .meta files ==="
$scriptMetas = @{
    "Core\GameManager"              = (New-FakeGUID 301)
    "Core\RNGService"               = (New-FakeGUID 302)
    "Core\SlotMachineController"    = (New-FakeGUID 303)
    "Reels\ReelController"          = (New-FakeGUID 304)
    "Reels\ReelStrip"               = (New-FakeGUID 305)
    "Symbols\SymbolData"            = (New-FakeGUID 306)
    "UI\UIManager"                  = (New-FakeGUID 307)
    "UI\PopupController"            = (New-FakeGUID 308)
    "UI\BetController"              = (New-FakeGUID 309)
    "UI\LeverAnimator"              = (New-FakeGUID 310)
    "UI\PaytablePanel"              = (New-FakeGUID 311)
    "Audio\AudioManager"            = (New-FakeGUID 312)
}

foreach ($script in $scriptMetas.GetEnumerator()) {
    $assetPath = "$scriptDir\Assets\Scripts\$($script.Key).cs"
    $guid = $script.Value
    $metaContent = @"
fileFormatVersion: 2
guid: $guid
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
"@
    Write-UnityYaml "$assetPath.meta" $metaContent
}

Write-Host "`n=== Generating Scene .meta file ==="
$sceneMeta = @"
fileFormatVersion: 2
guid: $(New-FakeGUID 401)
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
"@
Write-UnityYaml "$scriptDir\Assets\Scenes\GameScene.unity.meta" $sceneMeta

Write-Host "`n=== Generating folder .meta files ==="
$folders = @("Scripts", "Scripts\Core", "Scripts\Reels", "Scripts\Symbols", "Scripts\UI", "Scripts\Audio",
             "Prefabs", "Animations", "UI", "Sounds", "Scenes", "ScriptableObjects")
$seed = 500
foreach ($folder in $folders) {
    $guid = New-FakeGUID $seed
    $seed++
    $metaContent = @"
fileFormatVersion: 2
guid: $guid
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
"@
    Write-UnityYaml "$scriptDir\Assets\$folder.meta" $metaContent
}

Write-Host "`n=== Generating ProjectSettings .meta ==="
$psContent = @"
fileFormatVersion: 2
guid: $(New-FakeGUID 600)
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
"@
Write-UnityYaml "$scriptDir\ProjectSettings\ProjectSettings.asset.meta" $psContent

Write-Host "`n✅ All Unity asset files generated successfully!"
