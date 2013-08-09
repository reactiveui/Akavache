using System;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using ReactiveUI.Cocoa;
using ReactiveUI;
using Starter.Core.ViewModels;
using Akavache;
using System.Reactive.Linq;

namespace Starter.Views
{
    public partial class TestViewController : ReactiveViewController, IViewFor<TestViewModel>
    {
        static bool UserInterfaceIdiomIsPhone
        {
            get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
        }

        public TestViewController()
            : base (UserInterfaceIdiomIsPhone ? "TestViewController_iPhone" : "TestViewController_iPad", null)
        {
        }

        public override void DidReceiveMemoryWarning()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning();
            
            // Release any cached data, images, etc that aren't in use.
        }

        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();
            this.OneWayBind(ViewModel, x => x.TheGuid, x => x.TheGuid.Text);

            ViewModel = await BlobCache.LocalMachine.GetOrCreateObject("TestViewModel", () => {
                return new TestViewModel();
            });
        }

        TestViewModel _ViewModel;
        public TestViewModel ViewModel {
            get { return _ViewModel; }
            set { this.RaiseAndSetIfChanged(ref _ViewModel, value); }
        }

        object IViewFor.ViewModel {
            get { return ViewModel; }
            set { ViewModel = (TestViewModel)value; }
        }
    }
}

