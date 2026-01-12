using FlaUI.Core.AutomationElements;
using Serilog;

namespace Hipos.Tests.PageObjects;

/// <summary>
/// Page Object para la ventana principal de la aplicación demo.
/// Maneja la sección de entrada de texto.
/// </summary>
public class MainWindowPage : BasePage
{
    // AutomationIds de los elementos
    private const string InputTextBoxId = "InputTextBox";
    private const string SubmitButtonId = "SubmitButton";
    private const string ResultLabelId = "ResultLabel";

    public MainWindowPage(Window window) : base(window)
    {
        Log.Information("Navegando a MainWindowPage");
    }

    /// <summary>
    /// Ingresa texto en el campo de entrada.
    /// </summary>
    /// <param name="text">Texto a ingresar</param>
    public void EnterText(string text)
    {
        Log.Information("Ingresando texto en InputTextBox: {Text}", text);
        
        var inputBox = FindElement(InputTextBoxId);
        inputBox.SetText(text);
    }

    /// <summary>
    /// Hace click en el botón Submit.
    /// </summary>
    public void ClickSubmit()
    {
        Log.Information("Haciendo click en SubmitButton");
        
        var submitButton = FindElement(SubmitButtonId);
        submitButton.Click();
    }

    /// <summary>
    /// Obtiene el texto del label de resultado.
    /// </summary>
    /// <returns>Texto del resultado</returns>
    public string GetResult()
    {
        Log.Information("Obteniendo texto de ResultLabel");
        
        var resultLabel = FindElement(ResultLabelId);
        var text = resultLabel.GetText();
        
        Log.Information("Resultado obtenido: '{Text}'", text);
        return text;
    }

    /// <summary>
    /// Verifica si el botón Submit está habilitado.
    /// </summary>
    /// <returns>True si está habilitado, false en caso contrario</returns>
    public bool IsSubmitButtonEnabled()
    {
        Log.Debug("Verificando si SubmitButton está habilitado");
        
        var submitButton = FindElement(SubmitButtonId);
        return submitButton.IsEnabled();
    }

    /// <summary>
    /// Obtiene el texto actual del input.
    /// </summary>
    /// <returns>Texto del input</returns>
    public string GetInputText()
    {
        Log.Debug("Obteniendo texto de InputTextBox");
        
        var inputBox = FindElement(InputTextBoxId);
        return inputBox.GetText();
    }

    /// <summary>
    /// Verifica si la ventana principal está visible.
    /// </summary>
    /// <returns>True si está visible, false en caso contrario</returns>
    public bool IsVisible()
    {
        return !Window.IsOffscreen;
    }
}
