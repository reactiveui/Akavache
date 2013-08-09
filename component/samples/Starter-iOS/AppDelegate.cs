using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Starter.Views;
using ReactiveUI;

namespace Starter
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register ("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        // class-level declarations
        UIWindow window;
        TestViewController viewController;
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            // NB: GrossHackAlertTiem™:
            //
            // Monotouch appears to not load assemblies when you request them 
            // via Type.GetType, unlike every other platform (even 
            // Xamarin.Android). So, we've got to manually do what RxUI and 
            // Akavache would normally do for us
            var r = RxApp.MutableResolver;
            (new ReactiveUI.Cocoa.Registrations()).Register((f,t) => r.Register(f, t));
            (new ReactiveUI.Mobile.Registrations()).Register((f,t) => r.Register(f, t));
            (new Akavache.Registrations()).Register(r.Register);
            (new Akavache.Mobile.Registrations()).Register(r.Register);
            (new Akavache.Sqlite3.Registrations()).Register(r.Register);

            window = new UIWindow(UIScreen.MainScreen.Bounds);

            viewController = new TestViewController();
            window.RootViewController = viewController;
            window.MakeKeyAndVisible();
            
            return true;
        }
    }
}

