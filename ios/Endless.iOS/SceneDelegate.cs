using Foundation;
using UIKit;

namespace Endless.Apple.iOS;

[Register("SceneDelegate")]
public sealed class SceneDelegate : UIWindowSceneDelegate
{
    public override UIWindow? Window { get; set; }
    private ConnectionViewController? controller;

    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene) return;
        controller = new ConnectionViewController();
        Window = new UIWindow(windowScene)
        {
            RootViewController = controller
        };
        Window.MakeKeyAndVisible();
    }

    // Temporary interruptions (including credential AutoFill) must not tear down sign-in.
    public override void DidEnterBackground(UIScene scene) => controller?.CancelConnection();

    public override void DidDisconnect(UIScene scene)
    {
        controller?.CancelConnection();
        Window?.Dispose();
        Window = null;
        controller = null;
    }
}
