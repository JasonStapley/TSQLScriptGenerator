namespace TSQLScriptGenerator;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new FormScriptGenerator());
    }
}
