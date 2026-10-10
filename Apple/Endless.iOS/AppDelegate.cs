using Foundation;
using UIKit;

namespace Endless.Apple.iOS;

[Register("AppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate
{
    public override UISceneConfiguration GetConfiguration(UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options) =>
        new("Default Configuration", connectingSceneSession.Role) { DelegateType = typeof(SceneDelegate) };

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions) => true;
}
