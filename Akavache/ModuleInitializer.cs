/// <summary>
/// Used by the ModuleInit. All code inside the Initialize method is ran as soon as the assembly is loaded.
/// </summary>
using Splat;
using Akavache;

public static class ModuleInitializer
{
    /// <summary>
    /// Initializes the module.
    /// </summary>
    public static void Initialize()
    {
        Locator.RegisterResolverCallbackChanged(() => 
        {
            if (Locator.CurrentMutable == null) return;
            Locator.CurrentMutable.InitializeAkavache();
        });
    }
}