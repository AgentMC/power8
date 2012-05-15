using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
#if DEBUG
using System.Diagnostics;
#endif

namespace Power8
{
    public static class ImageManager
    {
        private static readonly Hashtable Cache = new Hashtable(); 

        private static string GetObjectDescriptor(PowerItem item, string resolved)
        {
            if (item.IsFolder || item.IsSpecialObject)
                return item.NonCachedIcon ? resolved : "*";
            var rl = resolved.ToLower();
            return rl.EndsWith(".lnk")
                || rl.EndsWith(".exe")
                || rl.EndsWith(".cpl")
                ? resolved : Path.GetExtension(resolved);
        }

        public static ImageContainer GetImageContainer(PowerItem item, API.Shgfi iconNeeded)
        {
            Util.Post(() => item.Icon = GetImageContainerSync(item, iconNeeded));
            return null;
        }

        public static ImageContainer GetImageContainerSync(PowerItem item, API.Shgfi iconNeeded)
        {
#if DEBUG
            var dbgLine = "GICS for " + item.FriendlyName + ": ";
            Debug.WriteLine(dbgLine + "begin");
#endif
            string resolvedArg, descr;
            try
            {
                resolvedArg = PowerItemTree.GetResolvedArgument(item, false);
                descr = GetObjectDescriptor(item, resolvedArg);
            }
            catch (IOException)
            {
                return null;
            }
            lock (Cache)
            {
                var container = (ImageContainer)(Cache.ContainsKey(descr) ? Cache[descr] : null);
#if DEBUG
                Debug.WriteLine("{3}arg<={0}, descr<={1}, container<={2}", resolvedArg, descr,
                                (container != null ? "not " : "") + "null", dbgLine);
#endif
                if (container == null)
                {
                    container = new ImageContainer(resolvedArg, descr, item.SpecialFolderId);
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
#if DEBUG
                Debug.WriteLine(dbgLine + "end<<<<<<<<<<<<<<");
#endif
                return container;
            }
        }

        public static ImageContainer GetImageContainerForIconSync(string description, IntPtr unmanagedIcon)
        {
            lock (Cache)
            {
                if (Cache.ContainsKey(description))
                    return (ImageContainer)Cache[description];
                var container = new ImageContainer(unmanagedIcon);
                Cache.Add(description, container);
                return container;
            }
        }



        public class ImageContainer
        {
// ReSharper disable NotAccessedField.Local
            private readonly string _objectDescriptor; //needed for debug purposes
// ReSharper restore NotAccessedField.Local
            private readonly string _initialObject;
            private readonly API.Csidl _id;
            private ImageSource _smallBitmap, _largeBitmap;
            private System.Windows.Controls.Image _smallImage, _largeImage;
            private bool _smallExtracted, _largeExtracted;

            public ImageSource SmallBitmap
            {
                get
                {
                    if (!_smallExtracted)
                        ExtractSmall();
                    return _smallBitmap;
                } 
                private set { _smallBitmap = value; }
            }
            public ImageSource LargeBitmap 
            { 
                get
                {
                    if (!_largeExtracted)
                        ExtractLarge();
                    return _largeBitmap;
                }
                private set { _largeBitmap = value; }
            }
            public System.Windows.Controls.Image SmallImage
            {
                get
                {
                    if (_smallImage == null)
                        GenerateSmallImage();
                    return _smallImage;
                }
                private set { _smallImage = value; }
            }
            public System.Windows.Controls.Image LargeImage 
            {
                get
                {
                    if(_largeImage == null)
                        GenerateLargeImage();
                    return _largeImage;
                } 
                private set { _largeImage = value; } 
            }

            public ImageContainer(string objectToGetIcons, string typeDescriptor, API.Csidl specialId)
            {
                _initialObject = objectToGetIcons;
                _objectDescriptor = typeDescriptor;
                _id = specialId;
            }

            public ImageContainer(IntPtr unmanagedIcon)
            {
                SmallBitmap = ExtractInternal(unmanagedIcon);
                SmallBitmap.Freeze();
                LargeBitmap = SmallBitmap;
            }

            public void ExtractSmall()
            {
                if (_smallBitmap != null) 
                    return;
                var smallIconHandle = GetUnmanagedIcon(API.Shgfi.SMALLICON);
                if (smallIconHandle != IntPtr.Zero)
                {
                    SmallBitmap = ExtractInternal(smallIconHandle);
                    SmallBitmap.Freeze();
                    Util.PostBackgroundIconDestroy(smallIconHandle);
                }
#if DEBUG
                else
                {
                    Debug.WriteLine("!!!ExtractSmall failed for {0} with code {1}", _initialObject, Marshal.GetLastWin32Error());
                }
#endif
                _smallExtracted = true;
            }

            public void GenerateSmallImage()
            {
                SmallImage = new System.Windows.Controls.Image {Source = SmallBitmap, Width=16, Height=16};
            }

            public void ExtractLarge()
            {
                if(_largeBitmap != null)
                    return;
                var largeIconHandle = GetUnmanagedIcon(API.Shgfi.LARGEICON);
                if (largeIconHandle != IntPtr.Zero)
                {
                    LargeBitmap = ExtractInternal(largeIconHandle);
                    LargeBitmap.Freeze();
                    Util.PostBackgroundIconDestroy(largeIconHandle);
                }
#if DEBUG
                else
                {
                    Debug.WriteLine("!!!ExtractLarge failed for {0} with code {1}", _initialObject, Marshal.GetLastWin32Error());
                }
#endif
                _largeExtracted = true;
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
#if DEBUG
                var dbgLine = "GUIc for " + _initialObject + ": ";
                Debug.WriteLine(dbgLine + "begin");
#endif
                var shinfo = new API.Shfileinfo();
                var zeroFails = API.SHGetFileInfo(_initialObject, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), API.Shgfi.ICON | iconType);
#if DEBUG
                Debug.WriteLine(dbgLine + "ShGetFileInfo returned " + zeroFails);
#endif
                if (zeroFails == IntPtr.Zero) //lot of stuff will work via this
                {
                    var temp = Util.GetDefaultIconResourceIdForClass(_initialObject);
#if DEBUG
                    Debug.WriteLine(dbgLine + "GetDefaultIconResourceIdForClass returned " + (temp ?? "NULL!!"));
#endif
                    if (!string.IsNullOrEmpty(temp))
                    {
                        zeroFails = Util.ResolveIconicResource(temp);
#if DEBUG
                        Debug.WriteLine(dbgLine + "ResolveIconicResource returned " + zeroFails);
#endif
                    }
                    if(zeroFails != IntPtr.Zero)//ResolveIconicResource() succeeded and zeroFails contains required handle
                    {
                        shinfo.hIcon = zeroFails;
                    }
                    else if (_id != API.Csidl.INVALID)//For PowerItems initialized without argument but with folderId
                    {
                        var ppIdl = IntPtr.Zero;
                        var hRes = API.SHGetSpecialFolderLocation(IntPtr.Zero, _id, ref ppIdl);
#if DEBUG
                        Debug.WriteLine("{2}SHGetSp.F.Loc. for id<={0} returned result code {1}", _id, hRes, dbgLine);
#endif
                        zeroFails = (hRes != 0
                                         ? IntPtr.Zero
                                         : API.SHGetFileInfo(ppIdl, 0, ref shinfo, (uint) Marshal.SizeOf(shinfo),
                                                             API.Shgfi.ICON | API.Shgfi.PIDL | API.Shgfi.USEFILEATTRIBUTES | iconType));
                        Marshal.FreeCoTaskMem(ppIdl);
#if DEBUG
                        Debug.WriteLine(dbgLine + "ShGetFileInfo (2p) returned " + zeroFails);      
#endif
                    }
                }
#if DEBUG
                Debug.WriteLine("{2}end<<<<<, zf={0}, hi={1}", zeroFails, shinfo.hIcon, dbgLine);
#endif
                return zeroFails == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero ? IntPtr.Zero : shinfo.hIcon;
            }
        }
    }
}
