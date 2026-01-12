---
sidebar_position: 7
---

# Troubleshooting

Soluciones a problemas comunes en automatización UI de Windows.

## Problemas de Lanzamiento de Aplicación

### TimeoutException: "No se pudo obtener la ventana principal"

**Síntomas:**
```
System.TimeoutException: No se pudo obtener la ventana principal después de 15000ms (PID: 38092)
```

**Causa:** El framework no puede encontrar la ventana de la aplicación.

#### Para Apps UWP (Calculadora, Apps de Windows Store)

✅ **Solución 1:** Aumentar timeout a 15-20 segundos

```json
{
  "AppPath": "calc.exe",
  "DefaultTimeout": 20000  // 20 segundos
}
```

✅ **Solución 2:** Verificar en logs qué modo de búsqueda se usó

```
# Buscar en logs/test-*.log
[00:05.000] ⚠️ Switching to relaxed search mode (by window title)
[00:05.500] ✓ Ventana encontrada: 'Calculadora' (PID: 38124, Mode: Relaxed)
```

Si ves `Mode: Relaxed`, significa que la ventana está en un **proceso hijo** (normal en UWP).

✅ **Solución 3:** Verificar que la app no requiere permisos de admin

```bash
# Ejecutar como usuario normal (NO como admin)
dotnet test
```

#### Para Apps Win32 Clásicas (Notepad, Paint, apps legacy)

✅ **Solución:** Timeout de 5-10 segundos es suficiente

```json
{
  "AppPath": "notepad.exe",
  "DefaultTimeout": 5000  // 5 segundos
}
```

Si falla, verifica que la app no esté bloqueada o requiera interacción manual.

### Cursor/VS Code se Cierra al Ejecutar Tests

**Síntoma:** Al ejecutar `dotnet test`, el IDE se cierra inesperadamente.

**Causa:** Versión antigua del framework que buscaba ventanas sin filtrar por PID.

✅ **Solución:** Actualizar a la última versión que incluye:
- Búsqueda estricta por ProcessId en primeros 5 segundos
- Lista de exclusión para IDEs (Cursor, VS Code, Visual Studio)

**Verificar que tienes la última versión:**
```csharp
// En AppLauncher.cs debería haber:
var excludedTitles = new[] { 
    "Barra de tareas", "Taskbar", "Program Manager", 
    "Cursor", "Visual Studio", "Visual Studio Code"
};
```

## Problemas de Elementos

### Element Not Found / TimeoutException

**Síntomas:**
```
FlaUI.Core.Exceptions.ElementNotAvailableException: 
Element with AutomationId 'ButtonId' not found
```

**Causas y Soluciones:**

#### 1. AutomationId Incorrecto

✅ **Solución:** Usar Inspect.exe para verificar

```bash
# Ubicación (Windows SDK)
C:\Program Files (x86)\Windows Kits\10\bin\<version>\x64\inspect.exe
```

**Pasos:**
1. Abrir Inspect.exe
2. Abrir tu aplicación
3. Hover sobre el elemento
4. Verificar propiedad "AutomationId" en panel derecho
5. Copiar valor exacto (case-sensitive)

#### 2. Elemento No Está Listo

✅ **Solución:** Aumentar timeout o añadir wait específico

```csharp
// Aumentar timeout
var element = WaitHelper.WaitForElement(window, "ButtonId", timeoutMs: 10000);

// O esperar condición específica
WaitHelper.WaitUntil(
    () => element != null && element.IsEnabled,
    timeoutMs: 5000,
    conditionDescription: "botón habilitado"
);
```

#### 3. Elemento en Popup/Modal

✅ **Solución:** Esperar a que popup aparezca primero

```csharp
// Esperar popup
WaitHelper.WaitForWindowTitle("Settings", 5000);

// Buscar en todas las ventanas
var allWindows = automation.GetDesktop().FindAllChildren();
var popup = allWindows.FirstOrDefault(w => w.Name == "Settings");

// Buscar elemento en popup
var element = WaitHelper.WaitForElement(popup, "OkButton", 5000);
```

#### 4. Aplicación Usa UI Framework No Estándar

✅ **Solución:** Cambiar a UIA2 (si UIA3 no funciona)

```csharp
// En lugar de UIA3Automation
var automation = new UIA2Automation();
```

### Flaky Tests (Intermitentes)

**Síntomas:** Tests pasan a veces pero fallan otras veces.

**Causas Comunes:**

#### 1. Waits Insuficientes

❌ **Problema:**
```csharp
button.Click();
Thread.Sleep(500);  // Hardcoded sleep
var result = GetResult();
```

✅ **Solución:**
```csharp
button.Click();
WaitHelper.WaitForElement(window, "ResultLabel", 5000);
var result = GetResult();
```

#### 2. Race Conditions

❌ **Problema:**
```csharp
EnterText("user");
EnterPassword("pass");
ClickLogin();  // Click antes de que texto esté completamente ingresado
```

✅ **Solución:**
```csharp
EnterText("user");
Thread.Sleep(100);  // Pequeño delay para que procese
EnterPassword("pass");
Thread.Sleep(100);
WaitHelper.WaitForElementEnabled(loginButton, 5000);
ClickLogin();
```

#### 3. Estado Residual de Test Anterior

❌ **Problema:** Test depende del estado dejado por test anterior.

✅ **Solución:**
```csharp
[SetUp]
public void TestSetUp()
{
    // BaseTest.SetUp ya lanzó app limpia
    // Navegar a estado inicial conocido
    ResetToDefaultState();
}
```

#### 4. Timing de Animaciones

✅ **Solución:** Esperar a que animación termine

```csharp
// Esperar que elemento sea visible Y esté quieto
WaitHelper.WaitUntil(
    () => element.IsVisible && element.BoundingRectangle.IsEmpty == false,
    5000,
    conditionDescription: "elemento visible y renderizado"
);

// Pequeño delay adicional para animaciones
Thread.Sleep(300);
```

## Problemas de Permisos

### Aplicación Requiere Permisos de Admin

**Síntomas:**
- App no lanza
- "Access denied"
- UAC prompt aparece pero tests no pueden interactuar

✅ **Soluciones:**

#### Opción 1: Ejecutar Test Runner como Admin

```bash
# Abrir terminal como Admin
dotnet test
```

En IDE:
- Visual Studio: Ejecutar VS como Admin
- Rider: Ejecutar Rider como Admin

#### Opción 2: Deshabilitar UAC para la App

Crear manifest para la app que especifique nivel requerido:

```xml
<!-- app.manifest -->
<trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
  <security>
    <requestedPrivileges>
      <requestedExecutionLevel level="asInvoker" uiAccess="false" />
    </requestedPrivileges>
  </security>
</trustInfo>
```

#### Opción 3: Modificar UAC Settings (NO recomendado para producción)

Solo para entorno de testing local:
1. Win + R → `UserAccountControlSettings`
2. Bajar slider a "Never notify"
3. Reiniciar

## Problemas de Sesión

### Tests Fallan en CI (Session Lock)

**Síntomas:**
- Tests pasan local
- Fallan en CI con timeout
- Error: "Window not available"

✅ **Solución:** Ver [CI/CD Guide](./ci-cd.md) - Sección "Sesión Interactiva"

Resumen:
- Usar self-hosted runner
- Configurar auto-login
- Ejecutar runner en sesión interactiva (no como servicio)

### Lock Screen Interrumpe Tests

✅ **Solución:** Deshabilitar lock screen

```powershell
# PowerShell como Admin
New-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" `
  -Name "InactivityTimeoutSecs" -Value 0 -PropertyType DWORD -Force

# Deshabilitar screensaver
Set-ItemProperty -Path "HKCU:\Control Panel\Desktop" -Name ScreenSaveActive -Value 0
```

## Problemas de Resolución/DPI

### Tests Fallan con Resolución Diferente

**Síntomas:**
- Click en coordenadas incorrectas
- Screenshots muestran elementos cortados
- BoundingRectangle no coincide

✅ **Solución 1:** No usar coordenadas, usar AutomationIds

❌ **No hacer esto:**
```csharp
Mouse.Click(new Point(100, 200));  // Coordenadas absolutas
```

✅ **Hacer esto:**
```csharp
var button = FindElement("ButtonId");
button.Click();  // Click relativo al elemento
```

✅ **Solución 2:** Configurar resolución fija en CI

```powershell
# PowerShell en runner
Set-DisplayResolution -Width 1920 -Height 1080 -Force
```

✅ **Solución 3:** DPI Awareness

Si tu app es DPI-aware, asegúrate que tests también lo sean:

```xml
<!-- app.manifest -->
<application xmlns="urn:schemas-microsoft-com:asm.v3">
  <windowsSettings>
    <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true</dpiAware>
  </windowsSettings>
</application>
```

## Problemas de FlaUI

### UIA3 vs UIA2

**¿Cuándo usar UIA2 en lugar de UIA3?**

Usa UIA2 si:
- ❌ App legacy (Win32, WinForms antiguo)
- ❌ UIA3 no encuentra elementos
- ❌ App usa controles custom/third-party

```csharp
// Cambiar en AppLauncher.cs
// De:
_automation = new UIA3Automation();

// A:
_automation = new UIA2Automation();
```

### Diferencias Importantes

| Aspecto | UIA3 | UIA2 |
|---------|------|------|
| Performance | ✅ Más rápido | ❌ Más lento |
| Compatibilidad | ✅ Apps modernas | ✅ Apps legacy |
| Mantenimiento | ✅ Activo | ⚠️ Legacy |
| Features | ✅ Más completo | ❌ Limitado |

## Problemas de Performance

### Tests Muy Lentos

**Causas:**

#### 1. Timeouts Muy Largos

✅ **Solución:** Ajustar timeouts apropiadamente

```json
// appsettings.json
{
  "DefaultTimeout": 3000  // Reducir de 5000 a 3000 si app es rápida
}
```

#### 2. Esperas Innecesarias

❌ **Problema:**
```csharp
Thread.Sleep(5000);  // Siempre espera 5 segundos
```

✅ **Solución:**
```csharp
WaitHelper.WaitForElement(window, "ButtonId", 5000);
// Retorna inmediatamente cuando encuentra elemento
```

#### 3. App Lanza Lento

✅ **Solución:** Aumentar timeout de launch

```json
{
  "DefaultTimeout": 10000
}
```

```csharp
var window = launcher.LaunchApp(appPath, timeoutMs: 15000);
```

#### 4. Logs Excesivos

✅ **Solución:** Reducir nivel de log

```json
{
  "Serilog": {
    "MinimumLevel": "Information"  // Cambiar de Debug a Information
  }
}
```

## Problemas de Reporting

### Allure Report No Se Genera

**Síntomas:**
```bash
allure generate ...
# Error: No test data found
```

✅ **Soluciones:**

#### 1. Verificar allure-results existe

```bash
ls src/Hipos.Tests/bin/Debug/net8.0-windows/allure-results/
# Debería tener archivos: *-result.json, *-container.json
```

Si está vacío:
- Verificar que Allure.NUnit esté instalado
- Verificar que tests se ejecutaron completamente

#### 2. Verificar Allure CLI instalado

```bash
allure --version
# Debería mostrar: 2.x.x
```

Si no está instalado:
```bash
choco install allure-commandline
```

### Screenshots No Aparecen en Reporte

✅ **Solución:**

Verificar que screenshots se guardaron:
```bash
ls src/Hipos.Tests/bin/Debug/net8.0-windows/allure-results/screenshots/
```

Si está vacío:
- Test no falló (screenshots solo en fallos)
- Verificar permisos de escritura
- Verificar que `BaseTest.TearDown` se ejecuta

## Problemas de Configuración

### appsettings.json No Se Lee

**Síntomas:**
- `AppPath` es null o vacío
- Configuración usa defaults

✅ **Soluciones:**

#### 1. Verificar archivo existe en output

```bash
ls src/Hipos.Tests/bin/Debug/net8.0-windows/appsettings.json
```

Si no existe, verificar .csproj:
```xml
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

#### 2. Verificar formato JSON válido

Usar validador JSON online o:
```bash
# PowerShell
Get-Content appsettings.json | ConvertFrom-Json
```

### Variables de Entorno No Sobrescriben

```bash
# Verificar sintaxis correcta
# Windows CMD
set AppPath=C:\path\to\app.exe
echo %AppPath%

# PowerShell
$env:AppPath = "C:\path\to\app.exe"
$env:AppPath

# Bash (Git Bash en Windows)
export AppPath="C:/path/to/app.exe"
echo $AppPath
```

## FAQ / Limitaciones

### ¿Por qué mis tests son flaky?

**Respuesta:** 99% de las veces es por **waits insuficientes o incorrectas**.

- ❌ No uses `Thread.Sleep()` hardcodeado
- ✅ Usa `WaitHelper.WaitForElement()` y variantes
- ✅ Espera condiciones específicas, no tiempos arbitrarios

### ¿Puedo ejecutar tests en paralelo?

**Respuesta:** NO recomendado para UI tests.

**Razones:**
- Solo una app puede tener foco a la vez
- Elementos en ventanas de fondo pueden no ser accesibles
- Race conditions entre tests

Si quieres paralelizar:
- Usa múltiples runners/VMs físicas (no en misma máquina)
- O ejecuta cada test en VM/container aislado

### ¿Funciona con aplicaciones Remote Desktop?

**Respuesta:** NO directamente.

UI Automation necesita acceso directo a la sesión de Windows. RDP crea una sesión separada.

**Alternativas:**
- Ejecutar tests EN el servidor remoto (self-hosted runner)
- Usar tecnologías de virtualización (Hyper-V, VMware) en lugar de RDP

### ¿Puedo testear apps 32-bit desde tests 64-bit?

**Respuesta:** SÍ, FlaUI maneja ambas arquitecturas.

Asegúrate que la configuración de build sea correcta:
```xml
<PropertyGroup>
  <PlatformTarget>AnyCPU</PlatformTarget>
</PropertyGroup>
```

### ¿Funciona con aplicaciones Electron/Chromium?

**Respuesta:** PARCIALMENTE.

- Elementos estándar (botones, textboxes) funcionan
- Custom controls pueden no ser accesibles
- Considera Selenium/Playwright para apps web-based

## Herramientas de Debug

### Inspect.exe (Windows SDK)
- Ver estructura de UI
- Identificar AutomationIds
- Verificar propiedades de elementos

### FlaUI Inspect
```bash
# Instalar
dotnet tool install -g FlaUI.Inspect

# Ejecutar
flaui-inspect
```

### Spy++ (Visual Studio)
- Analizar jerarquía de ventanas
- Ver mensajes de Windows
- Debug de eventos

## Obtener Ayuda

Si tu problema no está listado:

1. **Revisar logs:** `src/Hipos.Tests/bin/Debug/net8.0-windows/logs/`
2. **Capturar screenshot manual:** Durante debug, ver qué está pasando
3. **Usar Inspect.exe:** Verificar que elementos sean accesibles
4. **Simplificar:** Crear test mínimo que reproduzca el problema
5. **Buscar en GitHub Issues:** [FlaUI Issues](https://github.com/FlaUI/FlaUI/issues)

## Próximos Pasos

- **[Contributing](./contributing.md)** - Contribuir al proyecto
- **[Framework Guide](./framework-guide.md)** - Volver a guías
