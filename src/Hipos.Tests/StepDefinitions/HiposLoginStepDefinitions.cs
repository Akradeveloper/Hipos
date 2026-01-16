private Window WaitForWindowByTitle(string titlePart, int timeoutMs)
    {
        Window? foundWindow = null;

        var found = WaitHelper.WaitUntil(
            () =>
            {
                var automation = AppLauncher?.Automation;
                if (automation == null)
                {
                    return false;
                }

                var windows = automation.GetDesktop().FindAllChildren();
                foreach (var element in windows)
                {
                    var window = element.AsWindow();
                    if (window == null || window.IsOffscreen)
                    {
                        continue;
                    }

                    var title = window.Title ?? string.Empty;
                    if (title.Contains(titlePart, StringComparison.OrdinalIgnoreCase))
                    {
                        foundWindow = window;
                        return true;
                    }
                }

                return false;
            },
            timeoutMs,
            conditionDescription: $"ventana con título '{titlePart}'");

        if (!found || foundWindow == null)
        {
            throw new TimeoutException($"No se encontró la ventana con título '{titlePart}' en {timeoutMs}ms.");
        }

        return foundWindow;
    }