using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Serilog;

namespace Hipos.Tests.PageObjects;

/// <summary>
/// Page Object for Windows Calculator.
/// Interacts with numeric buttons, operators and display.
/// </summary>
public class CalculatorPage : BasePage
{
    public CalculatorPage(Window window) : base(window)
    {
        Log.Information("Initializing CalculatorPage for Windows Calculator");
    }

    /// <summary>
    /// Clicks a numeric button (0-9).
    /// </summary>
    public void ClickNumber(int number)
    {
        if (number < 0 || number > 9)
            throw new ArgumentException("Number must be between 0 and 9", nameof(number));

        Log.Information("Clicking on number: {Number}", number);
        
        // Windows Calculator uses names like "Uno", "Dos", etc. in Spanish
        // or "One", "Two", etc. in English
        var numberNames = new Dictionary<int, string[]>
        {
            {0, new[] {"Cero", "Zero"}},
            {1, new[] {"Uno", "One"}},
            {2, new[] {"Dos", "Two"}},
            {3, new[] {"Tres", "Three"}},
            {4, new[] {"Cuatro", "Four"}},
            {5, new[] {"Cinco", "Five"}},
            {6, new[] {"Seis", "Six"}},
            {7, new[] {"Siete", "Seven"}},
            {8, new[] {"Ocho", "Eight"}},
            {9, new[] {"Nueve", "Nine"}}
        };

        var button = FindButtonByNames(numberNames[number]);
        button?.Click();
        Thread.Sleep(100); // Small pause between clicks
    }

    /// <summary>
    /// Clicks the addition button (+).
    /// </summary>
    public void ClickPlus()
    {
        Log.Information("Clicking on Plus (+) button");
        var button = FindButtonByNames(new[] {"Más", "Plus", "Sumar", "Add"});
        button?.Click();
        Thread.Sleep(100);
    }

    /// <summary>
    /// Clicks the subtraction button (-).
    /// </summary>
    public void ClickMinus()
    {
        Log.Information("Clicking on Minus (-) button");
        var button = FindButtonByNames(new[] {"Menos", "Minus", "Restar", "Subtract"});
        button?.Click();
        Thread.Sleep(100);
    }

    /// <summary>
    /// Clicks the multiplication button (*).
    /// </summary>
    public void ClickMultiply()
    {
        Log.Information("Clicking on Multiply (*) button");
        var button = FindButtonByNames(new[] {"Multiplicar por", "Multiply by", "Por", "Times"});
        button?.Click();
        Thread.Sleep(100);
    }

    /// <summary>
    /// Clicks the division button (/).
    /// </summary>
    public void ClickDivide()
    {
        Log.Information("Clicking on Divide (/) button");
        var button = FindButtonByNames(new[] {"Dividir por", "Divide by", "Entre", "Divided by"});
        button?.Click();
        Thread.Sleep(100);
    }

    /// <summary>
    /// Clicks the equals button (=).
    /// </summary>
    public void ClickEquals()
    {
        Log.Information("Clicking on Equals (=) button");
        var button = FindButtonByNames(new[] {"Es igual a", "Equals", "Igual", "="});
        button?.Click();
        Thread.Sleep(200); // Wait for calculation
    }

    /// <summary>
    /// Clicks the Clear button (C).
    /// </summary>
    public void ClickClear()
    {
        Log.Information("Clicking on Clear (C) button");
        var button = FindButtonByNames(new[] {"Borrar", "Clear", "Limpiar", "C"});
        button?.Click();
        Thread.Sleep(100);
    }

    /// <summary>
    /// Gets the current display/result value.
    /// </summary>
    public string GetDisplayValue()
    {
        Log.Information("Getting display value");
        
        // Search for the text element showing the result
        // Can be a TextBlock or Text with AutomationId "CalculatorResults"
        try
        {
            var displayElement = Window.FindFirstDescendant(cf => 
                cf.ByAutomationId("CalculatorResults"));
            
            if (displayElement != null)
            {
                var text = displayElement.AsLabel()?.Text ?? 
                           displayElement.Name ?? 
                           displayElement.Properties.Name.ValueOrDefault;
                
                Log.Information("Display value: {Display}", text);
                return text ?? "";
            }
        }
        catch (Exception ex)
        {
            Log.Warning("Could not get main display: {Error}", ex.Message);
        }

        // Alternative method: search for any Text with the result
        try
        {
            var allTexts = Window.FindAllDescendants(cf => cf.ByControlType(ControlType.Text));
            foreach (var textElement in allTexts)
            {
                var text = textElement.Name;
                if (!string.IsNullOrEmpty(text) && text.Contains("is") && 
                    (char.IsDigit(text[0]) || text.StartsWith("Display")))
                {
                    Log.Information("Display value (alternative): {Display}", text);
                    return text;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning("Could not get alternative display: {Error}", ex.Message);
        }

        return "0";
    }

    /// <summary>
    /// Performs a complete operation: num1 operator num2 = result.
    /// </summary>
    public void PerformOperation(int num1, string operation, int num2)
    {
        Log.Information("Performing operation: {Num1} {Op} {Num2}", num1, operation, num2);
        
        ClickClear(); // Clear first
        
        // Enter first number
        EnterNumber(num1);
        
        // Select operation
        switch (operation)
        {
            case "+": ClickPlus(); break;
            case "-": ClickMinus(); break;
            case "*": ClickMultiply(); break;
            case "/": ClickDivide(); break;
            default: throw new ArgumentException($"Unsupported operation: {operation}");
        }
        
        // Enter second number
        EnterNumber(num2);
        
        // Calculate result
        ClickEquals();
    }

    /// <summary>
    /// Enters a number digit by digit.
    /// </summary>
    private void EnterNumber(int number)
    {
        var digits = number.ToString();
        foreach (var digit in digits)
        {
            ClickNumber(int.Parse(digit.ToString()));
        }
    }

    /// <summary>
    /// Searches for a button by a list of possible names.
    /// </summary>
    private AutomationElement? FindButtonByNames(string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            try
            {
                var button = Window.FindFirstDescendant(cf => 
                    cf.ByName(name).And(cf.ByControlType(ControlType.Button)));
                
                if (button != null && button.IsAvailable)
                {
                    Log.Debug("Button found: {Name}", name);
                    return button;
                }
            }
            catch
            {
                // Continue with next name
            }
        }
        
        Log.Warning("Button not found with names: {Names}", string.Join(", ", possibleNames));
        return null;
    }

    /// <summary>
    /// Verifies if a specific button is available.
    /// </summary>
    public bool IsButtonAvailable(string buttonName)
    {
        try
        {
            var button = Window.FindFirstDescendant(cf => cf.ByName(buttonName));
            return button != null && button.IsAvailable;
        }
        catch
        {
            return false;
        }
    }
}
