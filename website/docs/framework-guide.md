---
sidebar_position: 4
---

# Framework Guide

Guía detallada para usar los componentes del framework Hipos.

## AppLauncher

`AppLauncher` es un singleton que gestiona el lanzamiento y cierre de la aplicación bajo test.

### Uso Básico

```csharp
// Obtener instancia
var launcher = AppLauncher.Instance;

// Lanzar aplicación
var mainWindow = launcher.LaunchApp("calc.exe", timeoutMs: 15000);

// Obtener ventana principal en cualquier momento
var window = launcher.MainWindow;

// Cerrar aplicación
launcher.CloseApp();
```

### Características Principales

- **Singleton**: Una sola instancia para toda la suite de tests
- **Búsqueda Híbrida**: Sistema inteligente de detección de ventanas (ver abajo)
- **Timeout Configurable**: Espera configurable para que la ventana aparezca
- **Manejo Robusto**: Intenta cerrar gracefully, force-kill si falla
- **Logging Extensivo**: Registra todas las operaciones y ventanas encontradas

### Búsqueda Híbrida de Ventanas ⭐

Una característica clave que permite soportar apps UWP modernas (Calculadora) y Win32 clásicas (Notepad):

#### Fase 1: Strict Mode (primeros 5 segundos)
```csharp
// Busca ventanas SOLO del ProcessId exacto
if (windowProcessId != processId) {
    continue; // Skip
}
```

**Ventajas:**
- ✅ Seguro: no captura ventanas de otras apps (Cursor, VS Code, etc.)
- ✅ Rápido: encuentra apps Win32 clásicas inmediatamente
- ✅ Preciso: evita falsos positivos

#### Fase 2: Relaxed Mode (siguientes 10 segundos)
```csharp
// Busca por título de ventana si strict mode falló
if (window.Title.Contains("Calculadora") || 
    window.Title.Contains("Calculator")) {
    // ✅ Encontrada
}
```

**Ventajas:**
- ✅ Detecta apps UWP cuya ventana está en proceso hijo
- ✅ Excluye ventanas del sistema (Taskbar, Program Manager)
- ✅ Registra modo usado para debugging

#### Ejemplo de Log

```
[00:00.000] Lanzando aplicación: calc.exe
[00:00.100] Proceso lanzado con PID: 38092
[00:05.000] ⚠️ Switching to relaxed search mode (by window title)
[00:05.500] ✓ Ventana encontrada: 'Calculadora' (PID: 38124, Mode: Relaxed)
```

### Configuración

La ruta de la aplicación se configura en `appsettings.json`:

```json
{
  "AppPath": "calc.exe",           // Apps del sistema: solo nombre
  "DefaultTimeout": 15000          // 15s para apps UWP
}
```

**Ejemplos de AppPath:**

```json
// Calculadora de Windows (UWP)
"AppPath": "calc.exe"

// Notepad (Win32)
"AppPath": "notepad.exe"

// Tu aplicación personalizada
"AppPath": "C:\\MiApp\\bin\\Debug\\MiApp.exe"
```

## BaseTest

Clase base para todos tus tests. Proporciona hooks automáticos.

### Uso

```csharp
public class MyTests : BaseTest
{
    [Test]
    public void MyTest()
    {
        // MainWindow ya está disponible
        var page = new MainWindowPage(MainWindow!);
        
        // Tu lógica de test aquí
    }
}
```

### Hooks Automáticos

#### `[OneTimeSetUp]` - Una vez por fixture
1. Configura Serilog
2. Inicializa ConfigManager
3. **Lanza la aplicación UNA VEZ**
4. Proporciona `MainWindow` para todos los tests

**Ventaja:** La app NO se abre/cierra entre tests → **tests mucho más rápidos**

#### `[SetUp]` - Antes de cada test
1. Log de inicio del test
2. Crea instancia de Page Object si es necesario

#### `[TearDown]` - Después de cada test
1. Si el test falló:
   - Captura screenshot automáticamente
   - Adjunta a Allure report
2. Log de finalización del test

#### `[OneTimeTearDown]` - Una vez al final
1. **Cierra la aplicación**
2. Adjunta logs completos
3. Cierra Serilog

### Migración de SetUp/TearDown antiguo

```csharp
// ❌ Antiguo (app se abre/cierra cada test)
[SetUp]
public void SetUp() {
    AppLauncher.Instance.LaunchApp(...);
}

[TearDown]
public void TearDown() {
    AppLauncher.Instance.CloseApp();
}

// ✅ Nuevo (app se abre UNA VEZ)
// BaseTest ya maneja esto automáticamente con OneTimeSetUp/TearDown
```

### Propiedad Protegida

```csharp
protected Window? MainWindow { get; private set; }
```

Usa `MainWindow` en tus tests para crear Page Objects.

## WaitHelper

Utilidades para esperas explícitas. SIEMPRE usa esperas explícitas, no `Thread.Sleep()`.

### WaitUntil (Genérico)

```csharp
// Esperar condición custom
bool success = WaitHelper.WaitUntil(
    condition: () => someElement.IsVisible,
    timeoutMs: 5000,
    pollingIntervalMs: 500,
    conditionDescription: "elemento visible"
);

if (!success)
{
    // Timeout alcanzado
}
```

### WaitForElement

```csharp
// Esperar a que aparezca un elemento
var element = WaitHelper.WaitForElement(
    parent: MainWindow,
    automationId: "SubmitButton",
    timeoutMs: 5000
);

if (element == null)
{
    // Elemento no encontrado
}
```

### WaitForWindowTitle

```csharp
// Esperar ventana con título específico
bool found = WaitHelper.WaitForWindowTitle(
    title: "Settings",
    timeoutMs: 3000
);
```

### WaitForElementEnabled

```csharp
// Esperar a que elemento se habilite
bool enabled = WaitHelper.WaitForElementEnabled(
    element: button,
    timeoutMs: 5000
);
```

### WaitForElementClickable

```csharp
// Esperar a que elemento sea clickeable (enabled + visible)
bool clickable = WaitHelper.WaitForElementClickable(
    element: button,
    timeoutMs: 5000
);
```

### Mejores Prácticas

✅ **DO:**
```csharp
// Espera explícita
WaitHelper.WaitForElement(window, "ButtonId", 5000);
button.Click();
```

❌ **DON'T:**
```csharp
// Sleep hardcodeado
Thread.Sleep(2000);
button.Click();
```

## ElementWrapper

Wrapper sobre `AutomationElement` que añade esperas implícitas y logging.

### Crear Wrapper

```csharp
var element = WaitHelper.WaitForElement(window, "InputTextBox", 5000);
var wrapper = new ElementWrapper(element, defaultTimeout: 5000);
```

### Métodos

#### Click()
```csharp
wrapper.Click();
// Espera automática hasta que sea clickeable
// Log automático de la acción
```

#### SetText()
```csharp
wrapper.SetText("Hello World");
// Limpia texto existente (Ctrl+A, Delete)
// Establece nuevo texto
// Log automático
```

#### GetText()
```csharp
string text = wrapper.GetText();
// Intenta múltiples formas de obtener texto:
// 1. Text Pattern
// 2. Value Pattern
// 3. Name property
```

#### IsEnabled()
```csharp
bool enabled = wrapper.IsEnabled();
```

#### IsVisible()
```csharp
bool visible = wrapper.IsVisible();
// Verifica que no esté offscreen
```

#### WaitUntilExists()
```csharp
bool exists = wrapper.WaitUntilExists(timeoutMs: 3000);
```

### Acceso al Elemento Original

```csharp
AutomationElement original = wrapper.Element;
// Para casos avanzados donde necesitas API completa de FlaUI
```

## Page Objects

### BasePage

Clase base para todos los Page Objects.

```csharp
public abstract class BasePage
{
    protected Window Window { get; }
    protected int DefaultTimeout { get; }
    
    protected ElementWrapper FindElement(string automationId)
    {
        // Busca elemento con wait automático
        // Lanza excepción si no se encuentra
    }
    
    protected bool ElementExists(string automationId)
    {
        // Verifica existencia sin lanzar excepción
    }
    
    protected bool WaitForElementVisible(string automationId, int? timeoutMs = null)
    {
        // Espera hasta que elemento sea visible
    }
}
```

### Crear Tu Page Object

```csharp
public class LoginPage : BasePage
{
    // AutomationIds (constantes para fácil mantenimiento)
    private const string UsernameTextBoxId = "UsernameTextBox";
    private const string PasswordTextBoxId = "PasswordTextBox";
    private const string LoginButtonId = "LoginButton";
    private const string ErrorMessageId = "ErrorMessage";
    
    public LoginPage(Window window) : base(window)
    {
        AllureApi.Step("Navegando a LoginPage");
    }
    
    // Acciones atomicas
    public void EnterUsername(string username)
    {
        AllureApi.Step($"Ingresando username: {username}");
        var textBox = FindElement(UsernameTextBoxId);
        textBox.SetText(username);
    }
    
    public void EnterPassword(string password)
    {
        AllureApi.Step("Ingresando password");
        var textBox = FindElement(PasswordTextBoxId);
        textBox.SetText(password);
    }
    
    public void ClickLogin()
    {
        AllureApi.Step("Haciendo click en Login");
        var button = FindElement(LoginButtonId);
        button.Click();
    }
    
    // Acciones compuestas (fluent)
    public void Login(string username, string password)
    {
        AllureApi.Step($"Login con usuario: {username}");
        EnterUsername(username);
        EnterPassword(password);
        ClickLogin();
    }
    
    // Verificaciones
    public string GetErrorMessage()
    {
        var label = FindElement(ErrorMessageId);
        return label.GetText();
    }
    
    public bool IsLoginButtonEnabled()
    {
        var button = FindElement(LoginButtonId);
        return button.IsEnabled();
    }
}
```

### Convenciones

1. **AutomationIds como constantes**: Fácil de encontrar y cambiar
2. **Métodos públicos solo**: No expongas elementos directamente
3. **Naming**: Verbos para acciones (`Click`, `Enter`, `Select`)
4. **Allure Steps**: Documenta acciones importantes
5. **Retornar valores**: Solo para verificaciones, no elementos

## ConfigManager

Singleton para gestionar configuración.

### Uso

```csharp
var config = ConfigManager.Instance;

// Propiedades predefinidas
string appPath = config.AppPath;
int timeout = config.DefaultTimeout;
int retries = config.RetryCount;
string logLevel = config.LogLevel;

// Valores custom
string customValue = config.GetValue("MyCustomKey", "defaultValue");

// Sección completa
IConfigurationSection section = config.GetSection("MySection");
```

### appsettings.json

```json
{
  "AppPath": "path/to/app.exe",
  "DefaultTimeout": 5000,
  "RetryCount": 3,
  "Serilog": {
    "MinimumLevel": "Information"
  },
  "MyCustomKey": "MyValue",
  "MySection": {
    "SubKey": "SubValue"
  }
}
```

### Variables de Entorno

Sobrescriben valores de appsettings.json:

```bash
# Windows CMD
set AppPath=C:\other\app.exe

# PowerShell
$env:AppPath = "C:\other\app.exe"

# GitHub Actions / CI
env:
  AppPath: "C:\ci\app.exe"
```

## ScreenshotHelper

Helper estático para capturar screenshots.

### Uso

```csharp
string? path = ScreenshotHelper.TakeScreenshot("test-name");

if (path != null && File.Exists(path))
{
    // Screenshot guardado exitosamente
    AllureApi.AddAttachment("Screenshot", "image/png", File.ReadAllBytes(path));
}
```

### Características

- Captura ventana principal por defecto
- Fallback a pantalla completa si no hay ventana
- Guarda en `allure-results/screenshots/`
- Nombre sanitizado (sin caracteres inválidos)
- Timestamp automático

### Usado Automáticamente

`BaseTest` captura screenshots automáticamente cuando un test falla, no necesitas llamarlo manualmente.

## RetryPolicy

Política de reintentos para operaciones transitorias.

### Uso

```csharp
// Acción sin retorno
RetryPolicy.Execute(
    action: () => button.Click(),
    maxRetries: 3,
    delayMs: 1000
);

// Función con retorno
var result = RetryPolicy.Execute(
    func: () => element.GetText(),
    maxRetries: 3,
    delayMs: 1000
);
```

### Errores Transitorios (reintentables)

- `ElementNotAvailableException`
- `TimeoutException`
- `InvalidOperationException` con mensajes específicos

### Errores NO Transitorios (NO reintentables)

- **AssertionException**: Fallos de assert nunca se reintentan
- Otros errores no transitorios

### Ejemplo

```csharp
// Esto reintentará si el elemento no está disponible temporalmente
RetryPolicy.Execute(() =>
{
    var element = FindElement("DynamicButton");
    element.Click();
}, maxRetries: 3);

// Esto NO reintentará (fallo de assert)
RetryPolicy.Execute(() =>
{
    var text = GetResult();
    Assert.That(text, Is.EqualTo("Expected")); // Si falla, lanza inmediatamente
});
```

## Convenciones de Naming

### AutomationIds
- PascalCase
- Descriptivo: `SubmitButton`, `UsernameTextBox`, `ErrorMessage`

### Page Objects
- Sufijo `Page`: `LoginPage`, `MainWindowPage`
- PascalCase

### Tests
- Descriptivo y específico
- Formato: `Verify[Qué]_[Condición]_[Resultado]`
- Ejemplo: `VerifyLogin_WithInvalidCredentials_ShowsError`

### Métodos en Page Objects
- Verbos de acción: `Click`, `Enter`, `Select`, `Get`, `Is`, `Wait`
- PascalCase

## Ejemplo Completo

```csharp
[TestFixture]
[Category("Smoke")]
[AllureSuite("Login Tests")]
public class LoginTests : BaseTest
{
    private LoginPage _loginPage = null!;
    
    [SetUp]
    public void TestSetUp()
    {
        // BaseTest.SetUp ya lanzó la app
        _loginPage = new LoginPage(MainWindow!);
    }
    
    [Test]
    [AllureTag("login", "positive")]
    [AllureDescription("Verifica login exitoso con credenciales válidas")]
    public void VerifyLogin_WithValidCredentials_Success()
    {
        // Arrange
        var username = "testuser";
        var password = "testpass";
        
        // Act
        _loginPage.Login(username, password);
        
        // Wait for dashboard
        WaitHelper.WaitForWindowTitle("Dashboard", 5000);
        
        // Assert
        Assert.That(MainWindow.Title, Does.Contain("Dashboard"));
    }
    
    [Test]
    [AllureTag("login", "negative")]
    public void VerifyLogin_WithInvalidCredentials_ShowsError()
    {
        // Arrange
        var username = "invalid";
        var password = "wrong";
        
        // Act
        _loginPage.Login(username, password);
        
        // Wait for error
        Thread.Sleep(500); // O mejor: WaitForElementVisible("ErrorMessage")
        
        var errorMessage = _loginPage.GetErrorMessage();
        
        // Assert
        Assert.That(errorMessage, Does.Contain("Invalid credentials"));
    }
}
```

## Próximos Pasos

- **[Reporting & Logging](./reporting-logging.md)** - Personaliza reportes
- **[CI/CD](./ci-cd.md)** - Integra con pipelines
- **[Troubleshooting](./troubleshooting.md)** - Soluciona problemas comunes
