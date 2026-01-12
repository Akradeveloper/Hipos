---
sidebar_position: 2
---

# Getting Started

Esta guía te llevará desde cero hasta ejecutar tu primer test en **menos de 5 minutos**.

## Prerrequisitos

Antes de comenzar, asegúrate de tener instalado:

### Software Requerido

| Software | Versión Mínima | Descarga |
|----------|----------------|----------|
| **Windows** | 10 o superior | - |
| **.NET SDK** | 8.0 | [Download](https://dotnet.microsoft.com/download) |
| **Git** | Cualquiera | [Download](https://git-scm.com/) |

### Software Recomendado

- **Visual Studio 2022** o **JetBrains Rider** para desarrollo
- **Allure CLI** para generar reportes localmente
- **Windows SDK** con UI Automation Tools (incluye Inspect.exe)

### Verificar Instalación

```bash
# Verificar .NET
dotnet --version
# Debería mostrar: 8.0.x o superior

# Verificar Git
git --version
```

## Instalación

### 1. Clonar el Repositorio

```bash
git clone https://github.com/yourusername/Hipos.git
cd Hipos
```

### 2. Restaurar Dependencias

```bash
dotnet restore
```

### 3. Build del Proyecto

```bash
dotnet build
```

Si todo está correcto, deberías ver:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Tu Primer Test

### 1. Ejecutar Tests Básicos (Demo)

Los tests demo verifican que la Calculadora se abre y está funcional:

```bash
dotnet test --filter "Category=Demo"
```

### 2. Ejecutar Tests Complejos

Tests que realizan operaciones matemáticas reales:

```bash
dotnet test --filter "Category=Complex"
```

### 3. Ejecutar Todos los Tests

```bash
dotnet test
```

Deberías ver algo como:

```
✅ Passed! - Failed: 0, Passed: 11, Skipped: 0, Total: 11
⏱️  Duration: ~16s
```

### 3. Ver los Resultados

Los resultados se generan en múltiples formatos:

```bash
# Resultados TRX (XML)
src/Hipos.Tests/TestResults/*.trx

# Resultados Allure
src/Hipos.Tests/bin/Debug/net8.0-windows/allure-results/

# Logs
src/Hipos.Tests/bin/Debug/net8.0-windows/logs/
```

## Generar Reporte Allure

### Instalar Allure (solo primera vez)

#### Windows (Chocolatey)
```bash
choco install allure-commandline
```

#### Windows (Scoop)
```bash
scoop install allure
```

#### Manual
Descarga desde [GitHub Releases](https://github.com/allure-framework/allure2/releases) y añade al PATH.

### Generar y Abrir Reporte

```bash
# Generar reporte
allure generate src/Hipos.Tests/bin/Debug/net8.0-windows/allure-results -o allure-report --clean

# Abrir en navegador
allure open allure-report
```

El reporte incluye:
- ✅ Estado de cada test (passed/failed)
- 📊 Gráficas y estadísticas
- 📸 Screenshots de fallos
- 📄 Logs detallados
- 🏷️ Tags y categorías

## Estructura del Proyecto

```
Hipos/
├── src/
│   ├── Hipos.Framework/        # Core del framework
│   │   ├── Core/               # AppLauncher, BaseTest, ScreenshotHelper
│   │   ├── Utils/              # WaitHelper, ElementWrapper, RetryPolicy
│   │   └── Config/             # ConfigManager
│   └── Hipos.Tests/            # Tests y Page Objects
│       ├── PageObjects/        # CalculatorPage, BasePage
│       ├── Tests/              # CalculatorTests (11 tests)
│       └── appsettings.json    # Configuración
├── website/                    # Documentación Docusaurus
└── .github/workflows/          # CI/CD (ui-tests.yml, docs.yml)
```

**Nota:** El proyecto `Hipos.DemoApp` fue eliminado. Los tests ahora funcionan contra la **Calculadora de Windows** (`calc.exe`).

## Configuración

### appsettings.json

Configura la aplicación a testear en `src/Hipos.Tests/appsettings.json`:

```json
{
  "AppPath": "calc.exe",
  "DefaultTimeout": 15000,
  "RetryCount": 3,
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/test-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

**Parámetros importantes:**
- `AppPath`: Ruta al ejecutable (absoluta, relativa, o en PATH)
  - `calc.exe` - Calculadora de Windows
  - `notepad.exe` - Bloc de notas
  - `C:\MiApp\App.exe` - Tu aplicación personalizada
- `DefaultTimeout`: Timeout en milisegundos (15s recomendado para apps UWP)
- `RetryCount`: Número de reintentos para errores transitorios

**Aplicaciones Soportadas:**
- ✅ Win32 clásicas (Notepad, Paint, apps legacy)
- ✅ Apps UWP modernas (Calculadora, apps de Windows Store)
- ✅ WPF/WinForms (tus aplicaciones personalizadas)

### Variables de Entorno

También puedes usar variables de entorno (sobrescriben appsettings.json):

```bash
# Windows
set AppPath=C:\path\to\your\app.exe
set DefaultTimeout=10000

# PowerShell
$env:AppPath = "C:\path\to\your\app.exe"
$env:DefaultTimeout = "10000"
```

## Ejecutar Tests desde IDE

### Visual Studio

1. Abrir `Hipos.sln`
2. Ir a **Test Explorer** (Ctrl+E, T)
3. Click derecho → Run/Debug tests

### Rider

1. Abrir `Hipos.sln`
2. Ir a **Unit Tests** (Alt+8)
3. Click derecho → Run/Debug tests

## Próximos Pasos

Ahora que tienes el framework funcionando:

1. **[Arquitectura](./architecture.md)** - Entiende cómo está organizado
2. **[Framework Guide](./framework-guide.md)** - Aprende a crear tus propios tests
3. **[Reporting](./reporting-logging.md)** - Personaliza reportes y logs
4. **[CI/CD](./ci-cd.md)** - Integra con tu pipeline

## Troubleshooting Rápido

### Error: "No se encontró el ejecutable"

**Para apps del sistema** (calc, notepad): Usa solo el nombre del ejecutable:
```json
"AppPath": "calc.exe"  // ✅ Correcto
```

**Para apps personalizadas**: Usa ruta absoluta o relativa:
```json
"AppPath": "C:\\MiApp\\bin\\Debug\\App.exe"  // ✅ Correcto
```

### Tests se cuelgan o timeout

**Apps UWP (Calculadora, etc.):**
- Aumenta `DefaultTimeout` a 15000 o más
- El framework usa búsqueda híbrida (primeros 5s strict, luego relaxed)
- Revisa logs en `logs/test-*.log` para ver qué modo de búsqueda se usó

**Apps Win32 clásicas:**
- `DefaultTimeout` de 5000-10000 suele ser suficiente
- Verifica que la app no requiera permisos de admin
- Revisa logs en `src/Hipos.Tests/bin/Debug/net8.0-windows/logs/`

### No se generan screenshots

- Verifica que FlaUI pueda capturar la ventana
- Revisa permisos de escritura en directorio `allure-results/`

Para más ayuda, consulta [Troubleshooting](./troubleshooting.md).
