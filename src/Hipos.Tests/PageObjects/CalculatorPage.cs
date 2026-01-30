using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;
using Hipos.Framework.Utils;
using Serilog;

namespace Hipos.Tests.PageObjects;

/// <summary>
/// Page Object para la Calculadora de Windows (UWP).
/// Usa FlaUI/UIA directamente (no MSAA). AutomationIds alineados con el código fuente oficial
/// (microsoft/calculator): NumberPad.xaml y CalculatorStandardOperators.xaml.
/// </summary>
public class CalculatorPage
{
    private readonly Window _window;
    private readonly ConditionFactory _conditionFactory;
    private const string DisplayAutomationId = "CalculatorResults";
    private const string DisplayAlwaysOnTopAutomationId = "CalculatorAlwaysOnTopResults";

    private static readonly Dictionary<string, string> ButtonAutomationIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["0"] = "num0Button", ["1"] = "num1Button", ["2"] = "num2Button", ["3"] = "num3Button", ["4"] = "num4Button",
        ["5"] = "num5Button", ["6"] = "num6Button", ["7"] = "num7Button", ["8"] = "num8Button", ["9"] = "num9Button",
        ["+"] = "plusButton", ["-"] = "minusButton", ["*"] = "multiplyButton", ["/"] = "divideButton",
        ["="] = "equalButton", ["."] = "decimalSeparatorButton", [","] = "decimalSeparatorButton",
        ["C"] = "clearButton", ["CE"] = "clearEntryButton", ["±"] = "negateButton"
    };

    public CalculatorPage(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _conditionFactory = new ConditionFactory(new UIA3PropertyLibrary());
    }

    /// <summary>
    /// Hace clic en un botón por su etiqueta (número, operador o símbolo).
    /// </summary>
    public void ClickButton(string key)
    {
        if (!ButtonAutomationIds.TryGetValue(key.Trim(), out var automationId))
        {
            automationId = key.Trim();
        }

        var button = WaitForElement(automationId);
        if (button == null)
        {
            throw new InvalidOperationException($"No se encontró el botón con AutomationId '{automationId}' (key: '{key}').");
        }

        var invokable = button.AsButton();
        invokable?.Invoke();
        Log.Debug("Pulsado botón: {Key} (AutomationId: {AutomationId})", key, automationId);
    }

    /// <summary>
    /// Obtiene el texto mostrado en la pantalla de resultados.
    /// La Calculadora UWP expone el valor en el Name del elemento (p. ej. "Display is 8" en inglés, "La pantalla muestra 8" en español).
    /// Intenta CalculatorResults primero; si no existe (p. ej. modo siempre visible), usa CalculatorAlwaysOnTopResults.
    /// </summary>
    public string GetDisplayText()
    {
        var display = _window.FindFirstDescendant(_conditionFactory.ByAutomationId(DisplayAutomationId))
            ?? _window.FindFirstDescendant(_conditionFactory.ByAutomationId(DisplayAlwaysOnTopAutomationId));
        if (display == null)
        {
            throw new InvalidOperationException(
                $"No se encontró el display (AutomationId: {DisplayAutomationId} ni {DisplayAlwaysOnTopAutomationId}).");
        }

        var name = display.Name ?? string.Empty;
        // Prefijos conocidos del display según idioma (inglés, español, etc.)
        var displayPrefixes = new[] { "Display is ", "La pantalla muestra " };
        foreach (var prefix in displayPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(prefix.Length).Trim();
            }
        }

        return name.Trim();
    }

    /// <summary>
    /// Espera a que un elemento esté disponible (con reintentos).
    /// </summary>
    private AutomationElement? WaitForElement(string automationId, int timeoutMs = 5000)
    {
        AutomationElement? foundElement = null;
        var found = WaitHelper.WaitUntilAdaptive(
            () =>
            {
                foundElement = _window.FindFirstDescendant(_conditionFactory.ByAutomationId(automationId));
                return foundElement != null && !foundElement.IsOffscreen;
            },
            timeoutMs,
            conditionDescription: $"elemento AutomationId '{automationId}'");

        return found ? foundElement : null;
    }
}
