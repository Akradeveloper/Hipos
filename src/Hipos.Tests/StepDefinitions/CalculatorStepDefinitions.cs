using FlaUI.Core.AutomationElements;
using Hipos.Tests.PageObjects;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace Hipos.Tests.StepDefinitions;

[Binding]
public class CalculatorStepDefinitions : BaseStepDefinitions
{
    private CalculatorPage? _calculatorPage;

    [Given("la Calculadora está abierta")]
    public void GivenLaCalculadoraEstaAbierta()
    {
        LogInfo("Comprobando que la Calculadora esté abierta");
        Assert.That(MainWindow, Is.Not.Null, "La ventana principal debe estar disponible");

        var title = MainWindow!.Title ?? string.Empty;
        Assert.That(
            title.Contains("Calculadora", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("Calculator", StringComparison.OrdinalIgnoreCase),
            Is.True,
            $"La ventana debe ser la Calculadora (título: '{title}')");

        MainWindow.Focus();
        _calculatorPage = new CalculatorPage(MainWindow);
        LogPass("Calculadora lista para usar");
    }

    [When("pulso \"(.*)\"")]
    public void WhenPulso(string key)
    {
        LogInfo($"Pulsando: {key}");
        _calculatorPage!.ClickButton(key);
        LogPass($"Pulsado: {key}");
    }

    [Then("el resultado es \"(.*)\"")]
    public void ThenElResultadoEs(string expected)
    {
        LogInfo($"Comprobando resultado esperado: {expected}");
        var displayText = _calculatorPage!.GetDisplayText();

        Assert.That(
            displayText,
            Is.EqualTo(expected),
            $"El display debería mostrar '{expected}', pero muestra '{displayText}'");

        LogPass($"Resultado correcto: {displayText}");
    }
}
