[CmdletBinding()]
param(
    [switch]$crear_clave,
    [string]$version,
    [string]$carpeta_publish,
    [switch]$actualizar_recomendada,
    [string]$base_politica = 'ninos_db_testing',
    [securestring]$clave_segura
)

$ErrorActionPreference = 'Stop'
$iteraciones_kdf = 210000
$longitud_hash = 32
$minimo_clave = 12
$versiones_a_conservar = 3
$patron_version = '^\d+\.\d+\.\d+$'
$raiz_repo = Split-Path -Parent $PSScriptRoot
$carpeta_respaldos = Join-Path (Split-Path -Parent $raiz_repo) 'respaldos'
$carpeta_salida = Join-Path $carpeta_respaldos 'velopack-pruebas\releases'
$carpeta_privada = Join-Path $PSScriptRoot 'private'
$ruta_clave = Join-Path $carpeta_privada 'clave_publicacion.json'
$ruta_acceso = Join-Path $carpeta_privada 'acceso-vps.env'
$ruta_servidor = '/var/www/ninos-updates/updates'
$vpk = Join-Path $env:USERPROFILE '.dotnet\tools\vpk.exe'

function leer_acceso {
    if (-not (Test-Path $ruta_acceso)) { throw 'Falta installer\private\acceso-vps.env.' }
    $valores = @{}
    foreach ($linea in Get-Content $ruta_acceso) {
        if ($linea -match '^\s*([A-Z_]+)\s*=\s*(.*?)\s*$') { $valores[$matches[1]] = $matches[2] }
    }
    foreach ($obligatorio in @('VPS_HOST', 'SSH_USER', 'SSH_KEY')) {
        if ([string]::IsNullOrWhiteSpace($valores[$obligatorio])) { throw "Falta $obligatorio en acceso-vps.env." }
    }
    return $valores
}

function nuevo_hash {
    param([securestring]$secreto, [byte[]]$sal, [int]$iteraciones)
    $puntero = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secreto)
    try {
        $texto = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($puntero)
        $kdf = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($texto, $sal, $iteraciones, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
        try { return ,$kdf.GetBytes($longitud_hash) }
        finally { $kdf.Dispose() }
    }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($puntero) }
}

function bytes_iguales {
    param([byte[]]$a, [byte[]]$b)
    if ($a.Length -ne $b.Length) { return $false }
    $diferencia = 0
    for ($i = 0; $i -lt $a.Length; $i++) { $diferencia = $diferencia -bor ($a[$i] -bxor $b[$i]) }
    return ($diferencia -eq 0)
}

function crear_clave_publicacion {
    param([securestring]$primera, [securestring]$segunda)
    if ($primera.Length -lt $minimo_clave) { throw "La clave debe tener al menos $minimo_clave caracteres." }
    $sal = New-Object byte[] 16
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($sal)
    $hash_primera = nuevo_hash $primera $sal $iteraciones_kdf
    $hash_segunda = nuevo_hash $segunda $sal $iteraciones_kdf
    if (-not (bytes_iguales $hash_primera $hash_segunda)) { throw 'Las dos claves no coinciden. No se guardo nada.' }
    New-Item -ItemType Directory -Force $carpeta_privada | Out-Null
    $dato = [ordered]@{
        algoritmo = 'PBKDF2-SHA256'
        iteraciones = $iteraciones_kdf
        sal = [Convert]::ToBase64String($sal)
        hash = [Convert]::ToBase64String($hash_primera)
    }
    $dato | ConvertTo-Json | Set-Content -Path $ruta_clave -Encoding utf8
    Write-Host 'Clave de publicacion guardada (solo se guardaron el hash y la sal).'
}

function verificar_clave {
    param([securestring]$intento)
    if (-not (Test-Path $ruta_clave)) { throw 'No existe la clave de publicacion. Creela con -crear_clave.' }
    $dato = Get-Content $ruta_clave -Raw | ConvertFrom-Json
    $sal = [Convert]::FromBase64String($dato.sal)
    $esperado = [Convert]::FromBase64String($dato.hash)
    $calculado = nuevo_hash $intento $sal ([int]$dato.iteraciones)
    return (bytes_iguales $calculado $esperado)
}

function ejecutar_ssh {
    param($acceso, [string]$comando)
    & ssh -i $acceso['SSH_KEY'] -o BatchMode=yes -o StrictHostKeyChecking=accept-new "$($acceso['SSH_USER'])@$($acceso['VPS_HOST'])" $comando
    if ($LASTEXITCODE -ne 0) { throw "Fallo un comando en el servidor (codigo $LASTEXITCODE)." }
}

function copiar_al_servidor {
    param($acceso, [string[]]$origenes, [string]$destino)
    & scp -i $acceso['SSH_KEY'] -o BatchMode=yes -o StrictHostKeyChecking=accept-new @origenes "$($acceso['SSH_USER'])@$($acceso['VPS_HOST']):$destino"
    if ($LASTEXITCODE -ne 0) { throw "La subida se corto (codigo $LASTEXITCODE)." }
}

function descargar_del_servidor {
    param($acceso, [string]$ruta_remota, [string]$carpeta_local)
    & scp -i $acceso['SSH_KEY'] -o BatchMode=yes -o StrictHostKeyChecking=accept-new "$($acceso['SSH_USER'])@$($acceso['VPS_HOST']):$ruta_remota" $carpeta_local
    if ($LASTEXITCODE -ne 0) { throw "No se pudo bajar $ruta_remota del servidor (codigo $LASTEXITCODE)." }
}

function versiones_en_servidor {
    param($acceso)
    $lineas = & ssh -i $acceso['SSH_KEY'] -o BatchMode=yes -o StrictHostKeyChecking=accept-new "$($acceso['SSH_USER'])@$($acceso['VPS_HOST'])" "ls $ruta_servidor 2>/dev/null" 2>$null
    $versiones = @()
    foreach ($linea in $lineas) {
        if ($linea -match '^NinOS-(\d+\.\d+\.\d+)-full\.nupkg$') { $versiones += $matches[1] }
    }
    return @($versiones | Sort-Object { [version]$_ } -Descending)
}

function publicar_version {
    param([string]$nueva_version, [securestring]$clave, $acceso)
    if ($nueva_version -notmatch $patron_version) { throw "La version '$nueva_version' no tiene el formato X.Y.Z." }
    if (-not (verificar_clave $clave)) { throw 'Clave incorrecta. No se subio nada.' }
    Write-Host 'Clave correcta.'

    if (-not $carpeta_publish) {
        $carpeta_publish = Join-Path $carpeta_respaldos "velopack-pruebas\publish-$nueva_version"
        & dotnet publish (Join-Path $raiz_repo 'src\NinOS.UI\NinOS.UI.csproj') -c Release -r win-x64 --self-contained "-p:Version=$nueva_version" -o $carpeta_publish | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion. No se subio nada.' }
    }

    New-Item -ItemType Directory -Force $carpeta_salida | Out-Null
    Get-ChildItem $carpeta_salida -File | Where-Object { $_.Name -like 'NinOS*' -or $_.Name -eq 'RELEASES' -or $_.Name -like '*.json' } | Remove-Item -Force

    $en_servidor = @(versiones_en_servidor $acceso | Where-Object { $_ -ne $nueva_version })
    $conservar = @(@($en_servidor + $nueva_version) | Sort-Object { [version]$_ } -Descending | Select-Object -First $versiones_a_conservar)
    foreach ($version_remota in ($conservar | Where-Object { $_ -ne $nueva_version })) {
        descargar_del_servidor $acceso "$ruta_servidor/NinOS-$version_remota-full.nupkg" $carpeta_salida
    }

    & $vpk pack -u NinOS -v $nueva_version -p $carpeta_publish -e NinOS.UI.exe -o $carpeta_salida | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Fallo el empaquetado. No se subio nada.' }

    $nuevo = "$ruta_servidor.nuevo"
    ejecutar_ssh $acceso "rm -rf $nuevo && mkdir -p $nuevo"
    try {
        $archivos = @(Get-ChildItem $carpeta_salida -File | ForEach-Object { $_.FullName })
        copiar_al_servidor $acceso $archivos "$nuevo/"
    }
    catch {
        ejecutar_ssh $acceso "rm -rf $nuevo" 2>$null
        throw 'La subida se corto. La version nueva no quedo disponible.'
    }
    ejecutar_ssh $acceso "rm -rf $ruta_servidor.viejo; if [ -d $ruta_servidor ]; then mv $ruta_servidor $ruta_servidor.viejo; fi; mv $nuevo $ruta_servidor && rm -rf $ruta_servidor.viejo"
    Write-Host "Version $nueva_version publicada. Versiones en el servidor: $((versiones_en_servidor $acceso) -join ', ')"

    if ($actualizar_recomendada) {
        ejecutar_ssh $acceso "sudo -u postgres psql -v ON_ERROR_STOP=1 -d $base_politica -c `"update app_update_policy set recommended_version='$nueva_version' where id_policy=1;`""
        Write-Host "Version recomendada cambiada a $nueva_version en $base_politica."
    }
}

function iniciar {
    if ($crear_clave) {
        $primera = Read-Host 'Nueva clave de publicacion' -AsSecureString
        $segunda = Read-Host 'Repita la clave' -AsSecureString
        crear_clave_publicacion $primera $segunda
        return
    }
    if (-not $version) {
        Write-Host 'Uso: deploy.ps1 -crear_clave   |   deploy.ps1 -version X.Y.Z [-actualizar_recomendada]'
        return
    }
    $acceso = leer_acceso
    $clave = $clave_segura
    if (-not $clave) { $clave = Read-Host 'Clave de publicacion' -AsSecureString }
    publicar_version $version $clave $acceso
}

if ($MyInvocation.InvocationName -ne '.') { iniciar }
