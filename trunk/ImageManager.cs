using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
                return item.NonCachedIcon ? resolved : "*";
            var rl = resolved.ToLower();
            return rl.EndsWith(".lnk")
                || rl.EndsWith(".exe")
                || rl.EndsWith(".cpl")
                ? resolved : Path.GetExtension(resolved);
        }

        public static ImageContainer GetImageContainer(PowerItem item, API.Shgfi iconNeeded)
        {
            Util.Post(new Action(() => item.Icon = GetImageContainerSync(item, iconNeeded)));
            return null;
        }

        public static ImageContainer GetImageContainerSync(PowerItem item, API.Shgfi iconNeeded)
        {
            var resolvedArg = PowerItemTree.GetResolvedArgument(item, false);
            var descr = GetObjectDescriptor(item, resolvedArg);
            lock (Cache)
            {
                var container = (ImageContainer)(Cache.ContainsKey(descr) ? Cache[descr] : null);
                if (container == null)
                {
                    container = new ImageContainer(resolvedArg, descr, 
                                    item.IsSpecialFolder ? item.SpecialFolderId : API.Csidl.INVALID);
                    Cache.Add(descr, container);
                    if (iconNeeded == API.Shgfi.SMALLICON)
                        container.ExtractSmall();
                    else
                        container.ExtractLarge();
                }
                //TODO: after templating all items with <Image> there'll be no need in this:
                if (iconNeeded == API.Shgfi.SMALLICON)
                    container.GenerateSmallImage();
                else
                    container.GenerateLargeImage();
                return container;
            }
        }



        public class ImageContainer
        {
            private readonly string _objectDescriptor;
            private readonly string _initialObject;
            private readonly API.Csidl _id;

            public ImageSource SmallBitmap { get; private set; }
            public System.Windows.Controls.Image SmallImage { get; private set; }
            public ImageSource LargeBitmap { get; private set; }
            public System.Windows.Controls.Image LargeImage { get; private set; }

            public ImageContainer(string objectToGetIcons, string typeDescriptor, API.Csidl specialId)
            {
                _initialObject = objectToGetIcons;
                _objectDescriptor = typeDescriptor;
                _id = specialId;
            }

            public void ExtractSmall()
            {
                if (SmallBitmap != null) 
                    return;
                var smallIconHandle = GetUnmanagedIcon(API.Shgfi.SMALLICON);
                if (smallIconHandle != IntPtr.Zero)
                {
                    SmallBitmap = ExtractInternal(smallIconHandle);
                    SmallBitmap.Freeze();
                    API.DestroyIcon(smallIconHandle);
                }
                else
                {
                    Debug.WriteLine("!!!ExtractSmall failed for {0} with code {1}", _initialObject, Marshal.GetLastWin32Error());
                }
            }

            public void GenerateSmallImage()
            {
                SmallImage = new System.Windows.Controls.Image {Source = SmallBitmap, Width=16, Height=16};
            }

            public void ExtractLarge()
            {
                if(LargeBitmap != null)
                    return;
                var largeIconHandle = GetUnmanagedIcon(API.Shgfi.LARGEICON);
                if (largeIconHandle != IntPtr.Zero)
                {
                    LargeBitmap = ExtractInternal(largeIconHandle);
                    LargeBitmap.Freeze();
                    API.DestroyIcon(largeIconHandle);
                }
                else
                {
                    Debug.WriteLine("!!!ExtractLarge failed for {0} with code {1}", _initialObject, Marshal.GetLastWin32Error());
                }
            }

            public void GenerateLargeImage()
            {
                LargeImage = new System.Windows.Controls.Image {Source = LargeBitmap, Width=32, Height=32};
            }

            private static BitmapSource ExtractInternal(IntPtr handle)
            {

                var bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(handle,
                                                      System.Windows.Int32Rect.Empty,
                                                      BitmapSizeOptions.FromEmptyOptions());
                bs.Freeze();
                return bs;
            }

            private IntPtr GetUnmanagedIcon(API.Shgfi iconType)
            {
                var shinfo = new API.Shfileinfo();
                var zeroFails = API.SHGetFileInfo(_initialObject, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), API.Shgfi.ICON | iconType);
                if (zeroFails == IntPtr.Zero && _id != API.Csidl.INVALID) //some ids on Win8 will work via this
                {
                    var temp = Util.GetDefaultIconResourceIdForClass(_initialObject);
                    if (!string.IsNullOrEmpty(temp))
                    {
                        zeroFails = Util.ResolveIconicResource(temp);
                    }
                    if(zeroFails == IntPtr.Zero)
                    {
                        var ppIdl = IntPtr.Zero;
                        var hRes = API.SHGetSpecialFolderLocation(IntPtr.Zero, _id, ref ppIdl);
                        zeroFails = (hRes != 0
                                         ? IntPtr.Zero
                                         : API.SHGetFileInfo(ppIdl, 0, ref shinfo, (uint) Marshal.SizeOf(shinfo),
                                                             API.Shgfi.ICON | API.Shgfi.PIDL | API.Shgfi.USEFILEATTRIBUTES | iconType));
                        Marshal.FreeCoTaskMem(ppIdl);                                       
                    }
                    else //ResolveIconicResource() succeeded and zeroFails contains required handle
                    {
                        shinfo.hIcon = zeroFails;
                    }
                }
                return zeroFails == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero ? IntPtr.Zero : shinfo.hIcon;
            }
        }
    }
}
