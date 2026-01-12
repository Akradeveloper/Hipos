---
sidebar_position: 5
---

# Reporting & Logging

Hipos incluye reporting completo con Allure y logging estructurado con Serilog.

## Allure Reports

Allure genera reportes HTML interactivos y profesionales.

### Generar Reporte

```bash
# 1. Ejecutar tests (genera allure-results/)
dotnet test

# 2. Generar reporte HTML
allure generate src/Hipos.Tests/bin/Debug/net8.0-windows/allure-results -o allure-report --clean

# 3. Abrir en navegador
allure open allure-report
```

### Contenido del Reporte

El reporte Allure incluye:

- 📊 **Overview**: Estadísticas generales, gráficas de pasados/fallados
- 📋 **Suites**: Organización por TestFixture
- 📈 **Graphs**: Gráficas de severidad, duración, etc.
- ⏱️ **Timeline**: Línea de tiempo de ejecución
- 🏷️ **Tags**: Filtrado por categorías y tags
- 📦 **Packages**: Organización por namespace
- 📸 **Attachments**: Screenshots, logs, evidencias

### Anotar Tests con Allure

#### Suite
```csharp
[AllureSuite("Login Tests")]
public class LoginTests : BaseTest
{
}
```

#### Tags
```csharp
[Test]
[AllureTag("smoke", "login", "critical")]
public void MyTest() { }
```

#### Severidad
```csharp
[Test]
[AllureSeverity(SeverityLevel.critical)]  // critical, blocker, normal, minor, trivial
public void MyTest() { }
```

#### Descripción
```csharp
[Test]
[AllureDescription("Este test verifica que el login funcione correctamente")]
public void MyTest() { }
```

#### Steps
```csharp
[Test]
public void MyTest()
{
    AllureApi.Step("Paso 1: Navegar a login");
    // código
    
    AllureApi.Step("Paso 2: Ingresar credenciales");
    // código
    
    AllureApi.Step("Paso 3: Verificar dashboard");
    // código
}
```

#### Adjuntar Evidencia

```csharp
// Screenshot
var screenshotPath = "path/to/screenshot.png";
AllureApi.AddAttachment(
    name: "Screenshot Error",
    type: "image/png",
    content: File.ReadAllBytes(screenshotPath),
    fileExtension: ".png"
);

// Texto
AllureApi.AddAttachment(
    name: "Response Body",
    type: "text/plain",
    content: Encoding.UTF8.GetBytes(responseText),
    fileExtension: ".txt"
);

// JSON
AllureApi.AddAttachment(
    name: "API Response",
    type: "application/json",
    content: Encoding.UTF8.GetBytes(jsonString),
    fileExtension: ".json"
);
```

### Screenshots Automáticos

`BaseTest` captura screenshots automáticamente cuando un test falla:

1. Test falla con excepción
2. `TearDown` detecta fallo
3. `ScreenshotHelper.TakeScreenshot()` captura pantalla
4. Screenshot se adjunta a Allure
5. Disponible en reporte bajo "Attachments"

### Ejemplo Completo

```csharp
[TestFixture]
[AllureSuite("Calculator Tests")]
[AllureFeature("Basic Operations")]
public class CalculatorTests : BaseTest
{
    [Test]
    [Category("Smoke")]
    [AllureTag("calculator", "addition", "smoke")]
    [AllureSeverity(SeverityLevel.critical)]
    [AllureDescription("Verifica que la suma de dos números positivos funcione correctamente")]
    public void VerifyAddition_PositiveNumbers_Success()
    {
        // Arrange
        AllureApi.Step("Preparar página de calculadora");
        var calcPage = new CalculatorPage(MainWindow!);
        
        // Act
        AllureApi.Step("Ingresar números: 5 y 3");
        calcPage.EnterNumbers(5, 3);
        
        AllureApi.Step("Seleccionar operación: +");
        calcPage.SelectOperation("+");
        
        AllureApi.Step("Hacer click en Calculate");
        calcPage.ClickCalculate();
        
        Thread.Sleep(500);
        
        AllureApi.Step("Obtener resultado");
        var result = calcPage.GetCalculationResult();
        
        // Assert
        AllureApi.Step("Verificar que resultado sea 8");
        Assert.That(result, Does.Contain("8"));
    }
}
```

## Serilog Logging

Hipos usa Serilog para logging estructurado.

### Configuración

En `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/test-.log",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

### Niveles de Log

| Nivel | Uso | Ejemplo |
|-------|-----|---------|
| `Verbose` | Detalles muy granulares | Cada interacción con elemento |
| `Debug` | Información de debug | Búsqueda de elementos, waits |
| `Information` | Flujo general | "Test iniciado", "App lanzada" |
| `Warning` | Situaciones anormales pero recuperables | Reintentos, timeouts |
| `Error` | Errores que requieren atención | Fallos inesperados |
| `Fatal` | Errores críticos | App crash |

### Usar Serilog en Tu Código

```csharp
using Serilog;

public class MyPage : BasePage
{
    public void MyMethod()
    {
        Log.Information("Ejecutando MyMethod");
        Log.Debug("Buscando elemento con ID: {AutomationId}", "ButtonId");
        
        try
        {
            // código
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error en MyMethod");
            throw;
        }
    }
}
```

### Logging Estructurado

Serilog permite logging estructurado con propiedades:

```csharp
Log.Information("Usuario {Username} intentó login desde {IpAddress}", username, ipAddress);
// Output: Usuario john intentó login desde 192.168.1.1
```

### Ubicación de Logs

Por defecto, los logs se guardan en:
```
src/Hipos.Tests/bin/Debug/net8.0-windows/logs/test-YYYYMMDD.log
```

Rolling diario significa que cada día se crea un nuevo archivo.

### Ver Logs en Reporte Allure

`BaseTest` automáticamente adjunta los logs al reporte de Allure en el `TearDown`:

1. Lee el archivo de log más reciente
2. Adjunta como attachment en Allure
3. Disponible en reporte bajo "Attachments" → "Test Logs"

## Artifacts en CI

### GitHub Actions

El workflow `ui-tests.yml` sube automáticamente artifacts:

```yaml
- name: Upload artifacts - Allure Report
  uses: actions/upload-artifact@v4
  with:
    name: allure-report
    path: allure-report/
    retention-days: 30
```

### Artifacts Disponibles

1. **test-results-trx**: Resultados en formato TRX (XML)
2. **allure-results**: Datos raw de Allure
3. **allure-report**: Reporte HTML generado
4. **screenshots**: Screenshots de fallos
5. **test-logs**: Archivos de log de Serilog

### Descargar Artifacts

En GitHub:
1. Ir a la pestaña "Actions"
2. Click en el workflow run
3. Scroll down a "Artifacts"
4. Click para descargar ZIP

## Personalización Avanzada

### Custom Allure Categories

Crea `categories.json` en la carpeta de results:

```json
[
  {
    "name": "UI Timeout Errors",
    "matchedStatuses": ["failed"],
    "messageRegex": ".*TimeoutException.*"
  },
  {
    "name": "Element Not Found",
    "matchedStatuses": ["failed"],
    "messageRegex": ".*ElementNotAvailableException.*"
  }
]
```

### Múltiples Sinks de Serilog

```json
{
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/test-.log",
          "rollingInterval": "Day"
        }
      },
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}"
        }
      }
    ]
  }
}
```

### Allure Environment Info

Crea `environment.properties` en allure-results:

```properties
Browser=N/A
OS=Windows 10
Framework=Hipos v1.0
.NET=8.0
```

### Allure Executor Info

Para mostrar info de CI en el reporte, crea `executor.json`:

```json
{
  "name": "GitHub Actions",
  "type": "github",
  "url": "https://github.com/user/repo/actions/runs/123456",
  "buildOrder": 123,
  "buildName": "CI Build #123",
  "buildUrl": "https://github.com/user/repo/actions/runs/123456",
  "reportUrl": "https://user.github.io/repo/allure-report/"
}
```

## Mejores Prácticas

### Logging

✅ **DO:**
- Log en nivel adecuado (Information para flujo, Debug para detalles)
- Usa logging estructurado con propiedades
- Log antes y después de acciones críticas
- Log excepciones con `Log.Error(ex, "mensaje")`

❌ **DON'T:**
- No hagas log de información sensible (passwords, tokens)
- No uses `Console.WriteLine()` (usa Serilog)
- No llenes logs con información irrelevante

### Allure

✅ **DO:**
- Usa Steps para documentar flujo de test
- Adjunta evidencia relevante (screenshots, JSON)
- Usa tags para categorizar tests
- Establece severidad apropiada

❌ **DON'T:**
- No adjuntes archivos enormes (> 10MB)
- No uses Steps para cada línea de código
- No dupliques información (ej: Steps + Description con lo mismo)

### Screenshots

✅ **DO:**
- Deja que BaseTest capture automáticamente en fallos
- Captura manualmente solo cuando necesites evidencia específica
- Usa nombres descriptivos

❌ **DON'T:**
- No captures screenshot en cada paso (genera muchos archivos)
- No captures antes de verificar si hay ventana disponible

## Troubleshooting

### "No se generó reporte Allure"

Verifica que:
1. `allure-results/` exista y tenga archivos JSON
2. Allure CLI esté instalado: `allure --version`
3. Ejecutaste: `allure generate ...`

### "Screenshots no aparecen en reporte"

Verifica:
1. Directorio `allure-results/screenshots/` exista
2. Screenshots se copiaron a `allure-results/` antes de generar reporte
3. BaseTest.TearDown se está ejecutando

### "Logs están vacíos"

Verifica:
1. Nivel de log en appsettings.json (usa Debug para más detalles)
2. Permisos de escritura en directorio `logs/`
3. Serilog se inicializó en OneTimeSetUp

## Ejemplo: Reportes en Azure DevOps

Si usas Azure DevOps, publica el reporte:

```yaml
- task: PublishTestResults@2
  inputs:
    testResultsFormat: 'NUnit'
    testResultsFiles: '**/test-results.xml'

- task: PublishBuildArtifacts@1
  inputs:
    PathtoPublish: 'allure-report'
    ArtifactName: 'allure-report'
```

## Próximos Pasos

- **[CI/CD](./ci-cd.md)** - Integra con pipelines
- **[Troubleshooting](./troubleshooting.md)** - Soluciona problemas
