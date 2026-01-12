---
sidebar_position: 5
---

# Ejemplos de Tests

Ejemplos reales de tests automatizados contra la Calculadora de Windows.

## Test Básico - Verificación de Apertura

```csharp
[Test]
[Category("Demo")]
[AllureTag("calculator", "smoke")]
[AllureSeverity(SeverityLevel.critical)]
public void VerifyCalculatorOpens()
{
    Log.Information("Verificando que la Calculadora está abierta");
    
    // Assert
    Assert.That(MainWindow, Is.Not.Null, 
        "La ventana de la Calculadora debería estar disponible");
    Assert.That(MainWindow!.Title, 
        Does.Contain("Calculadora").Or.Contains("Calculator"), 
        "El título debería contener 'Calculadora' o 'Calculator'");
    
    Log.Information("✓ Calculadora abierta exitosamente - Título: {Title}", 
        MainWindow.Title);
}
```

## Test Complejo - Suma Simple

```csharp
[Test]
[Category("Complex")]
[AllureTag("calculator", "operation", "addition")]
[AllureSeverity(SeverityLevel.critical)]
[AllureDescription("Realiza una suma simple: 2 + 3 = 5")]
public void PerformSimpleAddition()
{
    Log.Information("Test: Suma simple 2 + 3");
    
    // Arrange
    _calculatorPage!.ClickClear();
    
    // Act
    _calculatorPage.PerformOperation(2, "+", 3);
    Thread.Sleep(500);
    
    var display = _calculatorPage.GetDisplayValue();
    Log.Information("Resultado obtenido: {Display}", display);
    
    // Assert
    Assert.That(display, Does.Contain("5"), 
        $"El resultado debería contener '5', pero se obtuvo: '{display}'");
    
    Log.Information("✓ Suma correcta: 2 + 3 = 5");
}
```

## Test Avanzado - Operaciones Secuenciales

```csharp
[Test]
[Category("Complex")]
[AllureTag("calculator", "operation", "sequential")]
[AllureSeverity(SeverityLevel.critical)]
[AllureDescription("Realiza operaciones secuenciales: (5 + 3) * 2")]
public void PerformSequentialOperations()
{
    Log.Information("Test: Operaciones secuenciales (5 + 3) * 2");
    
    // Arrange
    _calculatorPage!.ClickClear();
    
    // Act - Primera operación: 5 + 3 = 8
    _calculatorPage.ClickNumber(5);
    _calculatorPage.ClickPlus();
    _calculatorPage.ClickNumber(3);
    _calculatorPage.ClickEquals();
    Thread.Sleep(500);
    
    var intermediateResult = _calculatorPage.GetDisplayValue();
    Log.Information("Resultado intermedio (5 + 3): {Result}", intermediateResult);
    
    // Segunda operación: * 2 = 16
    _calculatorPage.ClickMultiply();
    _calculatorPage.ClickNumber(2);
    _calculatorPage.ClickEquals();
    Thread.Sleep(500);
    
    var finalResult = _calculatorPage.GetDisplayValue();
    Log.Information("Resultado final (* 2): {Result}", finalResult);
    
    // Assert
    Assert.That(intermediateResult, Does.Contain("8"), 
        "El resultado intermedio debería ser 8");
    Assert.That(finalResult, Does.Contain("16"), 
        "El resultado final debería ser 16");
    
    Log.Information("✓ Operaciones secuenciales correctas: (5 + 3) * 2 = 16");
}
```

## Page Object - CalculatorPage

```csharp
public class CalculatorPage : BasePage
{
    public CalculatorPage(Window window) : base(window)
    {
        Log.Information("Inicializando CalculatorPage para Calculadora de Windows");
    }

    /// <summary>
    /// Hace click en un botón numérico (0-9).
    /// </summary>
    public void ClickNumber(int number)
    {
        if (number < 0 || number > 9)
            throw new ArgumentException("El número debe estar entre 0 y 9");

        Log.Information("Haciendo click en número: {Number}", number);
        
        // Nombres en español e inglés
        var numberNames = new Dictionary<int, string[]>
        {
            {0, new[] {"Cero", "Zero"}},
            {1, new[] {"Uno", "One"}},
            {2, new[] {"Dos", "Two"}},
            // ... resto de números
        };

        var button = FindButtonByNames(numberNames[number]);
        button?.Click();
        Thread.Sleep(100);
    }

    /// <summary>
    /// Realiza una operación completa: num1 operador num2 = resultado.
    /// </summary>
    public void PerformOperation(int num1, string operation, int num2)
    {
        Log.Information("Realizando operación: {Num1} {Op} {Num2}", 
            num1, operation, num2);
        
        ClickClear();
        EnterNumber(num1);
        
        switch (operation)
        {
            case "+": ClickPlus(); break;
            case "-": ClickMinus(); break;
            case "*": ClickMultiply(); break;
            case "/": ClickDivide(); break;
        }
        
        EnterNumber(num2);
        ClickEquals();
    }
}
```

## Patrón Arrange-Act-Assert

```csharp
[Test]
public void ExampleTest()
{
    // Arrange (Preparar)
    // - Configurar estado inicial
    // - Crear Page Objects
    // - Preparar datos de prueba
    _calculatorPage.ClickClear();
    var expectedResult = "10";
    
    // Act (Actuar)
    // - Ejecutar la acción a probar
    // - Interactuar con la UI
    _calculatorPage.PerformOperation(5, "+", 5);
    var actualResult = _calculatorPage.GetDisplayValue();
    
    // Assert (Afirmar)
    // - Verificar resultado esperado
    // - Comparar valores
    Assert.That(actualResult, Does.Contain(expectedResult));
}
```

## Configuración para Diferentes Apps

### Calculadora de Windows

```json
{
  "AppPath": "calc.exe",
  "DefaultTimeout": 15000
}
```

### Notepad

```json
{
  "AppPath": "notepad.exe",
  "DefaultTimeout": 5000
}
```

### Aplicación Personalizada

```json
{
  "AppPath": "C:\\MiProyecto\\bin\\Debug\\MiApp.exe",
  "DefaultTimeout": 10000
}
```

## Ejecución de Tests

```bash
# Todos los tests
dotnet test

# Solo tests básicos
dotnet test --filter "Category=Demo"

# Solo tests complejos
dotnet test --filter "Category=Complex"

# Test específico
dotnet test --filter "FullyQualifiedName~PerformSimpleAddition"

# Con logging detallado
dotnet test --logger "console;verbosity=detailed"
```

## Ver Resultados

```bash
# Generar reporte Allure
allure serve src\Hipos.Tests\bin\Debug\net8.0-windows\allure-results

# Ver logs
cat src\Hipos.Tests\bin\Debug\net8.0-windows\logs\test-*.log

# Ver screenshots
ls src\Hipos.Tests\bin\Debug\net8.0-windows\allure-results\screenshots\
```

## Métricas del Proyecto

**Estado Actual:**
- ✅ 11 tests implementados
- ✅ 100% tasa de éxito
- ✅ ~16-25s tiempo de ejecución
- ✅ 4 tests básicos (verificación)
- ✅ 7 tests complejos (operaciones)

**Operaciones Matemáticas Soportadas:**
- Suma: `2 + 3 = 5`
- Resta: `10 - 4 = 6`
- Multiplicación: `7 * 8 = 56`
- División: `20 / 4 = 5`
- Secuenciales: `(5 + 3) * 2 = 16`

## Próximos Pasos

1. Añadir tests con decimales
2. Implementar tests de funciones científicas
3. Validar operaciones con memoria (M+, M-, MR)
4. Tests de validación (división por cero)
5. Tests de performance

Para más información, consulta las otras secciones de la documentación.
