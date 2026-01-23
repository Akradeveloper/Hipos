using FlaUI.Core.AutomationElements;
using Serilog;

namespace Hipos.Tests.PageObjects;

/// <summary>
/// Page Object for the HIPOS main menu page.
/// Handles validation of main menu elements after successful login flow.
/// </summary>
public class HiposMainMenuPage : BasePage
{
    private static readonly string[] ReceiptPath = { "receipt" };
    private static readonly string[] ReceiptInfoPath = { "receiptinfo" };
    private static readonly string[] ArticleInfoPath = { "article_info" };
    private static readonly string[] MainDataPath = { "main_data" };
    private static readonly string[] KeypadPath = { "keypad" };

    public HiposMainMenuPage(Window window) : base(window)
    {
    }

    /// <summary>
    /// Verifies that the receipt element exists.
    /// </summary>
    /// <returns>True if the element exists, false otherwise</returns>
    public bool VerifyReceiptExists()
    {
        var exists = ElementExistsByPath(ReceiptPath);
        Log.Information("Elemento 'receipt' existe: {Exists}", exists);
        return exists;
    }

    /// <summary>
    /// Verifies that the receiptinfo element exists.
    /// </summary>
    /// <returns>True if the element exists, false otherwise</returns>
    public bool VerifyReceiptInfoExists()
    {
        var exists = ElementExistsByPath(ReceiptInfoPath);
        Log.Information("Elemento 'receiptinfo' existe: {Exists}", exists);
        return exists;
    }

    /// <summary>
    /// Verifies that the article_info element exists.
    /// </summary>
    /// <returns>True if the element exists, false otherwise</returns>
    public bool VerifyArticleInfoExists()
    {
        var exists = ElementExistsByPath(ArticleInfoPath);
        Log.Information("Elemento 'article_info' existe: {Exists}", exists);
        return exists;
    }

    /// <summary>
    /// Verifies that the main_data element exists.
    /// </summary>
    /// <returns>True if the element exists, false otherwise</returns>
    public bool VerifyMainDataExists()
    {
        var exists = ElementExistsByPath(MainDataPath);
        Log.Information("Elemento 'main_data' existe: {Exists}", exists);
        return exists;
    }

    /// <summary>
    /// Verifies that the keypad element exists.
    /// </summary>
    /// <returns>True if the element exists, false otherwise</returns>
    public bool VerifyKeypadExists()
    {
        var exists = ElementExistsByPath(KeypadPath);
        Log.Information("Elemento 'keypad' existe: {Exists}", exists);
        return exists;
    }

    /// <summary>
    /// Verifies that all required elements exist on the main menu page.
    /// </summary>
    /// <returns>True if all elements exist, false if any element is missing</returns>
    public bool VerifyAllElementsExist()
    {
        EnsureWindowInForeground();
        Log.Information("Verificando que todos los elementos requeridos del menú principal existan");

        var receiptExists = VerifyReceiptExists();
        var receiptInfoExists = VerifyReceiptInfoExists();
        var articleInfoExists = VerifyArticleInfoExists();
        var mainDataExists = VerifyMainDataExists();
        var keypadExists = VerifyKeypadExists();

        var allExist = receiptExists && receiptInfoExists && articleInfoExists && mainDataExists && keypadExists;

        if (allExist)
        {
            Log.Information("Todos los elementos requeridos del menú principal existen correctamente");
        }
        else
        {
            Log.Warning("Algunos elementos del menú principal no existen: receipt={Receipt}, receiptinfo={ReceiptInfo}, article_info={ArticleInfo}, main_data={MainData}, keypad={Keypad}",
                receiptExists, receiptInfoExists, articleInfoExists, mainDataExists, keypadExists);
        }

        return allExist;
    }
}