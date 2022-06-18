using Android.Content;
using Android.Content.Res;
using AndroidX.Activity.Result;

namespace ComputerUtils.Android
{
    public class AndroidCore
    {
        public static Context context { get; set; } = null;
        public static AssetManager assetManager { get; set; } = null;
        public static ActivityResultLauncher launcher { get; set; } = null;
    }
}