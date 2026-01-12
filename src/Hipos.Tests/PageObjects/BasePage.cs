using FlaUI.Core.AutomationElements;
using Hipos.Framework.Config;
using Hipos.Framework.Utils;
using Serilog;

namespace Hipos.Tests.PageObjects;

/// <summary>
/// Clase base para todos los Page Objects.
/// Proporciona funcionalidad común para interactuar con elementos.
/// </summary>
public abstract class BasePage
{
    protected Window Window { get; }
    protected int DefaultTimeout { get; }

    protected BasePage(Window window)
    {
        Window = window ?? throw new ArgumentNullException(nameof(window));
        DefaultTimeout = ConfigManager.Instance.DefaultTimeout;
    }

    /// <summary>
    /// Encuentra un elemento por su AutomationId y lo envuelve en un ElementWrapper.
    /// </summary>
    /// <param name="automationId">AutomationId del elemento</param>
    /// <returns>ElementWrapper del elemento encontrado</returns>
    protected ElementWrapper FindElement(string automationId)
    {
        Log.Debug("Buscando elemento en página: {AutomationId}", automationId);

        var element = WaitHelper.WaitForElement(Window, automationId, DefaultTimeout);
        
        if (element == null)
        {
            throw new InvalidOperationException(
                $"No se encontró el elemento con AutomationId: {automationId}");
        }

        return new ElementWrapper(element, DefaultTimeout);
    }

    /// <summary>
    /// Verifica si un elemento existe por su AutomationId.
    /// </summary>
    /// <param name="automationId">AutomationId del elemento</param>
    /// <returns>True si existe, false en caso contrario</returns>
    protected bool ElementExists(string automationId)
    {
        try
        {
            var element = Window.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
            return element != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Espera hasta que un elemento sea visible.
    /// </summary>
    /// <param name="automationId">AutomationId del elemento</param>
    /// <param name="timeoutMs">Timeout en milisegundos (opcional)</param>
    /// <returns>True si el elemento es visible, false si timeout</returns>
    protected bool WaitForElementVisible(string automationId, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeout;
        return WaitHelper.WaitUntil(
            () => ElementExists(automationId),
            timeout,
            conditionDescription: $"elemento '{automationId}' visible");
    }

    /// <summary>
    /// Trae la ventana al primer plano.
    /// Útil cuando se necesita asegurar que la ventana esté activa antes de una interacción.
    /// </summary>
    protected void EnsureWindowInForeground()
    {
        try
        {
            Window.Focus();
            Thread.Sleep(300); // Pequeña pausa para que la ventana responda
            Log.Debug("Ventana traída al frente en Page Object");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo traer la ventana al frente en Page Object");
        }
    }
}
