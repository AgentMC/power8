using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Power8.Helpers;

namespace Power8
{
    /// <summary>
    /// Maintains cached lists of icons, resolves icons from resources and so on...
    /// </summary>
    public static class ImageManager
    {
        //Image cache
        private static readonly Hashtable Cache = new Hashtable();
        //Image processing queue
        private static readonly ConcurrentQueue<Tuple<PowerItem, API.Shgfi>> ImageQueue =
            new ConcurrentQueue<Tuple<PowerItem, API.Shgfi>>();

        static ImageManager()
        {
            Util.ForkStart(ImageProcessor, "Image processsor");
        }

        private static void ImageProcessor()
        {
            begin:

            while (ImageQueue.Count > 0)
            {
                Tuple<PowerItem, API.Shgfi> e;
                if (ImageQueue.TryDequeue(out e))
                {
                    var asyncContainer = GetImageContainerSync(e.Item1, e.Item2);
                    Util.Send(() => e.Item1.Icon = asyncContainer);
                }
                else
                {
                    break;
                }
            }

            if (!Util.MainDisp.HasShutdownStarted)
            {
                Thread.Sleep(333);
                goto begin; //just don't want unneeded code nesting here, anyway IL will be same.
            }
        }

        /// <summary>
        /// Gets a string that represents a kind of tag for icon for PowerItem passed
        /// </summary>
        /// <param name="item">PowerItem which we need icon for</param>
        /// <param name="resolved">Resolved argument for item.</param>
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

        /// <summary>
        /// Starts asynchronous extraction of ImageContainer for PowerItem
        /// </summary>
        /// <param name="item">PowerItem we need icon extracted for</param>
        /// <param name="iconNeeded">type of icon needed - small or large</param>
        /// <returns>Always null</returns>
        public static ImageContainer GetImageContainer(PowerItem item, API.Shgfi iconNeeded)
        {
            ImageQueue.Enqueue(new Tuple<PowerItem, API.Shgfi>(item, iconNeeded));
            return null;
        }

        /// <summary>
        /// Synchronous getter of an icon for PowerItem
        /// </summary>
        /// <param name="item">PowerItem we need icon extracted for</param>
        /// <param name="iconNeeded">type of icon needed - small or large</param>
        /// <returns>ImageContainer with ImageSources extracted. Can be null.</returns>
        public static ImageContainer GetImageContainerSync(PowerItem item, API.Shgfi iconNeeded)
        {
            Log.Raw("begin>>>>>>>>>>>>>>>", item.FriendlyName);
            //Checking if there's cached ImageContainer
            string resolvedArg, descr;
            try
            {
                resolvedArg = PowerItemTree.GetResolvedArgument(item);
                descr = GetObjectDescriptor(item, resolvedArg);
            }
            catch (IOException)
            {
                return null;
            }
            lock (Cache)
            {
                var container = (ImageContainer)(Cache.ContainsKey(descr) ? Cache[descr] : null);
                Log.Fmt("arg<={0}, descr<={1}, container<={2}", resolvedArg, descr,
                        (container != null ? "not " : "") + "null");
                if (container == null) //No cached instance
                {
                    container = new ImageContainer(resolvedArg, descr, item.SpecialFolderId);
                    Cache.Add(descr, container);
                    if (iconNeeded == API.Shgfi.SMALLICON)
                        container.ExtractSmall();
                    else
                        container.ExtractLarge();
                }
#if DEBUG
                Log.Raw("end<<<<<<<<<<<<<<", item.FriendlyName);
#endif
                return container;
            }
        }

        /// <summary>
        /// Returns ImageContainer for hIcon provided
        /// </summary>
        /// <param name="description">Object description, tag, under which the container will be stored in cache</param>
        /// <param name="unmanagedIcon">HICON you obtained after some unmanaged interactions. DestroyIcon() is NOT being
        /// called automatically</param>
        /// <returns>ImageContainer with ImageSource`s for the given hIcon</returns>
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


        /// <summary>
        /// Holds the cached instance of bitmap sources for big and large icons to be displayed.
        /// Can automatically extract icons from the file or special object given.
        /// </summary>
        public class ImageContainer
        {
// ReSharper disable NotAccessedField.Local
            private readonly string _objectDescriptor; //needed for debug purposes
// ReSharper restore NotAccessedField.Local
            private readonly string _initialObject; //Path to file or special object
            private readonly API.Csidl _id; //SpecialFolderId from source PowerItem, if any
            private ImageSource _smallBitmap, _largeBitmap; 
            private bool _smallExtracted, _largeExtracted;

            /// <summary>
            /// Gets 16x16 BitmapSource-representation of target icon
            /// </summary>
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
            /// <summary>
            /// Gets 32x32 BitmapSource-representation of target icon
            /// </summary>
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

            /// <summary>
            /// Constructs the instance of ImageContainer from data from PowerItem
            /// </summary>
            /// <param name="objectToGetIcons">Path to file or special object</param>
            /// <param name="typeDescriptor">Tag that will be set in cache for this ImageContainer. Used for debugging.</param>
            /// <param name="specialId">SpecialFolderId from source PowerItem</param>
            public ImageContainer(string objectToGetIcons, string typeDescriptor, API.Csidl specialId)
            {
                _initialObject = objectToGetIcons;
                _objectDescriptor = typeDescriptor;
                _id = specialId;
            }
            /// <summary>
            /// Constructs the instance of ImageContainer from the HICON extracted already
            /// </summary>
            /// <param name="unmanagedIcon">HICON you have already extracted</param>
            public ImageContainer(IntPtr unmanagedIcon)
            {
                SmallBitmap = ExtractInternal(unmanagedIcon);
                SmallBitmap.Freeze();
                LargeBitmap = SmallBitmap;
            }

            /// <summary>
            /// Extracts 16*16 icon when ImageContainer is constructed with PowerItem data
            /// </summary>
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
                    Log.Raw("FAILED with code " + Marshal.GetLastWin32Error(), _initialObject);
                }
#endif
                _smallExtracted = true;
            }
            /// <summary>
            /// Extracts 32*32 icon when ImageContainer is constructed with PowerItem data
            /// </summary>
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
                    Log.Raw("FAILED with code " + Marshal.GetLastWin32Error(), _initialObject);
                }
#endif
                _largeExtracted = true;
            }

            /// <summary>
            /// Converts HICON to BitmapSource, without calling of DestroyIcon()
            /// </summary>
            /// <param name="handle">HICON that is already extracted</param>
            private static BitmapSource ExtractInternal(IntPtr handle)
            {
                var bs = Imaging.CreateBitmapSourceFromHIcon(handle, System.Windows.Int32Rect.Empty,
                                                             BitmapSizeOptions.FromEmptyOptions());
                bs.Freeze();
                return bs;
            }

            /// <summary>
            /// Returns HICON of provided size (or of default size if requested one isn't available)
            /// </summary>
            /// <param name="iconType"></param>
            private IntPtr GetUnmanagedIcon(API.Shgfi iconType)
            {
                Log.Raw("begin", _initialObject);
                //Way 1, straightforward: "Hey shell, give me an icon for that file!"
                var shinfo = new API.ShfileinfoW();
                var zeroFails = API.SHGetFileInfo(_initialObject, 0, ref shinfo, (uint) Marshal.SizeOf(shinfo), API.Shgfi.ICON | iconType);
                Log.Raw("ShGetFileInfo returned " + zeroFails);
                if (zeroFails == IntPtr.Zero) //lot of stuff will work via this
                {
                    //Shell failed
                    //Way 2: way around: "Hey registry, and how should display the stuff of a kind?"
                    var temp = Util.GetDefaultIconResourceIdForClass(_initialObject);
                    Log.Raw("GetDefaultIconResourceIdForClass returned " + (temp ?? "NULL!!"));
                    if (!string.IsNullOrEmpty(temp))
                    {
                        zeroFails = Util.ResolveIconicResource(temp);
                        Log.Raw("ResolveIconicResource returned " + zeroFails);
                    }
                    if(zeroFails != IntPtr.Zero)//ResolveIconicResource() succeeded and zeroFails contains required handle
                    {
                        shinfo.hIcon = zeroFails;
                    }
                    else if (_id != API.Csidl.INVALID)//For PowerItems initialized without argument but with folderId
                    {//No icon, or Registry doesn't know
                        //Way 3, cumbersome: "Hey shell, I know that stuff means something for ya. Give me the icon for the thing this staff means!"
                        var ppIdl = IntPtr.Zero;
                        var hRes = API.SHGetSpecialFolderLocation(IntPtr.Zero, _id, ref ppIdl); //I know, obsolete, but works ;)
                        Log.Fmt("SHGetSp.F.Loc. for id<={0} returned result code {1}", _id, hRes);
                        zeroFails = (hRes != 0
                                         ? IntPtr.Zero
                                         : API.SHGetFileInfo(ppIdl, 0, ref shinfo, (uint) Marshal.SizeOf(shinfo),
                                                             API.Shgfi.ICON | API.Shgfi.PIDL | API.Shgfi.USEFILEATTRIBUTES | iconType));
                        Marshal.FreeCoTaskMem(ppIdl);
                        Log.Raw("ShGetFileInfo (2p) returned " + zeroFails);      
                    }
                }
                Log.Fmt("end<<<<<, zf={0}, hi={1}", zeroFails, shinfo.hIcon);
                return zeroFails == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero ? IntPtr.Zero : shinfo.hIcon;
            }
        }
    }
}
