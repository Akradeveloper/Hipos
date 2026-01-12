using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Serilog;

namespace Hipos.Tests.PageObjects;

/// <summary>
/// Page Object para la Calculadora de Windows.
/// Interactúa con botones numéricos, operadores y display.
/// </summary>
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
            throw new ArgumentException("El número debe estar entre 0 y 9", nameof(number));

        Log.Information("Haciendo click en número: {Number}", number);
        
        // La calculadora de Windows usa nombres como "Uno", "Dos", etc. en español
        // o "One", "Two", etc. en inglés
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
        Thread.Sleep(100); // Pequeña pausa entre clicks
    }

    /// <summary>
    /// Hace click en el botón de suma (+).
    /// </summary>
    public void ClickPlus()
    {
        Log.Information("Haciendo click en botón Más (+)");
        var button = FindButtonByNames(new[] {"Más", "Plus", "Sumar", "Add"});
        button?.Click();
        Thread.Sleep(100);
    }

    /// <summary>
    /// Hace click en el botón de resta (-).
    /// </summary>
    public void ClickMinus()
    {
        Log.Information("Haciendo click en botón Menos (-)");
        var button = FindButtonByNames(new[] {"Menos", "Minus", "Restar", "Subtract"});
        button?.Click();
        Thread.Sleep(100);
    }

    /// <summary>
    /// Hace click en el botón de multiplicación (*).
    /// </summary>
    public void ClickMultiply()
    {
        Log.Information("Haciendo click en botón Multiplicar (*)");
        var button = FindButtonByNames(new[] {"Multiplicar por", "Multiply by", "Por", "Times"});
        button?.Click();
        Thread.Sleep(100);
    }

    /// <summary>
    /// Hace click en el botón de división (/).
    /// </summary>
    public void ClickDivide()
    {
        Log.Information("Haciendo click en botón Dividir (/)");
        var button = FindButtonByNames(new[] {"Dividir por", "Divide by", "Entre", "Divided by"});
        button?.Click();
        Thread.Sleep(100);
    }

    /// <summary>
    /// Hace click en el botón igual (=).
    /// </summary>
    public void ClickEquals()
    {
        Log.Information("Haciendo click en botón Igual (=)");
        var button = FindButtonByNames(new[] {"Es igual a", "Equals", "Igual", "="});
        button?.Click();
        Thread.Sleep(200); // Esperar a que se calcule el resultado
    }

    /// <summary>
    /// Hace click en el botón Clear (C).
    /// </summary>
    public void ClickClear()
    {
        Log.Information("Haciendo click en botón Borrar (C)");
        var button = FindButtonByNames(new[] {"Borrar", "Clear", "Limpiar", "C"});
        button?.Click();
        Thread.Sleep(100);
    }

    /// <summary>
    /// Obtiene el valor actual del display/resultado.
    /// </summary>
    public string GetDisplayValue()
    {
        Log.Information("Obteniendo valor del display");
        
        // Buscar el elemento de texto que muestra el resultado
        // Puede ser un TextBlock o un Text con AutomationId "CalculatorResults"
        try
        {
            var displayElement = Window.FindFirstDescendant(cf => 
                cf.ByAutomationId("CalculatorResults"));
            
            if (displayElement != null)
            {
                var text = displayElement.AsLabel()?.Text ?? 
                           displayElement.Name ?? 
                           displayElement.Properties.Name.ValueOrDefault;
                
                Log.Information("Valor del display: {Display}", text);
                return text ?? "";
            }
        }
        catch (Exception ex)
        {
            Log.Warning("No se pudo obtener el display principal: {Error}", ex.Message);
        }

        // Método alternativo: buscar cualquier Text con el resultado
        try
        {
            var allTexts = Window.FindAllDescendants(cf => cf.ByControlType(ControlType.Text));
            foreach (var textElement in allTexts)
            {
                var text = textElement.Name;
                if (!string.IsNullOrEmpty(text) && text.Contains("is") && 
                    (char.IsDigit(text[0]) || text.StartsWith("Display")))
                {
                    Log.Information("Valor del display (alternativo): {Display}", text);
                    return text;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning("No se pudo obtener el display alternativo: {Error}", ex.Message);
        }

        return "0";
    }

    /// <summary>
    /// Realiza una operación completa: num1 operador num2 = resultado.
    /// </summary>
    public void PerformOperation(int num1, string operation, int num2)
    {
        Log.Information("Realizando operación: {Num1} {Op} {Num2}", num1, operation, num2);
        
        ClickClear(); // Limpiar primero
        
        // Ingresar primer número
        EnterNumber(num1);
        
        // Seleccionar operación
        switch (operation)
        {
            case "+": ClickPlus(); break;
            case "-": ClickMinus(); break;
            case "*": ClickMultiply(); break;
            case "/": ClickDivide(); break;
            default: throw new ArgumentException($"Operación no soportada: {operation}");
        }
        
        // Ingresar segundo número
        EnterNumber(num2);
        
        // Calcular resultado
        ClickEquals();
    }

    /// <summary>
    /// Ingresa un número digit por dígito.
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
    /// Busca un botón por una lista de posibles nombres.
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
                    Log.Debug("Botón encontrado: {Name}", name);
                    return button;
                }
            }
            catch
            {
                // Continuar con el siguiente nombre
            }
        }
        
        Log.Warning("No se encontró botón con nombres: {Names}", string.Join(", ", possibleNames));
        return null;
    }

    /// <summary>
    /// Verifica si un botón específico está disponible.
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
