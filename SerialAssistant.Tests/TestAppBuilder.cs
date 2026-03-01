using Avalonia;
using Avalonia.Headless;
using SerialAssistant;

[assembly: AvaloniaTestApplication(typeof(SerialAssistant.Tests.TestAppBuilder))]

namespace SerialAssistant.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
