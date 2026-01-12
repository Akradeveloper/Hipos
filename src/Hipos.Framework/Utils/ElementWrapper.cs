using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using Serilog;

namespace Hipos.Framework.Utils;

/// <summary>
/// Wrapper alrededor de AutomationElement que proporciona una API simplificada
/// con esperas implícitas y logging automático.
/// </summary>
public class ElementWrapper
{
    private readonly AutomationElement _element;
    private readonly int _defaultTimeout;

    public ElementWrapper(AutomationElement element, int defaultTimeout = 5000)
    {
        _element = element ?? throw new ArgumentNullException(nameof(element));
        _defaultTimeout = defaultTimeout;
    }

    /// <summary>
    /// Obtiene el elemento subyacente de FlaUI.
    /// </summary>
    public AutomationElement Element => _element;

    /// <summary>
    /// Hace click en el elemento.
    /// </summary>
    public void Click()
    {
        Log.Information("Click en elemento: {Name} (AutomationId: {Id})", 
            _element.Name, _element.AutomationId);

        try
        {
            // Esperar a que sea clickeable
            if (!WaitHelper.WaitForElementClickable(_element, _defaultTimeout))
            {
                throw new InvalidOperationException(
                    $"Elemento no clickeable después de {_defaultTimeout}ms: {_element.Name}");
            }

            _element.Click();
            Log.Debug("Click exitoso en: {Name}", _element.Name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al hacer click en elemento: {Name}", _element.Name);
            throw;
        }
    }

    /// <summary>
    /// Establece el texto en el elemento.
    /// </summary>
    /// <param name="text">Texto a establecer</param>
    public void SetText(string text)
    {
        Log.Information("Estableciendo texto en elemento: {Name} - Texto: '{Text}'", 
            _element.Name, text);

        try
        {
            // Esperar a que esté habilitado
            if (!WaitHelper.WaitForElementEnabled(_element, _defaultTimeout))
            {
                throw new InvalidOperationException(
                    $"Elemento no habilitado después de {_defaultTimeout}ms: {_element.Name}");
            }

            // Limpiar texto existente
            _element.Focus();
            Keyboard.TypeSimultaneously(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL, FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
            Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);

            // Escribir nuevo texto
            _element.AsTextBox().Text = text;
            
            Log.Debug("Texto establecido exitosamente en: {Name}", _element.Name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al establecer texto en elemento: {Name}", _element.Name);
            throw;
        }
    }

    /// <summary>
    /// Obtiene el texto del elemento.
    /// </summary>
    /// <returns>El texto del elemento</returns>
    public string GetText()
    {
        Log.Debug("Obteniendo texto de elemento: {Name}", _element.Name);

        try
        {
            string text;
            
            // Intentar diferentes formas de obtener el texto
            if (_element.Patterns.Text.IsSupported)
            {
                text = _element.Patterns.Text.Pattern.DocumentRange.GetText(-1);
            }
            else if (_element.Patterns.Value.IsSupported)
            {
                text = _element.Patterns.Value.Pattern.Value.Value ?? string.Empty;
            }
            else
            {
                text = _element.Name;
            }

            Log.Debug("Texto obtenido de {Name}: '{Text}'", _element.Name, text);
            return text;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al obtener texto de elemento: {Name}", _element.Name);
            throw;
        }
    }

    /// <summary>
    /// Verifica si el elemento está habilitado.
    /// </summary>
    /// <returns>True si está habilitado, false en caso contrario</returns>
    public bool IsEnabled()
    {
        try
        {
            var enabled = _element.IsEnabled;
            Log.Debug("Elemento {Name} está habilitado: {Enabled}", _element.Name, enabled);
            return enabled;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al verificar si elemento está habilitado: {Name}", _element.Name);
            return false;
        }
    }

    /// <summary>
    /// Verifica si el elemento es visible.
    /// </summary>
    /// <returns>True si es visible, false en caso contrario</returns>
    public bool IsVisible()
    {
        try
        {
            var visible = !_element.IsOffscreen;
            Log.Debug("Elemento {Name} es visible: {Visible}", _element.Name, visible);
            return visible;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al verificar si elemento es visible: {Name}", _element.Name);
            return false;
        }
    }

    /// <summary>
    /// Espera hasta que el elemento exista.
    /// </summary>
    /// <param name="timeoutMs">Timeout en milisegundos (opcional)</param>
    /// <returns>True si el elemento existe, false si timeout</returns>
    public bool WaitUntilExists(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _defaultTimeout;
        return WaitHelper.WaitUntil(
            () => _element != null && _element.IsAvailable,
            timeout,
            conditionDescription: $"elemento '{_element.Name}' existe");
    }
}
