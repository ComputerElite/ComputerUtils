using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Java.Lang;
using Java.Util;

namespace ComputerUtils.Android
{
    public class ChainLoadActivity : Activity
    {
        public static List<Activity> activityList = new List<Activity>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Bundle extras = Intent.Extras;
            ApplicationInfo launchApp = (ApplicationInfo)Objects.RequireNonNull(extras.GetParcelable("app"));

            // Get normal launch intent
            PackageManager pm = Application.Context.PackageManager;
            Intent normalIntent = pm.GetLaunchIntentForPackage(launchApp.PackageName);
            StartActivity(normalIntent);
            activityList.Add(this);
        }

        protected override void OnDestroy()
        {
            if (IsFinishing) activityList.Remove(this);
            base.OnDestroy();
        }
    }
    
    public class ChainLoadActivityPhone : ChainLoadActivity {}
    public class ChainLoadActivitySmall : ChainLoadActivity {}
    public class ChainLoadActivityLarge : ChainLoadActivity {}
    public class ChainLoadActivityHuge : ChainLoadActivity {}
}