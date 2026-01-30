# Hipos - Windows UI Automation Framework

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![FlaUI](https://img.shields.io/badge/FlaUI-4.0-blue)](https://github.com/FlaUI/FlaUI)
[![NUnit](https://img.shields.io/badge/NUnit-4.0-green)](https://nunit.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Framework enterprise de automatización para aplicaciones Windows (Win32, WPF, WinForms) con C#, FlaUI, NUnit, ExtentReports 5 y soporte completo para CI/CD.

## 🎯 Características

- **🚀 Fácil de Usar**: Page Objects, waits inteligentes con polling adaptativo, helpers robustos
- **⚡ Rendimiento Optimizado**: Polling adaptativo y timeouts dinámicos que se ajustan automáticamente
- **🔧 MSAA Integration**: Soporte para Microsoft Active Accessibility para aplicaciones legacy
- **📊 Reporting Completo**: ExtentReports 5 con screenshots automáticos y logs detallados
- **🧪 BDD Support**: SpecFlow para tests con sintaxis Gherkin
- **⚙️ CI/CD Ready**: Workflows para GitHub Actions, guía para Azure DevOps
- **🧪 Testing Robusto**: Retry policies, manejo de errores, categorización (smoke/regression)
- **📚 Documentación Completa**: Portal Docusaurus con guías, ejemplos y troubleshooting
- **🔧 Mantenible**: Arquitectura limpia con patrones de diseño probados

## 📋 Tabla de Contenido

- [Quick Start](#-quick-start)
- [Estructura del Proyecto](#-estructura-del-proyecto)
- [Ejecutar Tests](#-ejecutar-tests)
- [Generar Reportes](#-generar-reportes)
- [Documentación](#-documentación)
- [CI/CD](#-cicd)
- [Tech Stack](#-tech-stack)
- [Ejemplos](#-ejemplos)
- [Mejoras Futuras](#-mejoras-futuras)
- [Contribuir](#-contribuir)
- [Licencia](#-licencia)

## ⚡ Quick Start

### Prerrequisitos

- **Windows 10 o superior**
- **.NET 8 SDK** ([Descargar](https://dotnet.microsoft.com/download))
- **Git**

### Instalación

```bash
# 1. Clonar repositorio
git clone https://github.com/Akradeveloper/Hipos.git
cd Hipos

# 2. Restaurar dependencias
dotnet restore

# 3. Build
dotnet build

# 4. Ejecutar  tests
# Todos los tests
dotnet test

# Solo tests básicos
dotnet test --filter "Category=Demo"

# Solo tests complejos (operaciones matemáticas)
dotnet test --filter "Category=Complex"

# Test específico
dotnet test --filter "Name=PerformSimpleAddition"

# Con logging detallado
dotnet test --logger "console;verbosity=detailed"
```

### ¿Funcionó? 🎉

Si ves:
```
Passed!  - Failed:     0, Passed:     3, Skipped:     0
```

¡Estás listo! Continúa con la [documentación completa](./website/docs/intro.md).

## 📁 Estructura del Proyecto

```
Hipos/
├── src/
│   ├── Hipos.Framework/           # Core del framework
│   │   ├── Core/                  # AppLauncher, ScreenshotHelper
│   │   ├── Utils/                 # WaitHelper, MsaaHelper, AdaptiveTimeoutManager, ExtentReportManager, CucumberJsonReportGenerator
│   │   └── Config/                # ConfigManager
│   └── Hipos.Tests/               # Tests y Page Objects
│       ├── PageObjects/           # HiposLoginPage, BasePage (MSAA)
│       ├── StepDefinitions/       # SpecFlow step definitions
│       ├── Features/              # Gherkin feature files
│       ├── Hooks/                 # TestHooks (SpecFlow lifecycle)
│       └── appsettings.json       # Configuración con timeouts adaptativos
├── website/                       # Documentación Docusaurus
│   ├── docs/                      # 9 páginas de documentación
│   │   ├── intro.md               # Introducción
│   │   ├── getting-started.md     # Quick start
│   │   ├── architecture.md        # Diagramas y arquitectura
│   │   ├── framework-guide.md     # Guía del framework
│   │   ├── examples.md            # Ejemplos de tests
│   │   ├── reporting-logging.md   # Reportes y logs
│   │   ├── ci-cd.md               # CI/CD
│   │   ├── troubleshooting.md     # Troubleshooting
│   │   └── contributing.md        # Cómo contribuir
│   └── src/                       # Componentes React
├── .github/workflows/             # CI/CD GitHub Actions
│   ├── ui-tests.yml               # Workflow de tests
│   └── docs.yml                   # Deploy de documentación
├── Hipos.sln                      # Solución .NET (2 proyectos)
└── README.md                      # Este archivo
```

**Nota:** El framework usa FlaUI para lanzar aplicaciones y gestionar ventanas, y MSAA (Microsoft Active Accessibility) accediendo a través de los handles de ventana de FlaUI para interacciones con elementos UI. Incluye soporte para polling adaptativo y timeouts dinámicos.

## 🧪 Ejecutar Tests

### Todos los Tests

```bash
dotnet test
```

### Por Categoría

```bash
# Solo tests básicos (Demo)
dotnet test --filter "Category=Demo"

# Solo tests complejos (operaciones matemáticas)
dotnet test --filter "Category=Complex"
```

### Tests Específicos

```bash
# Por nombre
dotnet test --filter "FullyQualifiedName~VerifyMainWindowOpens"

# Por suite
dotnet test --filter "FullyQualifiedName~SmokeTests"
```

### Desde IDE

#### Visual Studio
1. Abrir `Hipos.sln`
2. **Test Explorer** (Ctrl+E, T)
3. Click derecho → Run/Debug

#### JetBrains Rider
1. Abrir `Hipos.sln`
2. **Unit Tests** (Alt+8)
3. Click derecho → Run/Debug

### Usando Scripts de PowerShell

El proyecto incluye scripts útiles en la carpeta `scripts/`:

#### Setup Inicial

```bash
# Configura el entorno de desarrollo completo
.\scripts\setup-dev.ps1
```

Verifica e instala:
- .NET SDK 8.0+
- Node.js (para documentación Docusaurus)
- Dependencias del proyecto

#### Ejecutar Tests

```bash
# Todos los tests
.\scripts\run-tests.ps1

# Por categoría
.\scripts\run-tests.ps1 -Category Demo
.\scripts\run-tests.ps1 -Category Complex

# Con configuración específica
.\scripts\run-tests.ps1 -Configuration Release

# Abrir reporte automáticamente
.\scripts\run-tests.ps1 -Category Demo -OpenReport
```

**Parámetros disponibles:**
- `-Category`: Filtrar tests por categoría (Demo, Complex, Smoke, etc.)
- `-Configuration`: Debug o Release (default: Debug)
- `-OpenReport`: Abre el reporte HTML automáticamente después de los tests

## 📊 Ver Reportes HTML

Los reportes HTML se generan **automáticamente** después de ejecutar los tests con **ExtentReports 5**:

```bash
# 1. Ejecutar tests (genera el reporte automáticamente)
dotnet test

# 2. Abrir reporte en navegador (Windows)
start src\Hipos.Tests\bin\Debug\net8.0-windows\reports\extent-report.html

# O en PowerShell
Invoke-Item src\Hipos.Tests\bin\Debug\net8.0-windows\reports\extent-report.html
```

### Contenido del Reporte

- 📊 Dashboard con estadísticas visuales y gráficos
- 📸 Screenshots automáticos en caso de fallos
- 📄 Logs detallados paso a paso
- 🏷️ Categorización por categorías y descripciones
- ⏱️ Tiempos de ejecución y performance
- 🌙 Tema oscuro para mejor legibilidad

## 🔗 Integración con Jira/Xray

El framework genera automáticamente reportes en formato **Cucumber JSON** compatibles con **Jira Xray** para integración con sistemas de gestión de pruebas.

### Generación Automática

Cada vez que ejecutas los tests, se genera automáticamente un archivo `cucumber.json`:

```bash
# Ejecutar tests
dotnet test

# El archivo se genera en:
src\Hipos.Tests\bin\Debug\net8.0-windows\reports\cucumber.json
```

### Configuración

Puedes personalizar la generación del reporte en `appsettings.json`:

```json
{
  "Reporting": {
    "CucumberJsonPath": "reports/cucumber.json",
    "IncludeScreenshots": true
  }
}
```

**Opciones:**
- `CucumberJsonPath`: Ruta donde se guardará el archivo JSON
- `IncludeScreenshots`: Incluir screenshots como base64 en el JSON (para fallos)

### Importar a Xray

#### Opción 1: Interfaz Web de Xray

1. Ir a tu proyecto en Jira
2. Navegar a **Xray** → **Import Execution Results**
3. Seleccionar formato: **Cucumber JSON**
4. Subir el archivo `cucumber.json`
5. Configurar opciones de importación (crear nuevos tests, actualizar existentes, etc.)

#### Opción 2: API REST de Xray

```bash
# Xray Cloud
curl -H "Content-Type: application/json" \
     -X POST \
     -H "Authorization: Bearer YOUR_TOKEN" \
     --data @cucumber.json \
     https://xray.cloud.getxray.app/api/v2/import/execution/cucumber

# Xray Server/DC
curl -H "Content-Type: application/json" \
     -X POST \
     -u username:password \
     --data @cucumber.json \
     https://your-jira-instance.com/rest/raven/2.0/import/execution/cucumber
```

#### Opción 3: Integración en CI/CD

Ejemplo para GitHub Actions:

```yaml
- name: Upload results to Xray
  if: always()
  run: |
    curl -H "Content-Type: application/json" \
         -X POST \
         -H "Authorization: Bearer ${{ secrets.XRAY_TOKEN }}" \
         --data @src/Hipos.Tests/bin/Debug/net8.0-windows/reports/cucumber.json \
         https://xray.cloud.getxray.app/api/v2/import/execution/cucumber
```

### Estructura del Reporte

El archivo `cucumber.json` contiene:

- ✅ **Features y Scenarios** con sus nombres y descripciones
- 📝 **Steps** con resultados (passed/failed/skipped)
- ⏱️ **Duración** de cada step en nanosegundos
- 🏷️ **Tags** de SpecFlow para categorización
- 📸 **Screenshots** embebidos en base64 (si está habilitado)
- ❌ **Mensajes de error** para fallos

### Mapeo de Tags para Xray

Usa tags en tus features de SpecFlow para vincular con Xray:

```gherkin
@CALC-123 @regression
Feature: Calculadora
  
  @CALC-124 @smoke
  Scenario: Suma básica
    Given que he ingresado 5 en la calculadora
    When presiono sumar
    And ingreso 3
    And presiono igual
    Then el resultado debe ser 8
```

Los tags `@CALC-123` y `@CALC-124` se importarán a Xray y vincularán automáticamente con los Test Cases correspondientes.

### Beneficios de la Integración

- 📊 **Trazabilidad completa** entre requisitos, tests y ejecuciones
- 🔄 **Sincronización automática** de resultados en cada ejecución
- 📈 **Métricas y reportes** centralizados en Jira
- 👥 **Visibilidad** para todo el equipo (QA, Dev, PM)
- 🎯 **Gestión de test cases** directamente desde Jira

## 📚 Documentación

### Portal Docusaurus

El proyecto incluye documentación completa en Docusaurus:

```bash
# Instalar dependencias (solo primera vez)
cd website
npm install

# Iniciar servidor de desarrollo
npm start

# Abrir http://localhost:3000
```

### Contenido

- **[Introducción](./website/docs/intro.md)** - Qué es Hipos y características
- **[Getting Started](./website/docs/getting-started.md)** - Instalación y primer test
- **[Arquitectura](./website/docs/architecture.md)** - Diseño del framework con diagramas
- **[Framework Guide](./website/docs/framework-guide.md)** - Guía detallada de uso
- **[Reporting & Logging](./website/docs/reporting-logging.md)** - ExtentReports y Serilog
- **[CI/CD](./website/docs/ci-cd.md)** - Integración continua y limitaciones
- **[Troubleshooting](./website/docs/troubleshooting.md)** - Solución de problemas
- **[Contributing](./website/docs/contributing.md)** - Cómo contribuir

### Build de Documentación

```bash
cd website
npm run build
# Output en: website/build/
```

## 🔄 CI/CD

### GitHub Actions

El proyecto incluye workflows para:

1. **ui-tests.yml** - Ejecutar tests en cada push/PR
2. **docs.yml** - Deploy de documentación a GitHub Pages

### ⚠️ Limitación Importante

**Los tests de UI Desktop requieren sesión interactiva de Windows.**

GitHub-hosted runners NO tienen sesión de escritorio activa, por lo que los tests pueden fallar.

**Soluciones:**
- ✅ **Self-hosted runner** con auto-login configurado (recomendado)
- ✅ **Azure DevOps** con agent interactivo
- ✅ **VM dedicada** con Remote Desktop persistente

Ver [documentación de CI/CD](./website/docs/ci-cd.md) para detalles completos.

### Configurar Self-Hosted Runner

```bash
# 1. En máquina Windows con sesión activa
mkdir actions-runner && cd actions-runner

# 2. Descargar y configurar runner
# (Seguir instrucciones de GitHub: Settings → Actions → Runners → New runner)

# 3. Ejecutar en sesión interactiva (NO como servicio)
.\run.cmd
```

### Publicar Docs a GitHub Pages

1. En **Settings → Pages** del repositorio, en **Build and deployment → Source** selecciona **GitHub Actions** (no "Deploy from a branch").
2. El workflow `.github/workflows/docs.yml` publica automáticamente en cada push a `main` cuando cambian archivos en `website/**` o el propio workflow.
3. No hace falta configurar rama ni carpeta; el job de Actions sube el build de Docusaurus.
4. La URL del sitio queda: [https://akradeveloper.github.io/Hipos/](https://akradeveloper.github.io/Hipos/) (con `baseUrl: '/Hipos/'` ya configurado en Docusaurus).

## 🛠️ Tech Stack

| Componente | Tecnología | Propósito |
|------------|-----------|-----------|
| **Lenguaje** | C# + .NET 8 | Framework base |
| **Test Runner** | NUnit 4.0 | Ejecución de tests |
| **BDD Framework** | SpecFlow 4.0 | Tests con sintaxis Gherkin |
| **Gestión de Ventanas** | FlaUI 4.0 (UIA3) | Lanzamiento de aplicaciones y gestión de ventanas |
| **Interacción UI** | MSAA (vía FlaUI) | Interacción con elementos UI usando handles de FlaUI |
| **Reporting** | ExtentReports 5.0 | Reportes HTML profesionales |
| **Logging** | Serilog 3.1 | Logs estructurados |
| **Configuration** | Microsoft.Extensions.Configuration | Gestión de config |
| **CI/CD** | GitHub Actions | Integración continua |
| **Documentation** | Docusaurus 3 | Portal de documentación |

### Dependencias Principales

```xml
<!-- Framework -->
<PackageReference Include="FlaUI.UIA3" Version="4.0.0" />
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="8.0.0" />

<!-- Tests -->
<PackageReference Include="NUnit" Version="4.2.2" />
<PackageReference Include="SpecFlow" Version="4.0.7-beta" />
<PackageReference Include="ExtentReports" Version="5.0.4" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
```

**Nota:** El framework usa MSAA (Microsoft Active Accessibility) para interacciones con elementos UI, accediendo a través de los handles de ventana proporcionados por FlaUI. FlaUI se usa para lanzar aplicaciones y gestionar ventanas, mientras que MSAA (usando los handles de FlaUI) se usa para las interacciones con elementos UI.

## 📖 Ejemplos

### Test con SpecFlow (BDD)

```gherkin
Feature: HIPOS login
  Scenario: Successful login hides datactrl
    Given the HIPOS login page is open
    When I login with employee "-1" and password "000000"
    Then the datactrl element should not exist
```

```csharp
[Binding]
public class HiposLoginStepDefinitions : BaseStepDefinitions
{
    private HiposLoginPage? _loginPage;

    [Given("the HIPOS login page is open")]
    public void GivenTheHiposLoginPageIsOpen()
    {
        Assert.That(MainWindow, Is.Not.Null, "HIPOS window should be available");
        _loginPage = new HiposLoginPage(MainWindow!);
    }

    [When("I login with employee \"(.*)\" and password \"(.*)\"")]
    public void WhenILoginWithEmployeeAndPassword(string employee, string password)
    {
        _loginPage!.Login(employee, password);
    }

    [Then("the datactrl element should not exist")]
    public void ThenTheDataCtrlElementShouldNotExist()
    {
        Assert.That(_loginPage!.WaitForDataCtrlToDisappear(), Is.True);
    }
}
```

### Page Object (MSAA)

```csharp
public class HiposLoginPage : BasePage
{
    // MSAA selectors as static constants
    private static readonly string[] EmployeePath = { "employee" };
    private static readonly string[] PasswordPath = { "password" };
    private static readonly string[] LoginButtonPath = { "login" };
    
    public HiposLoginPage(Window window) : base(window) { }
    
    public void Login(string employee, string password)
    {
        EnsureWindowInForeground();
        SetElementText(employee, EmployeePath);
        SetElementText(password, PasswordPath);
        ClickElement(LoginButtonPath);
    }
    
    public bool WaitForDataCtrlToDisappear()
    {
        // Uses adaptive timeouts if enabled
        return WaitForElementToDisappear(DataCtrlPath);
    }
}
```

### Configuración

```json
{
  "AppPath": "C:\\hiposAut.exe",
  "DefaultTimeout": 15000,
  "Timeouts": {
    "Adaptive": true,
    "InitialTimeout": 5000,
    "MinTimeout": 2000,
    "MaxTimeout": 30000,
    "ResponseTimeWindow": 10
  },
  "Reporting": {
    "CucumberJsonPath": "reports/cucumber.json",
    "IncludeScreenshots": true
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [{
      "Name": "File",
      "Args": { "path": "logs/test-.log" }
    }]
  }
}
```

**Nota:** Los selectores MSAA se definen como constantes estáticas en los PageObjects, no en `appsettings.json`.

## ✅ Mejoras Futuras

Checklist de funcionalidades que serían valiosas:

### Framework
- [ ] Soporte para drag & drop
- [ ] Helpers para grids/tablas complejas
- [ ] Manejo de múltiples aplicaciones simultáneas
- [ ] Video recording de tests
- [ ] Ejecución paralela (con runners separados)
- [ ] Integración con Azure DevOps Test Plans

### Reporting
- [ ] Dashboard de métricas históricas
- [ ] Integración con SonarQube
- [ ] Personalización avanzada de reportes ExtentReports

### CI/CD
- [ ] Guía detallada de Azure DevOps
- [ ] Ejemplo de Jenkins pipeline
- [ ] Docker support (experimental con Windows containers)

### Testing
- [x] BDD con SpecFlow ✅ (implementado)
- [ ] Accessibility testing con Axe
- [ ] Performance benchmarks
- [ ] Visual regression testing

### Documentación
- [ ] Videos tutoriales
- [ ] Más ejemplos y snippets
- [ ] Traducción completa a inglés
- [ ] Guía de migración desde Coded UI

## 🤝 Contribuir

¡Las contribuciones son bienvenidas! Por favor lee [CONTRIBUTING.md](./website/docs/contributing.md) para:

- Reportar bugs
- Sugerir mejoras
- Contribuir código
- Actualizar documentación

### Proceso Rápido

1. Fork el repositorio
2. Crear branch: `git checkout -b feature/mi-feature`
3. Commit cambios: `git commit -m 'feat: añadir mi feature'`
4. Push: `git push origin feature/mi-feature`
5. Crear Pull Request

## 📄 Licencia

Este proyecto está bajo la licencia MIT. Ver archivo [LICENSE](LICENSE) para detalles.

## 🙏 Agradecimientos

- **[FlaUI](https://github.com/FlaUI/FlaUI)** - Librería de UI Automation
- **[NUnit](https://nunit.org/)** - Framework de testing
- **[ExtentReports](https://www.extentreports.com/)** - Framework de reporting
- **[Serilog](https://serilog.net/)** - Librería de logging
- **[Docusaurus](https://docusaurus.io/)** - Generador de documentación

## 📞 Soporte

- 📖 **Documentación**: [Portal Docusaurus](./website/docs/intro.md)
- 🐛 **Issues**: [GitHub Issues](https://github.com/Akradeveloper/Hipos/issues)
- 💬 **Discusiones**: [GitHub Discussions](https://github.com/Akradeveloper/Hipos/discussions)

---

**Construido con ❤️ usando C#, FlaUI y .NET**

