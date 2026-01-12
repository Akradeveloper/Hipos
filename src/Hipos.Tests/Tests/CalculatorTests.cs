using NUnit.Framework;
using Hipos.Framework.Core;
using Hipos.Framework.Utils;
using Hipos.Tests.PageObjects;
using Serilog;

namespace Hipos.Tests.Tests;

/// <summary>
/// Tests demo para la Calculadora de Windows.
/// Estos tests demuestran el uso del framework con una aplicación real de Windows.
/// </summary>
[TestFixture]
[Category("Demo")]
[Description("Calculator Demo Tests")]
public class CalculatorTests : BaseTest
{
    private CalculatorPage? _calculatorPage;

    [SetUp]
    public void TestSetup()
    {
        _calculatorPage = new CalculatorPage(MainWindow!);
        ExtentReportManager.LogInfo($"Iniciando test: {TestContext.CurrentContext.Test.Name}");
    }

    [TearDown]
    public void TestTearDown()
    {
        var outcome = TestContext.CurrentContext.Result.Outcome.Status;
        var testName = TestContext.CurrentContext.Test.Name;
        
        if (outcome == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            ExtentReportManager.LogFail($"Test fallido: {testName}");
        }
        else if (outcome == NUnit.Framework.Interfaces.TestStatus.Passed)
        {
            ExtentReportManager.LogPass($"Test exitoso: {testName}");
        }
    }

    [Test]
    [Category("Smoke")]
    [Description("Verifica que la Calculadora se abra correctamente")]
    public void VerifyCalculatorOpens()
    {
        Log.Information("Verificando que la Calculadora está abierta");
        ExtentReportManager.LogInfo("Verificando que la Calculadora está abierta");
        
        // Assert
        Assert.That(MainWindow, Is.Not.Null, "La ventana de la Calculadora debería estar disponible");
        Assert.That(MainWindow!.Title, Does.Contain("Calculadora").Or.Contains("Calculator"), 
            "El título debería contener 'Calculadora' o 'Calculator'");
        
        Log.Information("✓ Calculadora abierta exitosamente - Título: {Title}", MainWindow.Title);
        ExtentReportManager.LogPass($"Calculadora abierta - Título: {MainWindow.Title}");
    }

    [Test]
    [Description("Verifica que la ventana de la Calculadora sea visible y accesible")]
    public void VerifyCalculatorWindowVisible()
    {
        Log.Information("Verificando visibilidad de la ventana de la Calculadora");
        ExtentReportManager.LogInfo("Verificando visibilidad de la ventana");
        
        // Verificar que la ventana esté accesible
        Assert.That(MainWindow, Is.Not.Null, "La ventana debería existir");
        Assert.That(MainWindow!.IsOffscreen, Is.False, "La ventana no debería estar fuera de pantalla");
        Assert.That(MainWindow.IsEnabled, Is.True, "La ventana debería estar habilitada");
        
        Log.Information("✓ Ventana de Calculadora visible y accesible");
        ExtentReportManager.LogPass("Ventana visible y accesible");
    }

    [Test]
    [Description("Verifica que la interfaz de la Calculadora tenga elementos interactivos")]
    public void VerifyCalculatorUIElements()
    {
        Log.Information("Verificando elementos de UI en la Calculadora");
        ExtentReportManager.LogInfo("Verificando elementos de UI");
        
        // Verificar que la ventana tiene elementos hijos (botones, display, etc.)
        var children = MainWindow!.FindAllChildren();
        var childCount = children.Length;
        
        Log.Information("Elementos UI encontrados: {Count}", childCount);
        ExtentReportManager.LogInfo($"Elementos UI encontrados: {childCount}");
        
        Assert.That(childCount, Is.GreaterThan(0), "La calculadora debería tener elementos de UI");
        Assert.That(MainWindow.IsEnabled, Is.True, "La ventana debería estar habilitada para interacción");
        
        Log.Information("✓ Calculadora tiene {Count} elementos de UI", childCount);
        ExtentReportManager.LogPass($"Calculadora tiene {childCount} elementos de UI");
    }

    [Test]
    [Description("Muestra información sobre la ventana de la Calculadora")]
    public void DisplayCalculatorInfo()
    {
        Log.Information("Obteniendo información de la ventana de la Calculadora");
        ExtentReportManager.LogInfo("Obteniendo información de la ventana");
        
        if (MainWindow != null)
        {
            var title = MainWindow.Title;
            var className = MainWindow.ClassName;
            var processId = MainWindow.Properties.ProcessId;
            var isEnabled = MainWindow.IsEnabled;
            var bounds = MainWindow.BoundingRectangle;
            
            Log.Information("🧮 Título: {Title}", title);
            Log.Information("🏷️ Clase: {ClassName}", className);
            Log.Information("🔢 Process ID: {ProcessId}", processId);
            Log.Information("✓ Habilitado: {IsEnabled}", isEnabled);
            Log.Information("📐 Posición: X={X}, Y={Y}, Width={Width}, Height={Height}", 
                bounds.X, bounds.Y, bounds.Width, bounds.Height);
            
            ExtentReportManager.LogInfo($"Título: {title}");
            ExtentReportManager.LogInfo($"Clase: {className}");
            ExtentReportManager.LogInfo($"Process ID: {processId}");
            ExtentReportManager.LogInfo($"Dimensiones: {bounds.Width}x{bounds.Height}");
            
            TestContext.Out.WriteLine($"Calculadora - {title}");
            TestContext.Out.WriteLine($"Clase: {className}");
            TestContext.Out.WriteLine($"Process ID: {processId}");
            TestContext.Out.WriteLine($"Dimensiones: {bounds.Width}x{bounds.Height}");
            
            Assert.Pass($"Información de la Calculadora capturada correctamente");
        }
    }

    // ============================================================
    // TESTS COMPLEJOS - Interacciones reales con la Calculadora
    // ============================================================

    [Test]
    [Category("Complex")]
    [Description("Realiza una suma simple: 2 + 3 = 5")]
    public void PerformSimpleAddition()
    {
        Log.Information("Test: Suma simple 2 + 3");
        ExtentReportManager.LogInfo("Realizando suma: 2 + 3");
        
        // Arrange
        _calculatorPage!.ClickClear();
        
        // Act
        _calculatorPage.PerformOperation(2, "+", 3);
        Thread.Sleep(500);
        
        var display = _calculatorPage.GetDisplayValue();
        Log.Information("Resultado obtenido: {Display}", display);
        ExtentReportManager.LogInfo($"Resultado: {display}");
        
        // Assert
        Assert.That(display, Does.Contain("5"), 
            $"El resultado debería contener '5', pero se obtuvo: '{display}'");
        
        Log.Information("✓ Suma correcta: 2 + 3 = 5");
        ExtentReportManager.LogPass("Suma correcta: 2 + 3 = 5");
    }

    [Test]
    [Category("Complex")]
    [Description("Realiza una resta: 10 - 4 = 6")]
    public void PerformSubtraction()
    {
        Log.Information("Test: Resta 10 - 4");
        ExtentReportManager.LogInfo("Realizando resta: 10 - 4");
        
        // Arrange
        _calculatorPage!.ClickClear();
        
        // Act
        _calculatorPage.PerformOperation(10, "-", 4);
        Thread.Sleep(500);
        
        var display = _calculatorPage.GetDisplayValue();
        Log.Information("Resultado obtenido: {Display}", display);
        ExtentReportManager.LogInfo($"Resultado: {display}");
        
        // Assert
        Assert.That(display, Does.Contain("6"), 
            $"El resultado debería contener '6', pero se obtuvo: '{display}'");
        
        Log.Information("✓ Resta correcta: 10 - 4 = 6");
        ExtentReportManager.LogPass("Resta correcta: 10 - 4 = 6");
    }

    [Test]
    [Category("Complex")]
    [Description("Realiza una multiplicación: 7 * 8 = 56")]
    public void PerformMultiplication()
    {
        Log.Information("Test: Multiplicación 7 * 8");
        ExtentReportManager.LogInfo("Realizando multiplicación: 7 * 8");
        
        // Arrange
        _calculatorPage!.ClickClear();
        
        // Act
        _calculatorPage.PerformOperation(7, "*", 8);
        Thread.Sleep(500);
        
        var display = _calculatorPage.GetDisplayValue();
        Log.Information("Resultado obtenido: {Display}", display);
        ExtentReportManager.LogInfo($"Resultado: {display}");
        
        // Assert
        Assert.That(display, Does.Contain("56"), 
            $"El resultado debería contener '56', pero se obtuvo: '{display}'");
        
        Log.Information("✓ Multiplicación correcta: 7 * 8 = 56");
        ExtentReportManager.LogPass("Multiplicación correcta: 7 * 8 = 56");
    }

    [Test]
    [Category("Complex")]
    [Description("Realiza una división: 20 / 4 = 5")]
    public void PerformDivision()
    {
        Log.Information("Test: División 20 / 4");
        ExtentReportManager.LogInfo("Realizando división: 20 / 4");
        
        // Arrange
        _calculatorPage!.ClickClear();
        
        // Act
        _calculatorPage.PerformOperation(20, "/", 4);
        Thread.Sleep(500);
        
        var display = _calculatorPage.GetDisplayValue();
        Log.Information("Resultado obtenido: {Display}", display);
        ExtentReportManager.LogInfo($"Resultado: {display}");
        
        // Assert
        Assert.That(display, Does.Contain("5"), 
            $"El resultado debería contener '5', pero se obtuvo: '{display}'");
        
        Log.Information("✓ División correcta: 20 / 4 = 5");
        ExtentReportManager.LogPass("División correcta: 20 / 4 = 5");
    }

    [Test]
    [Category("Complex")]
    [Description("Realiza operaciones secuenciales: (5 + 3) * 2")]
    public void PerformSequentialOperations()
    {
        Log.Information("Test: Operaciones secuenciales (5 + 3) * 2");
        ExtentReportManager.LogInfo("Realizando operaciones secuenciales: (5 + 3) * 2");
        
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
        ExtentReportManager.LogInfo($"Resultado intermedio: {intermediateResult}");
        
        // Segunda operación: * 2 = 16
        _calculatorPage.ClickMultiply();
        _calculatorPage.ClickNumber(2);
        _calculatorPage.ClickEquals();
        Thread.Sleep(500);
        
        var finalResult = _calculatorPage.GetDisplayValue();
        Log.Information("Resultado final (* 2): {Result}", finalResult);
        ExtentReportManager.LogInfo($"Resultado final: {finalResult}");
        
        // Assert
        Assert.That(intermediateResult, Does.Contain("8"), "El resultado intermedio debería ser 8");
        Assert.That(finalResult, Does.Contain("16"), "El resultado final debería ser 16");
        
        Log.Information("✓ Operaciones secuenciales correctas: (5 + 3) * 2 = 16");
        ExtentReportManager.LogPass("Operaciones secuenciales correctas: (5 + 3) * 2 = 16");
    }

    [Test]
    [Category("Complex")]
    [Description("Verifica que todos los botones numéricos (0-9) están disponibles")]
    public void VerifyAllNumericButtonsAvailable()
    {
        Log.Information("Test: Verificando disponibilidad de botones numéricos 0-9");
        ExtentReportManager.LogInfo("Verificando disponibilidad de botones 0-9");
        
        var missingButtons = new List<int>();
        
        for (int i = 0; i <= 9; i++)
        {
            try
            {
                _calculatorPage!.ClickNumber(i);
                Log.Debug("✓ Botón {Number} disponible", i);
            }
            catch (Exception ex)
            {
                Log.Warning("✗ Botón {Number} no disponible: {Error}", i, ex.Message);
                ExtentReportManager.LogWarning($"Botón {i} no disponible: {ex.Message}");
                missingButtons.Add(i);
            }
        }
        
        // Limpiar
        _calculatorPage!.ClickClear();
        
        // Assert
        Assert.That(missingButtons, Is.Empty, 
            $"Los siguientes botones no están disponibles: {string.Join(", ", missingButtons)}");
        
        Log.Information("✓ Todos los botones numéricos (0-9) están disponibles");
        ExtentReportManager.LogPass("Todos los botones numéricos (0-9) están disponibles");
    }

    [Test]
    [Category("Complex")]
    [Description("Verifica que el botón Clear (C) limpia correctamente el display")]
    public void VerifyClearButtonFunctionality()
    {
        Log.Information("Test: Funcionalidad del botón Clear");
        ExtentReportManager.LogInfo("Verificando funcionalidad del botón Clear");
        
        // Arrange - Ingresar algunos números
        _calculatorPage!.ClickNumber(1);
        _calculatorPage.ClickNumber(2);
        _calculatorPage.ClickNumber(3);
        Thread.Sleep(300);
        
        var beforeClear = _calculatorPage.GetDisplayValue();
        Log.Information("Display antes de Clear: {Display}", beforeClear);
        ExtentReportManager.LogInfo($"Display antes de Clear: {beforeClear}");
        
        // Act - Presionar Clear
        _calculatorPage.ClickClear();
        Thread.Sleep(300);
        
        var afterClear = _calculatorPage.GetDisplayValue();
        Log.Information("Display después de Clear: {Display}", afterClear);
        ExtentReportManager.LogInfo($"Display después de Clear: {afterClear}");
        
        // Assert
        Assert.That(beforeClear, Does.Contain("123").Or.Contains("1"), 
            "Debería haber números antes de Clear");
        Assert.That(afterClear, Does.Contain("0"), 
            "El display debería mostrar '0' después de Clear");
        
        Log.Information("✓ Botón Clear funciona correctamente");
        ExtentReportManager.LogPass("Botón Clear funciona correctamente");
    }
}
