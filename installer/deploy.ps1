[CmdletBinding()]
param(
    [switch]$crear_clave,
    [string]$version,
    [switch]$volver_atras,
    [string]$a_version,
    [string]$carpeta_publish,
    [switch]$actualizar_recomendada,
    [string]$base_politica = 'ninos_db_testing',
    [securestring]$clave_segura
)

$ErrorActionPreference = 'Stop'
$iteraciones_kdf = 210000
$longitud_hash = 32
$minimo_clave = 12
$versiones_a_conservar = 4
$patron_version = '^\d+\.\d+\.\d+$'
$segundos_entre_medidas = 3
$raiz_repo = Split-Path -Parent $PSScriptRoot
$carpeta_respaldos = Join-Path (Split-Path -Parent $raiz_repo) 'respaldos'
$carpeta_salida = Join-Path $carpeta_respaldos 'velopack-pruebas\releases'
$carpeta_privada = Join-Path $PSScriptRoot 'private'
$ruta_clave = Join-Path $carpeta_privada 'clave_publicacion.json'
$ruta_acceso = Join-Path $carpeta_privada 'acceso-vps.env'
$ruta_servidor = '/var/www/ninos-updates/updates'
$vpk = Join-Path $env:USERPROFILE '.dotnet\tools\vpk.exe'
$opciones_red = @('-o', 'BatchMode=yes', '-o', 'StrictHostKeyChecking=accept-new', '-o', 'ConnectTimeout=20', '-o', 'ServerAliveInterval=15', '-o', 'ServerAliveCountMax=3')

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
    $null | & ssh -i $acceso['SSH_KEY'] @opciones_red "$($acceso['SSH_USER'])@$($acceso['VPS_HOST'])" $comando
    if ($LASTEXITCODE -ne 0) { throw "Fallo un comando en el servidor (codigo $LASTEXITCODE)." }
}

function ejecutar_con_progreso {
    param([string]$actividad, [int]$segundos_estimados, [scriptblock]$bloque, [object[]]$argumentos)
    $trabajo = Start-Job -ScriptBlock $bloque -ArgumentList $argumentos
    $inicio = Get-Date
    while ($trabajo.State -eq 'Running') {
        Start-Sleep -Milliseconds 500
        $transcurrido = ((Get-Date) - $inicio).TotalSeconds
        $porcentaje = [int][math]::Min(95, 100 * $transcurrido / [math]::Max($segundos_estimados, 1))
        Write-Progress -Activity $actividad -Status ("{0:N0} s transcurridos" -f $transcurrido) -PercentComplete $porcentaje
    }
    Write-Progress -Activity $actividad -Completed
    $codigo = Receive-Job -Job $trabajo -Wait | Select-Object -Last 1
    Remove-Job -Job $trabajo
    Write-Host ("{0} terminado en {1:N0} s." -f $actividad, ((Get-Date) - $inicio).TotalSeconds)
    return $codigo
}

function bytes_en_servidor {
    param($acceso, [string]$carpeta_remota)
    $salida = $null | & ssh -i $acceso['SSH_KEY'] @opciones_red "$($acceso['SSH_USER'])@$($acceso['VPS_HOST'])" "du -sb $carpeta_remota 2>/dev/null | cut -f1"
    $numero = 0
    if ([long]::TryParse(("$salida").Trim(), [ref]$numero)) { return $numero }
    return 0
}

function copiar_al_servidor {
    param($acceso, [string[]]$origenes, [string]$destino)
    $total_bytes = [long](($origenes | ForEach-Object { (Get-Item -LiteralPath $_).Length } | Measure-Object -Sum).Sum)
    $carpeta_remota = $destino.TrimEnd('/')
    $bytes_de_partida = bytes_en_servidor $acceso $carpeta_remota
    $argumentos = @('-i', $acceso['SSH_KEY']) + $opciones_red + $origenes + @("$($acceso['SSH_USER'])@$($acceso['VPS_HOST']):$destino")
    Write-Host ("Subiendo {0:N1} MB al servidor..." -f ($total_bytes / 1MB))
    $inicio = Get-Date
    $trabajo = Start-Job -ScriptBlock { param([string[]]$lista) & scp @lista; $LASTEXITCODE } -ArgumentList (, $argumentos)
    $bytes_previos = 0
    $momento_previo = $inicio
    while ($trabajo.State -eq 'Running') {
        Start-Sleep -Seconds $segundos_entre_medidas
        $enviados = [math]::Max(0, (bytes_en_servidor $acceso $carpeta_remota) - $bytes_de_partida)
        $ahora = Get-Date
        $segundos = [math]::Max(($ahora - $momento_previo).TotalSeconds, 0.1)
        $velocidad_mb = (($enviados - $bytes_previos) / $segundos) / 1MB
        $porcentaje = [int][math]::Min(100, [math]::Floor(100 * $enviados / [math]::Max($total_bytes, 1)))
        $estado = "{0:N1} de {1:N1} MB  |  {2:N2} MB/s  |  {3:N0} s" -f ($enviados / 1MB), ($total_bytes / 1MB), $velocidad_mb, ($ahora - $inicio).TotalSeconds
        Write-Progress -Activity 'Subiendo actualizacion al servidor' -Status $estado -PercentComplete $porcentaje
        $bytes_previos = $enviados
        $momento_previo = $ahora
    }
    Write-Progress -Activity 'Subiendo actualizacion al servidor' -Completed
    $codigo = Receive-Job -Job $trabajo -Wait | Select-Object -Last 1
    Remove-Job -Job $trabajo
    if ($codigo -ne 0) { throw "La subida se corto (codigo $codigo)." }
    Write-Host ("Subida terminada en {0:N0} s." -f ((Get-Date) - $inicio).TotalSeconds)
}

function descargar_del_servidor {
    param($acceso, [string]$ruta_remota, [string]$carpeta_local)
    $null | & scp -i $acceso['SSH_KEY'] @opciones_red "$($acceso['SSH_USER'])@$($acceso['VPS_HOST']):$ruta_remota" $carpeta_local
    if ($LASTEXITCODE -ne 0) { throw "No se pudo bajar $ruta_remota del servidor (codigo $LASTEXITCODE)." }
}

function versiones_en_servidor {
    param($acceso)
    $lineas = $null | & ssh -i $acceso['SSH_KEY'] @opciones_red "$($acceso['SSH_USER'])@$($acceso['VPS_HOST'])" "ls $ruta_servidor 2>/dev/null" 2>$null
    $versiones = @()
    foreach ($linea in $lineas) {
        if ($linea -match '^NinOS-(\d+\.\d+\.\d+)-full\.nupkg$') { $versiones += $matches[1] }
    }
    return @($versiones | Sort-Object { [version]$_ } -Descending)
}

function autorizar_publicacion {
    param([securestring]$clave, $acceso)
    Write-Host 'Revisando la clave...'
    if (-not (verificar_clave $clave)) { throw 'Clave incorrecta. No se subio nada.' }
    Write-Host 'Clave correcta.'
    Write-Host 'Revisando la conexion con el servidor...'
    ejecutar_ssh $acceso 'echo conexion-ok'
    Write-Host 'Conexion correcta.'
}

function publicar_paquete {
    param([string]$nueva_version, [string]$carpeta_codigo, $acceso)
    if ($nueva_version -notmatch $patron_version) { throw "La version '$nueva_version' no tiene el formato X.Y.Z." }

    New-Item -ItemType Directory -Force $carpeta_salida | Out-Null
    Get-ChildItem $carpeta_salida -File | Remove-Item -Force

    Write-Host 'Mirando que versiones hay en el servidor...'
    $en_servidor = @(versiones_en_servidor $acceso | Where-Object { $_ -ne $nueva_version })
    $conservar = @(@($en_servidor + $nueva_version) | Sort-Object { [version]$_ } -Descending | Select-Object -First $versiones_a_conservar)
    $descartar = @($en_servidor | Where-Object { $conservar -notcontains $_ })

    if ($en_servidor.Count -gt 0) {
        Write-Host "Bajando solo la version $($en_servidor[0]) (la mas reciente) para armar el cambio diferencial..."
        descargar_del_servidor $acceso "$ruta_servidor/NinOS-$($en_servidor[0])-full.nupkg" $carpeta_salida
    }

    $codigo_empaquetado = ejecutar_con_progreso 'Empaquetando la actualizacion (completa y diferencial)' 40 {
        param($vpk_a_usar, $version_a_empaquetar, $codigo_a_empaquetar, $salida_empaquetado)
        & $vpk_a_usar pack -u NinOS -v $version_a_empaquetar -p $codigo_a_empaquetar -e NinOS.UI.exe -o $salida_empaquetado *> $null
        $LASTEXITCODE
    } @($vpk, $nueva_version, $carpeta_codigo, $carpeta_salida)
    if ($codigo_empaquetado -ne 0) { throw 'Fallo el empaquetado. No se subio nada.' }
    Write-Host 'Paquete listo.'

    $nuevo = "$ruta_servidor.nuevo"
    $comandos_de_limpieza = @($descartar | ForEach-Object { "rm -f $nuevo/NinOS-$_-full.nupkg $nuevo/NinOS-$_-delta.nupkg" })
    $copia = "rm -rf $nuevo && mkdir -p $nuevo && cp -a $ruta_servidor/. $nuevo/"
    ejecutar_ssh $acceso ((@($copia) + $comandos_de_limpieza) -join ' && ')

    try {
        $archivos = @(Get-ChildItem $carpeta_salida -File | ForEach-Object { $_.FullName })
        copiar_al_servidor $acceso $archivos "$nuevo/"
    }
    catch {
        ejecutar_ssh $acceso "rm -rf $nuevo" 2>$null
        throw 'La subida se corto. La version nueva no quedo disponible.'
    }
    Write-Host 'Activando la version en el servidor...'
    ejecutar_ssh $acceso "rm -rf $ruta_servidor.viejo; if [ -d $ruta_servidor ]; then mv $ruta_servidor $ruta_servidor.viejo; fi; mv $nuevo $ruta_servidor && rm -rf $ruta_servidor.viejo"
    Write-Host "Version $nueva_version publicada. Versiones en el servidor: $((versiones_en_servidor $acceso) -join ', ')"
    Get-ChildItem $carpeta_salida -File | Remove-Item -Force
    Write-Host 'Archivos temporales borrados de tu PC.'

    if ($actualizar_recomendada) { actualizar_version_recomendada $acceso $nueva_version }
}

function actualizar_version_recomendada {
    param($acceso, [string]$nueva_version)
    $sql = "update app_update_policy set recommended_version='$nueva_version' where id_policy=1;"
    $sql_codificado = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($sql))
    ejecutar_ssh $acceso "echo $sql_codificado | base64 -d | sudo -u postgres psql -v ON_ERROR_STOP=1 -d $base_politica"
    Write-Host "Version recomendada cambiada a $nueva_version en $base_politica."
}

function publicar_version {
    param([string]$nueva_version, [securestring]$clave, $acceso)
    if ($nueva_version -notmatch $patron_version) { throw "La version '$nueva_version' no tiene el formato X.Y.Z." }
    autorizar_publicacion $clave $acceso

    if (-not $carpeta_publish) {
        $carpeta_publish = Join-Path $carpeta_respaldos "velopack-pruebas\publish-$nueva_version"
        $proyecto = Join-Path $raiz_repo 'src\NinOS.UI\NinOS.UI.csproj'
        $codigo_compilacion = ejecutar_con_progreso "Compilando la version $nueva_version" 150 {
            param($proyecto_a_compilar, $version_a_compilar, $salida_compilacion)
            dotnet publish $proyecto_a_compilar -c Release -r win-x64 --self-contained "-p:Version=$version_a_compilar" -o $salida_compilacion *> $null
            $LASTEXITCODE
        } @($proyecto, $nueva_version, $carpeta_publish)
        if ($codigo_compilacion -ne 0) { throw 'Fallo la compilacion. No se subio nada.' }
    }

    publicar_paquete $nueva_version $carpeta_publish $acceso
}

function siguiente_version_de_parche {
    param([string]$version_actual)
    $partes = [version]$version_actual
    return "$($partes.Major).$($partes.Minor).$($partes.Build + 1)"
}

function codigo_de_version_guardada {
    param($acceso, [string]$version_guardada, [string]$carpeta_trabajo)
    New-Item -ItemType Directory -Force $carpeta_trabajo | Out-Null
    descargar_del_servidor $acceso "$ruta_servidor/NinOS-$version_guardada-full.nupkg" $carpeta_trabajo
    $paquete_zip = Join-Path $carpeta_trabajo "NinOS-$version_guardada-full.zip"
    Copy-Item -LiteralPath (Join-Path $carpeta_trabajo "NinOS-$version_guardada-full.nupkg") -Destination $paquete_zip -Force
    $carpeta_extraida = Join-Path $carpeta_trabajo 'codigo'
    Remove-Item -LiteralPath $carpeta_extraida -Recurse -Force -ErrorAction SilentlyContinue
    Expand-Archive -LiteralPath $paquete_zip -DestinationPath $carpeta_extraida -Force
    Get-ChildItem $carpeta_extraida -Recurse -Filter 'appsettings*.json' | Remove-Item -Force
    $ejecutable = Get-ChildItem $carpeta_extraida -Recurse -Filter 'NinOS.UI.exe' | Select-Object -First 1
    if (-not $ejecutable) { throw "El paquete $version_guardada no trae NinOS.UI.exe." }
    return $ejecutable.Directory.FullName
}

function volver_atras_version {
    param([securestring]$clave, $acceso, [string]$version_pedida)
    autorizar_publicacion $clave $acceso

    $versiones = @(versiones_en_servidor $acceso)
    if ($versiones.Count -lt 2) { throw 'No hay versiones anteriores en el servidor para volver atras.' }
    $anteriores = @($versiones | Select-Object -Skip 1 -First ($versiones_a_conservar - 1))
    if ($version_pedida) {
        if ($version_pedida -notmatch $patron_version) { throw "La version '$version_pedida' no tiene el formato X.Y.Z." }
        if ($anteriores -notcontains $version_pedida) { throw "Solo se puede volver a: $($anteriores -join ', ')." }
        $origen = $version_pedida
    }
    else {
        $origen = $anteriores[0]
    }

    $nueva_version = siguiente_version_de_parche $versiones[0]
    Write-Host "Vuelve el CODIGO de la $origen y se publica como version $nueva_version."
    Write-Host 'Recuerda: vuelve el codigo, no los datos de la base.'
    $carpeta_trabajo = Join-Path $carpeta_respaldos "velopack-pruebas\volver-atras-$origen"
    $carpeta_codigo = codigo_de_version_guardada $acceso $origen $carpeta_trabajo
    Write-Host "Codigo de la $origen listo."
    publicar_paquete $nueva_version $carpeta_codigo $acceso
    Remove-Item -LiteralPath $carpeta_trabajo -Recurse -Force
    Write-Host 'Carpeta temporal de la vuelta atras borrada de tu PC.'
}

function iniciar {
    if ($crear_clave) {
        $primera = Read-Host 'Nueva clave de publicacion' -AsSecureString
        $segunda = Read-Host 'Repita la clave' -AsSecureString
        crear_clave_publicacion $primera $segunda
        return
    }
    if ($volver_atras) {
        $acceso = leer_acceso
        $clave = $clave_segura
        if (-not $clave) { $clave = Read-Host 'Clave de publicacion' -AsSecureString }
        volver_atras_version $clave $acceso $a_version
        return
    }
    if (-not $version) {
        Write-Host 'Uso: deploy.ps1 -crear_clave   |   deploy.ps1 -version X.Y.Z [-actualizar_recomendada]   |   deploy.ps1 -volver_atras [-a_version X.Y.Z] [-actualizar_recomendada]'
        return
    }
    $acceso = leer_acceso
    $clave = $clave_segura
    if (-not $clave) { $clave = Read-Host 'Clave de publicacion' -AsSecureString }
    publicar_version $version $clave $acceso
}

if ($MyInvocation.InvocationName -ne '.') { iniciar }
