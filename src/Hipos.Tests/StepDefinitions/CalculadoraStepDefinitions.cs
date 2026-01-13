using FlaUI.Core.AutomationElements;
using Hipos.Tests.PageObjects;
using TechTalk.SpecFlow;
using NUnit.Framework;
using Serilog;

namespace Hipos.Tests.StepDefinitions;

/// <summary>
/// Step definitions para los escenarios de la Calculadora.
/// </summary>
[Binding]
public class CalculadoraStepDefinitions : BaseStepDefinitions
{
    private CalculatorPage? _calculatorPage;
    private string? _displayValue;
    private string? _intermediateResult;
    private string? _finalResult;
    private List<int>? _missingButtons;

    [Given("que la calculadora está abierta")]
    public void GivenQueLaCalculadoraEstaAbierta()
    {
        LogInfo("Verificando que la calculadora está abierta");
        
        Assert.That(MainWindow, Is.Not.Null, "La ventana de la Calculadora debería estar disponible");
        
        // Asegurar que la ventana esté en primer plano
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(200);
        
        _calculatorPage = new CalculatorPage(MainWindow!);
        LogPass("Calculadora abierta y lista para usar");
    }

    [When("verifico el título de la ventana")]
    public void WhenVerificoElTituloDeLaVentana()
    {
        LogInfo("Verificando el título de la ventana");
    }

    [Then("el título debería contener \"(.*)\" o \"(.*)\"")]
    public void ThenElTituloDeberiaContenerO(string opcion1, string opcion2)
    {
        Assert.That(MainWindow, Is.Not.Null, "La ventana debería existir");
        Assert.That(MainWindow!.Title, Does.Contain(opcion1).Or.Contains(opcion2),
            $"El título debería contener '{opcion1}' o '{opcion2}'");
        
        LogPass($"Título verificado: {MainWindow.Title}");
    }

    [When("verifico la visibilidad de la ventana")]
    public void WhenVerificoLaVisibilidadDeLaVentana()
    {
        LogInfo("Verificando visibilidad de la ventana");
    }

    [Then("la ventana debería estar visible y habilitada")]
    public void ThenLaVentanaDeberiaEstarVisibleYHabilitada()
    {
        Assert.That(MainWindow, Is.Not.Null, "La ventana debería existir");
        Assert.That(MainWindow!.IsOffscreen, Is.False, "La ventana no debería estar fuera de pantalla");
        Assert.That(MainWindow.IsEnabled, Is.True, "La ventana debería estar habilitada");
        
        LogPass("Ventana visible y accesible");
    }

    [When("verifico los elementos de la interfaz")]
    public void WhenVerificoLosElementosDeLaInterfaz()
    {
        LogInfo("Verificando elementos de UI");
    }

    [Then("debería haber elementos de UI disponibles")]
    public void EntoncesDeberiaHaberElementosDeUIDisponibles()
    {
        var children = MainWindow!.FindAllChildren();
        var childCount = children.Length;
        
        LogInfo($"Elementos UI encontrados: {childCount}");
        
        Assert.That(childCount, Is.GreaterThan(0), "La calculadora debería tener elementos de UI");
        Assert.That(MainWindow.IsEnabled, Is.True, "La ventana debería estar habilitada para interacción");
        
        LogPass($"Calculadora tiene {childCount} elementos de UI");
    }

    [When("obtengo la información de la ventana")]
    public void WhenObtengoLaInformacionDeLaVentana()
    {
        LogInfo("Obteniendo información de la ventana");
    }

    [Then("debería mostrar el título, clase, process ID y dimensiones")]
    public void EntoncesDeberiaMostrarElTituloClaseProcessIDYDimensiones()
    {
        var title = MainWindow!.Title;
        var className = MainWindow.ClassName;
        var processId = MainWindow.Properties.ProcessId;
        var bounds = MainWindow.BoundingRectangle;
        
        LogInfo($"Título: {title}");
        LogInfo($"Clase: {className}");
        LogInfo($"Process ID: {processId}");
        LogInfo($"Dimensiones: {bounds.Width}x{bounds.Height}");
        
        LogPass("Información de la Calculadora capturada correctamente");
    }

    [When("limpio la calculadora")]
    public void WhenLimpioLaCalculadora()
    {
        // Asegurar que la ventana esté en primer plano antes de interactuar
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(200);
        
        LogInfo("Limpiando la calculadora");
        _calculatorPage!.ClickClear();
        Thread.Sleep(200);
    }

    [When("realizo la operación \"(.*)\"")]
    public void WhenRealizoLaOperacion(string operacion)
    {
        // Asegurar que la ventana esté en primer plano antes de interactuar
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(300);
        
        // Parsear la operación: "2 + 3", "10 - 4", etc.
        var parts = operacion.Split(' ');
        if (parts.Length != 3)
        {
            throw new ArgumentException($"Formato de operación inválido: {operacion}");
        }
        
        var num1 = int.Parse(parts[0]);
        var operador = parts[1];
        var num2 = int.Parse(parts[2]);
        
        LogInfo($"Realizando operación: {num1} {operador} {num2}");
        _calculatorPage!.PerformOperation(num1, operador, num2);
        
        // Esperar más tiempo para que la calculadora procese y muestre el resultado
        Thread.Sleep(1000);
        
        // Asegurar nuevamente que la ventana esté en primer plano antes de leer el resultado
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(200);
    }

    [Then("el resultado debería ser \"(.*)\"")]
    public void ThenElResultadoDeberiaSer(string resultadoEsperado)
    {
        // Asegurar que la ventana esté en primer plano antes de leer
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(200);
        
        // Intentar leer el display varias veces con esperas
        _displayValue = null;
        for (int i = 0; i < 3; i++)
        {
            _displayValue = _calculatorPage!.GetDisplayValue();
            
            // Extraer número del texto si contiene "La pantalla muestra X"
            if (!string.IsNullOrEmpty(_displayValue))
            {
                var numberMatch = System.Text.RegularExpressions.Regex.Match(_displayValue, @"\d+");
                if (numberMatch.Success)
                {
                    _displayValue = numberMatch.Value;
                }
                
                if (_displayValue.Contains(resultadoEsperado))
                {
                    break;
                }
            }
            Thread.Sleep(300);
        }
        
        LogInfo($"Resultado obtenido: {_displayValue}");
        
        Assert.That(_displayValue, Is.Not.Null, "No se pudo obtener el valor del display");
        Assert.That(_displayValue, Does.Contain(resultadoEsperado),
            $"El resultado debería contener '{resultadoEsperado}', pero se obtuvo: '{_displayValue}'");
        
        LogPass($"Operación correcta: resultado = {resultadoEsperado}");
    }

    [When("ingreso el número \"(.*)\"")]
    public void WhenIngresoElNumero(string numero)
    {
        // Asegurar que la ventana esté en primer plano
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(100);
        
        var num = int.Parse(numero);
        LogInfo($"Ingresando número: {num}");
        _calculatorPage!.ClickNumber(num);
    }

    [When("presiono el botón de suma")]
    public void WhenPresionoElBotonDeSuma()
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(100);
        
        LogInfo("Presionando botón de suma");
        _calculatorPage!.ClickPlus();
    }

    [When("presiono el botón de multiplicación")]
    public void WhenPresionoElBotonDeMultiplicacion()
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(100);
        
        LogInfo("Presionando botón de multiplicación");
        _calculatorPage!.ClickMultiply();
    }

    [When("presiono el botón igual")]
    public void WhenPresionoElBotonIgual()
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(100);
        
        LogInfo("Presionando botón igual");
        _calculatorPage!.ClickEquals();
        Thread.Sleep(800); // Esperar a que se calcule el resultado
    }

    [Then("el resultado intermedio debería ser \"(.*)\"")]
    public void ThenElResultadoIntermedioDeberiaSer(string resultadoEsperado)
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(200);
        
        _intermediateResult = _calculatorPage!.GetDisplayValue();
        
        // Extraer número del texto si es necesario
        if (!string.IsNullOrEmpty(_intermediateResult))
        {
            var numberMatch = System.Text.RegularExpressions.Regex.Match(_intermediateResult, @"\d+");
            if (numberMatch.Success)
            {
                _intermediateResult = numberMatch.Value;
            }
        }
        
        LogInfo($"Resultado intermedio obtenido: {_intermediateResult}");
        
        Assert.That(_intermediateResult, Does.Contain(resultadoEsperado),
            $"El resultado intermedio debería contener '{resultadoEsperado}', pero se obtuvo: '{_intermediateResult}'");
        
        LogPass($"Resultado intermedio correcto: {resultadoEsperado}");
    }

    [Then("el resultado final debería ser \"(.*)\"")]
    public void ThenElResultadoFinalDeberiaSer(string resultadoEsperado)
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(200);
        
        _finalResult = _calculatorPage!.GetDisplayValue();
        
        // Extraer número del texto si es necesario
        if (!string.IsNullOrEmpty(_finalResult))
        {
            var numberMatch = System.Text.RegularExpressions.Regex.Match(_finalResult, @"\d+");
            if (numberMatch.Success)
            {
                _finalResult = numberMatch.Value;
            }
        }
        
        LogInfo($"Resultado final obtenido: {_finalResult}");
        
        Assert.That(_finalResult, Does.Contain(resultadoEsperado),
            $"El resultado final debería contener '{resultadoEsperado}', pero se obtuvo: '{_finalResult}'");
        
        LogPass($"Resultado final correcto: {resultadoEsperado}");
    }

    [When("verifico la disponibilidad de los botones numéricos del (\\d+) al (\\d+)")]
    public void WhenVerificoLaDisponibilidadDeLosBotonesNumericosDelAl(int desde, int hasta)
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(200);
        
        LogInfo($"Verificando disponibilidad de botones numéricos del {desde} al {hasta}");
        _missingButtons = new List<int>();
        
        for (int i = desde; i <= hasta; i++)
        {
            try
            {
                _calculatorPage!.ClickNumber(i);
                Log.Debug("✓ Botón {Number} disponible", i);
            }
            catch (Exception ex)
            {
                LogWarning($"Botón {i} no disponible: {ex.Message}");
                _missingButtons.Add(i);
            }
        }
        
        // Limpiar después de verificar
        _calculatorPage!.ClickClear();
    }

    [Then("todos los botones numéricos deberían estar disponibles")]
    public void EntoncesTodosLosBotonesNumericosDeberianEstarDisponibles()
    {
        Assert.That(_missingButtons, Is.Not.Null, "La verificación de botones debería haberse ejecutado");
        Assert.That(_missingButtons!, Is.Empty,
            $"Los siguientes botones no están disponibles: {string.Join(", ", _missingButtons)}");
        
        LogPass("Todos los botones numéricos (0-9) están disponibles");
    }

    [When("ingreso los números \"(.*)\"")]
    public void WhenIngresoLosNumeros(string numeros)
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(100);
        
        LogInfo($"Ingresando números: {numeros}");
        foreach (var digit in numeros)
        {
            if (char.IsDigit(digit))
            {
                _calculatorPage!.ClickNumber(int.Parse(digit.ToString()));
            }
        }
        Thread.Sleep(300);
    }

    [When("verifico el valor del display")]
    public void WhenVerificoElValorDelDisplay()
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(100);
        
        _displayValue = _calculatorPage!.GetDisplayValue();
        
        // Extraer número del texto si es necesario
        if (!string.IsNullOrEmpty(_displayValue))
        {
            var numberMatch = System.Text.RegularExpressions.Regex.Match(_displayValue, @"\d+");
            if (numberMatch.Success)
            {
                _displayValue = numberMatch.Value;
            }
        }
        
        LogInfo($"Valor del display: {_displayValue}");
    }

    [Then("el display debería contener \"(.*)\"")]
    public void ThenElDisplayDeberiaContener(string valorEsperado)
    {
        Assert.That(_displayValue, Is.Not.Null, "El valor del display debería haberse obtenido");
        Assert.That(_displayValue, Does.Contain(valorEsperado),
            $"El display debería contener '{valorEsperado}', pero muestra: '{_displayValue}'");
        
        LogPass($"Display contiene '{valorEsperado}'");
    }

    [When("presiono el botón Clear")]
    public void WhenPresionoElBotonClear()
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(100);
        
        LogInfo("Presionando botón Clear");
        _calculatorPage!.ClickClear();
        Thread.Sleep(300);
    }

    [Then("el display debería mostrar \"(.*)\"")]
    public void ThenElDisplayDeberiaMostrar(string valorEsperado)
    {
        Assert.That(_displayValue, Is.Not.Null, "El valor del display debería haberse obtenido");
        Assert.That(_displayValue, Does.Contain(valorEsperado),
            $"El display debería mostrar '{valorEsperado}', pero muestra: '{_displayValue}'");
        
        LogPass($"Display muestra '{valorEsperado}'");
    }
}
