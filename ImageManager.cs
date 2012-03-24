using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Power8
{
    public static class ImageManager
    {
        private static readonly Hashtable Cache = new Hashtable(); 

        private static string GetObjectDescriptor(PowerItem item, string resolved)
        {
            if (item.IsFolder)
                return "*";
            var rl = resolved.ToLower();
            return rl.EndsWith(".lnk")
                || rl.EndsWith(".exe")
                || rl.EndsWith(".cpl")
                ? resolved : Path.GetExtension(resolved);
        }

        public static ImageContainer GetImageContainer(PowerItem item, API.Shgfi iconNeeded)
        {
            var psi = PowerItemTree.ResolveItem(item);
            var arg = item.IsFolder ? psi.Arguments : psi.FileName;
            var descr = GetObjectDescriptor(item, arg);
            lock (Cache)
            {
                var container = (ImageContainer) (Cache.ContainsKey(descr) ? Cache[descr] : null);
                if (container == null)
                {
                    container = new ImageContainer(arg, descr);
                    Cache.Add(descr, container);
                    if(iconNeeded == API.Shgfi.SHGFI_SMALLICON)
                        container.ExtractSmall();
                    else
                        container.ExtractLarge();
                }
                if(iconNeeded == API.Shgfi.SHGFI_SMALLICON)
                    container.GenerateSmallImage();
                else
                    container.GenerateLargeImage();
                return container;
            }
        }

        public class ImageContainer
        {
            public readonly string ObjectDescriptor, InitialObject;

            public IntPtr SmallIconHandle { get; private set; }
            public Icon SmallIcon { get; private set; }
            public ImageSource SmallBitmap { get; private set; }
            public System.Windows.Controls.Image SmallImage { get; private set; }
            public IntPtr LargeIconHandle { get; private set; }
            public Icon LargeIcon { get; private set; }
            public ImageSource LargeBitmap { get; private set; }
            public System.Windows.Controls.Image LargeImage { get; private set; }

            public ImageContainer(string objectToGetIcons, string typeDescriptor)
            {
                InitialObject = objectToGetIcons;
                ObjectDescriptor = typeDescriptor;
            }

            public void ExtractSmall()
            {
                if (SmallImage != null) 
                    return;
                SmallIconHandle = API.GetIconForFile(InitialObject, API.Shgfi.SHGFI_SMALLICON);
                SmallIcon = Icon.FromHandle(SmallIconHandle);
                SmallBitmap = ExtractInternal(SmallIcon);
            }

            public void GenerateSmallImage()
            {
                SmallImage = new System.Windows.Controls.Image {Source = SmallBitmap, Width=16, Height=16};
            }

            public void ExtractLarge()
            {
                if(LargeImage != null)
                    return;
                LargeIconHandle = API.GetIconForFile(InitialObject, API.Shgfi.SHGFI_LARGEICON);
                LargeIcon = Icon.FromHandle(LargeIconHandle);
                LargeBitmap = ExtractInternal(LargeIcon);
            }

            public void GenerateLargeImage()
            {
                LargeImage = new System.Windows.Controls.Image {Source = LargeBitmap, Width=32, Height=32};
            }

            private static BitmapImage ExtractInternal(Icon icon)
            {
                var stream = new MemoryStream();
                icon.ToBitmap().Save(stream, ImageFormat.Png);
                var img = new BitmapImage();
                img.BeginInit();
                img.CreateOptions = BitmapCreateOptions.None;
                img.CacheOption = BitmapCacheOption.Default;
                img.StreamSource = stream;
                img.EndInit();
                return img;
            }
        }
    }
}
