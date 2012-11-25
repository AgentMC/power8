using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;

namespace Power8
{
    public static class API
    {
        // ReSharper disable InconsistentNaming
        #region Identifiers

        public static class WndIds
        {
            public const string METRO_EDGE_WND = "EdgeUiInputWndClass";
            public const string TRAY_WND_CLASS = "Shell_TrayWnd";
            public const string TRAY_NTF_WND_CLASS = "TrayNotifyWnd";
            public const string TRAY_REBAR_WND_CLASS = "ReBarWindow32";
            public const string SH_DSKTP_WND_CLASS = "TrayShowDesktopButtonWClass";
            public const string SH_DSKTP_START_CLASS = "Button";
        }

        public static class SEVerbs
        {
            public const string Edit = "edit";
            public const string Explore = "explore";
            public const string Find = "find";
            public const string Open = "open";
            public const string OpenAs = "openas";
            public const string OpenNew = "opennew";
            public const string Print = "print";
            public const string Properties = "properties";
            public const string RunAsAdmin = "runas";
        }

        public static class ShNs
        {
            //Explorer /N,{ns}
            public const string MyComputer = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
            public const string AllControlPanelItems =
                @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{21EC2020-3AEA-1069-A2DD-08002B30309D}";
            public const string ControlPanel = "::{26EE0668-A00A-44D7-9371-BEB064C98683}";
            public const string PrintersAndtelecopiers =
                @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{21EC2020-3AEA-1069-A2DD-08002B30309D}\::{2227A280-3AEA-1069-A2DE-08002B30309D}";
            public const string Fonts =
                @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{21EC2020-3AEA-1069-A2DD-08002B30309D}\::{D20EA4E1-3957-11d2-A40B-0C5020524152}";
            public const string ScannersAndCameras =
                @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{21EC2020-3AEA-1069-A2DD-08002B30309D}\::{E211B736-43FD-11D1-9EFB-0000F8757FCD}";
            public const string NetworkConnections =
                @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{21EC2020-3AEA-1069-A2DD-08002B30309D}\::{7007ACC7-3202-11D1-AAD2-00805FC1270E}";
            public const string AdministrationTools =
                @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{21EC2020-3AEA-1069-A2DD-08002B30309D}\::{D20EA4E1-3957-11d2-A40B-0C5020524153}";
            public const string TasksScheduler =
                @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{21EC2020-3AEA-1069-A2DD-08002B30309D}\::{D6277990-4C6A-11CF-8D87-00AA0060F5BF}";
            public const string WebFolders =
                @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{BDEADF00-C265-11D0-BCED-00A0C90AB50F}";
            public const string MyDocuments = "::{450D8FBA-AD25-11D0-98A8-0800361B1103}";
            public const string RecycleBin = "::{645FF040-5081-101B-9F08-00AA002F954E}";
            public const string NetworkNeighbourhood = "::{208D2C60-3AEA-1069-A2D7-08002B30309D}";
            public const string DefaultNavigator = "::{871C5380-42A0-1069-A2EA-08002B30309D}";
            public const string ComputerSearchResultsFolder = "::{1F4DE370-D627-11D1-BA4F-00A0C91EEDBA}";
            public const string NetworkSearchResultsComputer = "::{E17D4FC0-5564-11D1-83F2-00A0C90DC849}";
            public const string Libraries = "::{031E4825-7B94-4dc3-B131-E946B44C8DD5}";
        }

        public static class KnFldrIds
        {
            public const string AddNewPrograms = "{de61d971-5ebc-4f02-a3a9-6c82895e5c04}";
            public const string AdminTools = "{724EF170-A42D-4FEF-9F26-B60E846FBA4F}";
            public const string ApplicationShortcuts = "{A3918781-E5F2-4890-B3D9-A7E54332328C}";
            public const string AppsFolder = "{1e87508d-89c2-42f0-8a7e-645a0f50ca58}";
            public const string AppUpdates = "{a305ce99-f527-492b-8b1a-7e76fa98d6e4}";
            public const string CDBurning = "{9E52AB10-F80D-49DF-ACB8-4330F5687855}";
            public const string ChangeRemovePrograms = "{df7266ac-9274-4867-8d55-3bd661de872d}";
            public const string CommonAdminTools = "{D0384E7D-BAC3-4797-8F14-CBA229B392B5}";
            public const string CommonOEMLinks = "{C1BAE2D0-10DF-4334-BEDD-7AA20B227A9D}";
            public const string CommonPrograms = "{0139D44E-6AFE-49F2-8690-3DAFCAE6FFB8}";
            public const string CommonStartMenu = "{A4115719-D62E-491D-AA7C-E74B8BE3B067}";
            public const string CommonStartup = "{82A5EA35-D9CD-47C5-9629-E15D2F714E6E}";
            public const string CommonTemplates = "{B94237E7-57AC-4347-9151-B08C6C32D1F7}";
            public const string ComputerFolder = "{0AC0837C-BBF8-452A-850D-79D08E667CA7}";
            public const string ConflictFolder = "{4bfefb45-347d-4006-a5be-ac0cb0567192}";
            public const string ConnectionsFolder = "{6F0CD92B-2E97-45D1-88FF-B0D186B8DEDD}";
            public const string Contacts = "{56784854-C6CB-462b-8169-88E350ACB882}";
            public const string ControlPanelFolder = "{82A74AEB-AEB4-465C-A014-D097EE346D63}";
            public const string Cookies = "{2B0F765D-C0E9-4171-908E-08A611B84FF6}";
            public const string Desktop = "{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}";
            public const string DeviceMetadataStore = "{5CE4A5E9-E4EB-479D-B89F-130C02886155}";
            public const string DocumentsLibrary = "{7B0DB17D-9CD2-4A93-9733-46CC89022E7C}";
            public const string Downloads = "{374DE290-123F-4565-9164-39C4925E467B}";
            public const string Favorites = "{1777F761-68AD-4D8A-87BD-30B759FA33DD}";
            public const string Fonts = "{FD228CB7-AE11-4AE3-864C-16F3910AB8FE}";
            public const string Games = "{CAC52C1A-B53D-4edc-92D7-6B2E8AC19434}";
            public const string GameTasks = "{054FAE61-4DD8-4787-80B6-090220C4B700}";
            public const string History = "{D9DC8A3B-B784-432E-A781-5A1130A75963}";
            public const string HomeGroup = "{52528A6B-B9E3-4ADD-B60D-588C2DBA842D}";
            public const string HomeGroupCurrentUser = "{9B74B6A3-0DFD-4f11-9E78-5F7800F2E772}";
            public const string ImplicitAppShortcuts = "{BCB5256F-79F6-4CEE-B725-DC34E402FD46}";
            public const string InternetCache = "{352481E8-33BE-4251-BA85-6007CAEDCF9D}";
            public const string InternetFolder = "{4D9F7874-4E0C-4904-967B-40B0D20C3E4B}";
            public const string Libraries = "{1B3EA5DC-B587-4786-B4EF-BD1DC332AEAE}";
            public const string Links = "{bfb9d5e0-c6a9-404c-b2b2-ae6db6af4968}";
            public const string LocalAppData = "{F1B32785-6FBA-4FCF-9D55-7B8E7F157091}";
            public const string LocalAppDataLow = "{A520A1A4-1780-4FF6-BD18-167343C5AF16}";
            public const string LocalizedResourcesDir = "{2A00375E-224C-49DE-B8D1-440DF7EF3DDC}";
            public const string Music = "{4BD8D571-6D19-48D3-BE97-422220080E43}";
            public const string MusicLibrary = "{2112AB0A-C86A-4FFE-A368-0DE96E47012E}";
            public const string NetHood = "{C5ABBF53-E17F-4121-8900-86626FC2C973}";
            public const string NetworkFolder = "{D20BEEC4-5CA8-4905-AE3B-BF251EA09B53}";
            public const string OriginalImages = "{2C36C0AA-5812-4b87-BFD0-4CD0DFB19B39}";
            public const string PhotoAlbums = "{69D2CF90-FC33-4FB7-9A0C-EBB0F0FCB43C}";
            public const string PicturesLibrary = "{A990AE9F-A03B-4E80-94BC-9912D7504104}";
            public const string Pictures = "{33E28130-4E1E-4676-835A-98395C3BC3BB}";
            public const string Playlists = "{DE92C1C7-837F-4F69-A3BB-86E631204A23}";
            public const string PrintersFolder = "{76FC4E2D-D6AD-4519-A663-37BD56068185}";
            public const string PrintHood = "{9274BD8D-CFD1-41C3-B35E-B13F55A758F4}";
            public const string Profile = "{5E6C858F-0E22-4760-9AFE-EA3317B67173}";
            public const string ProgramData = "{62AB5D82-FDC1-4DC3-A9DD-070D1D495D97}";
            public const string ProgramFiles = "{905e63b6-c1bf-494e-b29c-65b732d3d21a}";
            public const string ProgramFilesX64 = "{6D809377-6AF0-444b-8957-A3773F02200E}";
            public const string ProgramFilesX86 = "{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}";
            public const string ProgramFilesCommon = "{F7F1ED05-9F6D-47A2-AAAE-29D317C6F066}";
            public const string ProgramFilesCommonX64 = "{6365D5A7-0F0D-45E5-87F6-0DA56B6A4F7D}";
            public const string ProgramFilesCommonX86 = "{DE974D24-D9C6-4D3E-BF91-F4455120B917}";
            public const string Programs = "{A77F5D77-2E2B-44C3-A6A2-ABA601054A51}";
            public const string Public = "{DFDF76A2-C82A-4D63-906A-5644AC457385}";
            public const string PublicDesktop = "{C4AA340D-F20F-4863-AFEF-F87EF2E6BA25}";
            public const string PublicDocuments = "{ED4824AF-DCE4-45A8-81E2-FC7965083634}";
            public const string PublicDownloads = "{3D644C9B-1FB8-4f30-9B45-F670235F79C0}";
            public const string PublicGameTasks = "{DEBF2536-E1A8-4c59-B6A2-414586476AEA}";
            public const string PublicLibraries = "{48DAF80B-E6CF-4F4E-B800-0E69D84EE384}";
            public const string PublicMusic = "{3214FAB5-9757-4298-BB61-92A9DEAA44FF}";
            public const string PublicPictures = "{B6EBFB86-6907-413C-9AF7-4FC2ABF07CC5}";
            public const string PublicRingtones = "{E555AB60-153B-4D17-9F04-A5FE99FC15EC}";
            public const string PublicUserTiles = "{0482af6c-08f1-4c34-8c90-e17ec98b1e17}";
            public const string PublicVideos = "{2400183A-6185-49FB-A2D8-4A392A602BA3}";
            public const string QuickLaunch = "{52a4f021-7b75-48a9-9f6b-4b87a210bc8f}";
            public const string Recent = "{AE50C081-EBD2-438A-8655-8A092E34987A}";
            public const string RecordedTVLibrary = "{1A6FDBA2-F42D-4358-A798-B74D745926C5}";
            public const string RecycleBinFolder = "{B7534046-3ECB-4C18-BE4E-64CD4CB7D6AC}";
            public const string ResourceDir = "{8AD10C31-2ADB-4296-A8F7-E4701232C972}";
            public const string Ringtones = "{C870044B-F49E-4126-A9C3-B52A1FF411E8}";
            public const string RoamingAppData = "{3EB685DB-65F9-4CF6-A03A-E3EF65729F3D}";
            public const string RoamingTiles = "{00BCFC5A-ED94-4e48-96A1-3F6217F21990}";
            public const string SampleMusic = "{B250C668-F57D-4EE1-A63C-290EE7D1AA1F}";
            public const string SamplePictures = "{C4900540-2379-4C75-844B-64E6FAF8716B}";
            public const string SamplePlaylists = "{15CA69B3-30EE-49C1-ACE1-6B5EC372AFB5}";
            public const string SampleVideos = "{859EAD94-2E85-48AD-A71A-0969CB56A6CD}";
            public const string SavedGames = "{4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4}";
            public const string SavedSearches = "{7d1d3a04-debb-4115-95cf-2f29da2920da}";
            public const string SEARCH_CSC = "{ee32e446-31ca-4aba-814f-a5ebd2fd6d5e}";
            public const string SEARCH_MAPI = "{98ec0e18-2098-4d44-8644-66979315a281}";
            public const string SearchHome = "{190337d1-b8ca-4121-a639-6d472d16972a}";
            public const string SendTo = "{8983036C-27C0-404B-8F08-102D10DCFD74}";
            public const string SidebarDefaultParts = "{7B396E54-9EC5-4300-BE0A-2482EBAE1A26}";
            public const string SidebarParts = "{A75D362E-50FC-4fb7-AC2C-A8BEAA314493}";
            public const string StartMenu = "{625B53C3-AB48-4EC1-BA1F-A1EF4146FC19}";
            public const string Startup = "{B97D20BB-F46A-4C97-BA10-5E3608430854}";
            public const string SyncManagerFolder = "{43668BF8-C14E-49B2-97C9-747784D784B7}";
            public const string SyncResultsFolder = "{289a9a43-be44-4057-a41b-587a76d7e7f9}";
            public const string SyncSetupFolder = "{0F214138-B1D3-4a90-BBA9-27CBC0C5389A}";
            public const string System = "{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}";
            public const string SystemX86 = "{D65231B0-B2F1-4857-A4CE-A8E7C6EA7D27}";
            public const string Templates = "{A63293E8-664E-48DB-A079-DF759E0509F7}";
            public const string UserPinned = "{9E3995AB-1F9C-4F13-B827-48B24B6C7174}";
            public const string UserProfiles = "{0762D272-C50A-4BB0-A382-697DCD729B80}";
            public const string UserProgramFiles = "{5CD7AEE2-2219-4A67-B85D-6C9CE15660CB}";
            public const string UserProgramFilesCommon = "{BCBD3057-CA5C-4622-B42D-BC56DB0AE516}";
            public const string UsersFiles = "{f3ce0f7c-4901-4acc-8648-d5d44b04ef8f}";
            public const string UsersLibraries = "{A302545D-DEFF-464b-ABE8-61C8648D939B}";
            public const string UserTiles = "{008ca0b1-55b4-4c56-b8a8-4de4b299d3be}";
            public const string Videos = "{18989B1D-99B5-455B-841C-AB7C74E4DDFC}";
            public const string VideosLibrary = "{491E922F-5643-4AF4-A7EB-4E7A138D8174}";
            public const string Windows = "{F38BF404-1D43-42F2-9305-67DE0B28FC23}";
        }

        public static class Sys
        {
            //ClassID
            public const string IdCShellLink = "00021401-0000-0000-C000-000000000046";
            public const string IdCExplorerBrowser = "71f96385-ddd6-48d3-a0c1-ae06e8b055fb";
            public const string IdCShellWindows = "9BA05972-F6A8-11CF-A442-00A0C90A8F39";
            public const string IdCApplicationDocumentLists = "86bec222-30f2-47e0-9f25-60d11cd75c28";
            //ServiceID
            public const string IdSTopLevelBrowser = "4C96BE40-915C-11CF-99D3-00AA004AE837";
            //InterfaceID
            public const string IdIApplicationDocumentLists = "3c594f9f-9f30-47a1-979a-c9e83d3d0a06";
            public const string IdIObjectArray = "92CA9DCD-5622-4bba-A805-5E9F541BD8C9";
            public const string IdIPersistFile = "0000010b-0000-0000-C000-000000000046";
            public const string IdIPersist = "0000010c-0000-0000-c000-000000000046";
            public const string IdIPropertyStore = "886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99";
            public const string IdIShellItem = "43826d1e-e718-42ee-bc55-a1e261c37bfe";
            public const string IdIShellLinkW = "000214F9-0000-0000-C000-000000000046";
            public const string IdIShellFolder = "000214E6-0000-0000-C000-000000000046";
            public const string IdIShellView = "000214E3-0000-0000-C000-000000000046";
            public const string IdIShellBrowser = "000214e2-0000-0000-c000-000000000046";
            public const string IdIShellWindows = "85CB6900-4D95-11CF-960C-0080C7F4EE85";
            public const string IdIServiceProvider = "6d5140c1-7436-11ce-8034-00aa006009fa";
            public const string IdIExplorerBrowser = "dfd3b6b5-c10c-4be9-85f6-a66969f402f6";
        }

        public static class Lib
        {
            public const string USER = "user32.dll";
            public const string DWMAPI = "dwmapi.dll";
            public const string SHELL = "shell32.dll";
            public const string KERNEL = "kernel32.dll";
            public const string OLE = "ole32.dll";
        }

        #endregion


        //Windows positioning=====================================================================================
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X, Y;
        }

        [DllImport(Lib.USER, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "FindWindowW")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport(Lib.USER, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "FindWindowExW")]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lpClassName,
                                                 string lpWindowName);

        [DllImport(Lib.USER, SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport(Lib.USER, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport(Lib.USER, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MoveWindow(IntPtr hwnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport(Lib.USER, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(ref POINT lpPoint);


        //Utility User32 functions and data, needed in different places============================
        public enum WM : uint
        {
            //excerpt
            SYSCOMMAND = 0x0112,
            HOTKEY = 0x312,

            NCCREATE = 0x0081,
            NCDESTROY = 0x0082,
            NCCALCSIZE = 0x0083,
            NCHITTEST = 0x0084,
            NCPAINT = 0x0085,
            NCACTIVATE = 0x0086,
            GETDLGCODE = 0x0087,
            SYNCPAINT = 0x0088,
            NCMOUSEMOVE = 0x00A0,
            NCLBUTTONDOWN = 0x00A1,
            NCLBUTTONUP = 0x00A2,
            NCLBUTTONDBLCLK = 0x00A3,
            NCRBUTTONDOWN = 0x00A4,
            NCRBUTTONUP = 0x00A5,
            NCRBUTTONDBLCLK = 0x00A6,
            NCMBUTTONDOWN = 0x00A7,
            NCMBUTTONUP = 0x00A8,
            NCMBUTTONDBLCLK = 0x00A9,
            NCXBUTTONDOWN = 0x00AB,
            NCXBUTTONUP = 0x00AC,
            NCXBUTTONDBLCLK = 0x00AD,

            MOUSEFIRST = 0x0200,
            MOUSEMOVE = 0x0200,
            LBUTTONDOWN = 0x0201,
            LBUTTONUP = 0x0202,
            LBUTTONDBLCLK = 0x0203,
            RBUTTONDOWN = 0x0204,
            RBUTTONUP = 0x0205,
            RBUTTONDBLCLK = 0x0206,
            MBUTTONDOWN = 0x0207,
            MBUTTONUP = 0x0208,
            MBUTTONDBLCLK = 0x0209,
            MOUSEWHEEL = 0x020A,
            XBUTTONDOWN = 0x020B,
            XBUTTONUP = 0x020C,
            XBUTTONDBLCLK = 0x020D,
            MOUSEHWHEEL = 0x020E,

            DWMCOMPOSITIONCHANGED = 0x031E,
            DWMNCRENDERINGCHANGED = 0x031F,
            DWMCOLORIZATIONCOLORCHANGED = 0x0320,
            DWMWINDOWMAXIMIZEDCHANGE = 0x0321
        }

        public enum SC
        {
            SIZE = 0xF000,
            MOVE = 0xF010,
            MINIMIZE = 0xF020,
            MAXIMIZE = 0xF030,
            NEXTWINDOW = 0xF040,
            PREVWINDOW = 0xF050,
            CLOSE = 0xF060,
            VSCROLL = 0xF070,
            HSCROLL = 0xF080,
            MOUSEMENU = 0xF090,
            KEYMENU = 0xF100,
            ARRANGE = 0xF110,
            RESTORE = 0xF120,
            TASKLIST = 0xF130,
            SCREENSAVE = 0xF140,
            HOTKEY = 0xF150,
            DEFAULT = 0xF160,
            MONITORPOWER = 0xF170,
            CONTEXTHELP = 0xF180,
            SEPARATOR = 0xF00F
        }

        public enum HT
        {
            ERROR = (-2),
            TRANSPARENT = (-1),
            NOWHERE = 0,
            CLIENT = 1,
            CAPTION = 2,
            SYSMENU = 3,
            GROWBOX = 4,
            SIZE = GROWBOX,
            MENU = 5,
            HSCROLL = 6,
            VSCROLL = 7,
            MINBUTTON = 8,
            MAXBUTTON = 9,
            LEFT = 10,
            RIGHT = 11,
            TOP = 12,
            TOPLEFT = 13,
            TOPRIGHT = 14,
            BOTTOM = 15,
            BOTTOMLEFT = 16,
            BOTTOMRIGHT = 17,
            BORDER = 18,
            REDUCE = MINBUTTON,
            ZOOM = MAXBUTTON,
            SIZEFIRST = LEFT,
            SIZELAST = BOTTOMRIGHT,
            OBJECT = 19,
            CLOSE = 20,
            HELP = 21
        }

        [DllImport(Lib.USER)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport(Lib.USER)]
        public static extern IntPtr SendMessage(IntPtr hWnd, WM msg, int wParam, int lParam);

        [DllImport(Lib.USER)]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport(Lib.USER)]
        public static extern bool ShowWindow(IntPtr hWnd, SWCommands nCmdShow);


        //Aero Glass===============================================================================
        [StructLayout(LayoutKind.Sequential)]
        public class Margins
        {
            public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class DwmBlurbehind
        {
            public uint dwFlags;
            [MarshalAs(UnmanagedType.Bool)] public bool fEnable;
            public IntPtr hRegionBlur;
            [MarshalAs(UnmanagedType.Bool)] public bool fTransitionOnMaximized;
            public const uint DWM_BB_ENABLE = 1;
            public const uint DWM_BB_BLURREGION = 2;
            public const uint DWM_BB_TRANSITIONONMAXIMIZED = 4;
        }

        [DllImport(Lib.DWMAPI)]
        public static extern void DwmEnableBlurBehindWindow(IntPtr hWnd, DwmBlurbehind pBlurBehind);

        [DllImport(Lib.DWMAPI)]
        public static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, Margins pMargins);

        [DllImport(Lib.DWMAPI, PreserveSig = false)]
        public static extern bool DwmIsCompositionEnabled();


        //Getting icons============================================================================
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
        public struct ShfileinfoW
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        [Flags]
        public enum Shgfi : uint
        {
            ICON = 0x000000100, // get icon
            DISPLAYNAME = 0x000000200, // get display name
            TYPENAME = 0x000000400, // get type name
            ATTRIBUTES = 0x000000800, // get attributes
            ICONLOCATION = 0x000001000, // get icon location
            EXETYPE = 0x000002000, // return exe type
            SYSICONINDEX = 0x000004000, // get system icon index
            LINKOVERLAY = 0x000008000, // put a link overlay on icon
            SELECTED = 0x000010000, // show icon in selected state
            ATTR_SPECIFIED = 0x000020000, // get only specified attributes
            LARGEICON = 0x000000000, // get large icon
            SMALLICON = 0x000000001, // get small icon
            OPENICON = 0x000000002, // get open icon
            SHELLICONSIZE = 0x000000004, // get shell size icon
            PIDL = 0x000000008, // pszPath is a pidl
            USEFILEATTRIBUTES = 0x000000010, // use passed dwFileAttribute
            ADDOVERLAYS = 0x000000020, // apply the appropriate overlays
            OVERLAYINDEX = 0x000000040 // Get the index of the overlay in the upper 8 bits of the iIcon
        }

        public enum Csidl : uint
        {
            DESKTOP = 0x0000, // <desktop>
            INTERNET = 0x0001, // Internet Explorer (icon on desktop)
            PROGRAMS = 0x0002, // Start Menu\Programs
            CONTROLS = 0x0003, // My Computer\Control Panel
            PRINTERS = 0x0004, // My Computer\Printers
            PERSONAL = 0x0005, // My Documents
            FAVORITES = 0x0006, // <user name>\Favorites
            STARTUP = 0x0007, // Start Menu\Programs\Startup
            RECENT = 0x0008, // <user name>\Recent
            SENDTO = 0x0009, // <user name>\SendTo
            BITBUCKET = 0x000a, // <desktop>\Recycle Bin
            STARTMENU = 0x000b, // <user name>\Start Menu
            MYDOCUMENTS = PERSONAL, //  Personal was just a silly name for My Documents
            MYMUSIC = 0x000d, // "My Music" folder
            MYVIDEO = 0x000e, // "My Videos" folder
            DESKTOPDIRECTORY = 0x0010, // <user name>\Desktop
            DRIVES = 0x0011, // My Computer
            NETWORK = 0x0012, // Network Neighborhood (My Network Places)
            NETHOOD = 0x0013, // <user name>\nethood
            FONTS = 0x0014, // windows\fonts
            TEMPLATES = 0x0015,
            COMMON_STARTMENU = 0x0016, // All Users\Start Menu
            COMMON_PROGRAMS = 0x0017, // All Users\Start Menu\Programs
            COMMON_STARTUP = 0x0018, // All Users\Startup
            COMMON_DESKTOPDIRECTORY = 0x0019, // All Users\Desktop
            APPDATA = 0x001a, // <user name>\Application Data
            PRINTHOOD = 0x001b, // <user name>\PrintHood
            LOCAL_APPDATA = 0x001c, // <user name>\Local Settings\Applicaiton Data (non roaming)
            ALTSTARTUP = 0x001d, // non localized startup
            COMMON_ALTSTARTUP = 0x001e, // non localized common startup
            COMMON_FAVORITES = 0x001f,
            INTERNET_CACHE = 0x0020,
            COOKIES = 0x0021,
            HISTORY = 0x0022,
            COMMON_APPDATA = 0x0023, // All Users\Application Data
            WINDOWS = 0x0024, // GetWindowsDirectory()
            SYSTEM = 0x0025, // GetSystemDirectory()
            PROGRAM_FILES = 0x0026, // C:\Program Files
            MYPICTURES = 0x0027, // C:\Program Files\My Pictures
            PROFILE = 0x0028, // USERPROFILE
            SYSTEMX86 = 0x0029, // x86 system directory on RISC
            PROGRAM_FILESX86 = 0x002a, // x86 C:\Program Files on RISC
            PROGRAM_FILES_COMMON = 0x002b, // C:\Program Files\Common
            PROGRAM_FILES_COMMONX86 = 0x002c, // x86 Program Files\Common on RISC
            COMMON_TEMPLATES = 0x002d, // All Users\Templates
            COMMON_DOCUMENTS = 0x002e, // All Users\Documents
            COMMON_ADMINTOOLS = 0x002f, // All Users\Start Menu\Programs\Administrative Tools
            ADMINTOOLS = 0x0030, // <user name>\Start Menu\Programs\Administrative Tools
            CONNECTIONS = 0x0031, // Network and Dial-up Connections
            COMMON_MUSIC = 0x0035, // All Users\My Music
            COMMON_PICTURES = 0x0036, // All Users\My Pictures
            COMMON_VIDEO = 0x0037, // All Users\My Video
            RESOURCES = 0x0038, // Resource Direcotry
            RESOURCES_LOCALIZED = 0x0039, // Localized Resource Direcotry
            COMMON_OEM_LINKS = 0x003a, // Links to All Users OEM specific apps
            CDBURN_AREA = 0x003b, // USERPROFILE\Local Settings\Application Data\Microsoft\CD Burning
            INVALID = 0x003c, // Incorrect, used internally in Power8, reserved in API
            COMPUTERSNEARME = 0x003d, // Computers Near Me (computered from Workgroup membership)
            POWER8CLASS = 0x003e, // Power8 internal, unknown to API. Argument == name of P8 class
            POWER8JLITEM = 0x003f, // Power8 internal, unknown to API. Item is a JumpList item for parent
            FLAG_CREATE = 0x8000, // combine with CSIDL_ value to force folder creation in SHGetFolderPath()
            FLAG_DONT_VERIFY = 0x4000, // combine with CSIDL_ value to return an unverified folder path
            FLAG_DONT_UNEXPAND = 0x2000, // combine with CSIDL_ value to avoid unexpanding environment variables
            FLAG_NO_ALIAS = 0x1000, // combine with CSIDL_ value to insure non-alias versions of the pidl
            FLAG_PER_USER_INIT = 0x0800, // combine with CSIDL_ value to indicate per-user init (eg. upgrade)
            FLAG_MASK = 0xFF00, // mask for all possible flag values
        }

        [DllImport(Lib.SHELL, CharSet = CharSet.Unicode, EntryPoint = "SHGetFileInfoW")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
                                                  ref ShfileinfoW psfi, uint cbSizeFileInfo, Shgfi uFlags);

        [DllImport(Lib.SHELL, CharSet = CharSet.Unicode, EntryPoint = "SHGetFileInfoW")]
        public static extern IntPtr SHGetFileInfo(IntPtr pIdList, uint dwFileAttributes,
                                                  ref ShfileinfoW psfi, uint cbSizeFileInfo, Shgfi uFlags);

        [DllImport(Lib.USER)]
        public static extern int DestroyIcon(IntPtr hIcon);

        [DllImport(Lib.KERNEL)]
        public static extern Boolean CloseHandle(IntPtr handle);

        [DllImport(Lib.SHELL, EntryPoint = "ExtractIconW", CharSet = CharSet.Unicode)]
        public static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, uint nIconIndex);


        //Invoking specific verbs (show properties)================================================
        [StructLayout(LayoutKind.Sequential)]
        public class ShellExecuteInfo
        {
            public int cbSize;
            public SEIFlags fMask;
            public IntPtr hwnd = (IntPtr) 0;
            [MarshalAs(UnmanagedType.LPTStr)] public String lpVerb = "";
            [MarshalAs(UnmanagedType.LPTStr)] public String lpFile = "";
            [MarshalAs(UnmanagedType.LPTStr)] public String lpParameters = "";
            [MarshalAs(UnmanagedType.LPTStr)] public String lpDirectory = "";
            public SWCommands nShow;
            public IntPtr hInstApp = (IntPtr) 0;
            public IntPtr lpIDList = (IntPtr) 0;
            [MarshalAs(UnmanagedType.LPTStr)] public String lpClass = "";
            public IntPtr hkeyClass = (IntPtr) 0;
            public int dwHotKey;
            public IntPtr hIcon = (IntPtr) 0;
            public IntPtr hProcess = (IntPtr) 0;

            public ShellExecuteInfo()
            {
                cbSize = Marshal.SizeOf(this);
            }
        }

        [Flags]
        public enum SEIFlags
        {
            SEE_MASK_DEFAULT = 0x00000000,
            SEE_MASK_CLASSNAME = 0x00000001,
            SEE_MASK_CLASSKEY = 0x00000003,
            SEE_MASK_IDLIST = 0x00000004,
            SEE_MASK_INVOKEIDLIST = 0x0000000C,
            SEE_MASK_ICON = 0x00000010,
            SEE_MASK_HOTKEY = 0x00000020,
            SEE_MASK_NOCLOSEPROCESS = 0x00000040,
            SEE_MASK_CONNECTNETDRV = 0x00000080,
            SEE_MASK_NOASYNC = 0x00000100,
            [EditorBrowsable(EditorBrowsableState.Never)] SEE_MASK_FLAG_DDEWAIT = 0x00000100,
            SEE_MASK_DOENVSUBST = 0x00000200,
            SEE_MASK_FLAG_NO_UI = 0x00000400,
            SEE_MASK_UNICODE = 0x00004000,
            SEE_MASK_NO_CONSOLE = 0x00008000,
            SEE_MASK_ASYNCOK = 0x00100000,
            SEE_MASK_NOQUERYCLASSSTORE = 0x01000000,
            SEE_MASK_HMONITOR = 0x00200000,
            SEE_MASK_NOZONECHECKS = 0x00800000,
            SEE_MASK_WAITFORINPUTIDLE = 0x02000000,
            SEE_MASK_FLAG_LOG_USAGE = 0x04000000,
        }

        public enum SWCommands
        {
            HIDE = 0,
            MAXIMIZE = 3,
            MINIMIZE = 6,
            RESTORE = 9,
            SHOW = 5,
            SHOWDEFAULT = 10,
            SHOWMAXIMIZED = 3,
            SHOWMINIMIZED = 2,
            SHOWMINNOACTIVE = 7,
            SHOWNA = 8,
            SHOWNOACTIVATE = 4,
            SHOWNORMAL = 1,
        }

        [DllImport(Lib.SHELL, CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = false)]
        public static extern bool ShellExecuteEx(ShellExecuteInfo info);


        //Resolving links==========================================================================
        [Flags]
        public enum SLGP_FLAGS
        {
            SLGP_SHORTPATH = 0x1,
            SLGP_UNCPRIORITY = 0x2,
            SLGP_RAWPATH = 0x4,
            SLGP_RELATIVEPRIORITY = 0x8
        }

        [Flags]
        public enum SLR_FLAGS
        {
            /// <summary>
            /// Do not display a dialog box if the link cannot be resolved. When SLR_NO_UI is set,
            /// the high-order word of fFlags can be set to a time-out value that specifies the
            /// maximum amount of time to be spent resolving the link. The function returns if the
            /// link cannot be resolved within the time-out duration. If the high-order word is set
            /// to zero, the time-out duration will be set to the default value of 3,000 milliseconds
            /// (3 seconds). To specify a value, set the high word of fFlags to the desired time-out
            /// duration, in milliseconds.
            /// </summary>
            SLR_NO_UI = 0x1,

            /// <summary>Obsolete and no longer used</summary>
            SLR_ANY_MATCH = 0x2,

            /// <summary>If the link object has changed, update its path and list of identifiers.
            /// If SLR_UPDATE is set, you do not need to call IPersistFile::IsDirty to determine
            /// whether or not the link object has changed.</summary>
            SLR_UPDATE = 0x4,

            /// <summary>Do not update the link information</summary>
            SLR_NOUPDATE = 0x8,

            /// <summary>Do not execute the search heuristics</summary>
            SLR_NOSEARCH = 0x10,

            /// <summary>Do not use distributed link tracking</summary>
            SLR_NOTRACK = 0x20,

            /// <summary>Disable distributed link tracking. By default, distributed link tracking tracks
            /// removable media across multiple devices based on the volume name. It also uses the
            /// Universal Naming Convention (UNC) path to track remote file systems whose drive letter
            /// has changed. Setting SLR_NOLINKINFO disables both types of tracking.</summary>
            SLR_NOLINKINFO = 0x40,

            /// <summary>Call the Microsoft Windows Installer</summary>
            SLR_INVOKE_MSI = 0x80
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public long ftCreationTime;
            public long ftLastAccessTime;
            public long ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public string cAlternateFileName;
        }

        [ComImport, Guid(Sys.IdIPersist),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPersist
        {
            void GetClassID(out Guid pClassID);
        }

        [ComImport, Guid(Sys.IdIPersistFile),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPersistFile : IPersist
        {
            new void GetClassID(out Guid pClassID);
            bool IsDirty();
            void Load([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);

            void Save([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
                      [In, MarshalAs(UnmanagedType.Bool)] bool fRemember);

            void SaveCompleted([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([In, MarshalAs(UnmanagedType.LPWStr)] string ppszFileName);
        }

        /// <summary>The IShellLink interface allows Shell links to be created, modified, and resolved</summary>
        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid(Sys.IdIShellLinkW)]
        public interface IShellLink
        {
            /// <summary>Retrieves the path and file name of a Shell link object</summary>
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath,
                         out WIN32_FIND_DATAW pfd, SLGP_FLAGS fFlags);

            /// <summary>Retrieves the list of item identifiers for a Shell link object</summary>
            void GetIDList(out IntPtr ppidl);

            /// <summary>Sets the pointer to an item identifier list (PIDL) for a Shell link object.</summary>
            void SetIDList(IntPtr pidl);

            /// <summary>Retrieves the description string for a Shell link object</summary>
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);

            /// <summary>Sets the description for a Shell link object. The description can be any application-defined string</summary>
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

            /// <summary>Retrieves the name of the working directory for a Shell link object</summary>
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

            /// <summary>Sets the name of the working directory for a Shell link object</summary>
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

            /// <summary>Retrieves the command-line arguments associated with a Shell link object</summary>
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

            /// <summary>Sets the command-line arguments for a Shell link object</summary>
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

            /// <summary>Retrieves the hot key for a Shell link object</summary>
            void GetHotkey(out short pwHotkey);

            /// <summary>Sets a hot key for a Shell link object</summary>
            void SetHotkey(short wHotkey);

            /// <summary>Retrieves the show command for a Shell link object</summary>
            void GetShowCmd(out int piShowCmd);

            /// <summary>Sets the show command for a Shell link object. The show command sets the initial show state of the window.</summary>
            void SetShowCmd(int iShowCmd);

            /// <summary>Retrieves the location (path and index) of the icon for a Shell link object</summary>
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
                                 int cchIconPath, out int piIcon);

            /// <summary>Sets the location (path and index) of the icon for a Shell link object</summary>
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

            /// <summary>Sets the relative path to the Shell link object</summary>
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);

            /// <summary>Attempts to find the target of a Shell link, even if it has been moved or renamed</summary>
            void Resolve(IntPtr hwnd, SLR_FLAGS fFlags);

            /// <summary>Sets the path and file name of a Shell link object</summary>
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        /// <summary> Implements IShellLink and IPersistFile COM interfaces</summary>
        [ComImport, ClassInterface(ClassInterfaceType.None), Guid(Sys.IdCShellLink)]
        public class ShellLink{}


        //Jump lists===============================================================================
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct PKEY
        {
            public static readonly PKEY Title = new PKEY(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 2U);
            public static readonly PKEY AppUserModel_ID = new PKEY(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5U);

            public static readonly PKEY AppUserModel_IsDestListSeparator =
                new PKEY(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 6U);

            public static readonly PKEY AppUserModel_RelaunchCommand =
                new PKEY(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 2U);

            public static readonly PKEY AppUserModel_RelaunchDisplayNameResource =
                new PKEY(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 4U);

            public static readonly PKEY AppUserModel_RelaunchIconResource =
                new PKEY(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 3U);

            private readonly Guid _fmtid;
            private readonly uint _pid;

            private PKEY(Guid fmtid, uint pid)
            {
                _fmtid = fmtid;
                _pid = pid;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public class PROPVARIANT : IDisposable
        {
            [FieldOffset(0)] public ushort vt;
            [FieldOffset(8)] public IntPtr pointerVal;
            [FieldOffset(8)] public byte byteVal;
            [FieldOffset(8)] public long longVal;
            [FieldOffset(8)] public short boolVal;

            public VarEnum VarType
            {
                get { return (VarEnum) vt; }
            }

            ~PROPVARIANT()
            {
                Dispose(false);
            }

            public string GetValue()
            {
                return vt == 31 ? Marshal.PtrToStringUni(pointerVal) : null;
            }

            public void SetValue(bool f)
            {
                Clear();
                vt = 11;
                boolVal = f ? (short) -1 : (short) 0;
            }

            public void SetValue(string val)
            {
                Clear();
                vt = 31;
                pointerVal = Marshal.StringToCoTaskMemUni(val);
            }

            public void Clear()
            {
                PropVariantClear(this);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

// ReSharper disable UnusedParameter.Local
            private void Dispose(bool disposing)
            {
                Clear();
            }
// ReSharper restore UnusedParameter.Local
        }

        [DllImport(Lib.OLE)]
        internal static extern int PropVariantClear(PROPVARIANT pvar);

        [ComImport, Guid(Sys.IdIPropertyStore), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPropertyStore
        {
            uint GetCount();
            PKEY GetAt(uint iProp);
            void GetValue([In] ref PKEY pkey, [In, Out] PROPVARIANT pv);
            void SetValue([In] ref PKEY pkey, PROPVARIANT pv);
            void Commit();
        }

        public enum GETPROPERTYSTOREFLAGS
        {
            GPS_DEFAULT = 0,
            GPS_HANDLERPROPERTIESONLY = 0x1,
            GPS_READWRITE = 0x2,
            GPS_TEMPORARY = 0x4,
            GPS_FASTPROPERTIESONLY = 0x8,
            GPS_OPENSLOWITEM = 0x10,
            GPS_DELAYCREATION = 0x20,
            GPS_BESTEFFORT = 0x40,
            GPS_NO_OPLOCK = 0x80,
            GPS_PREFERQUERYPROPERTIES = 0x100,
            GPS_MASK_VALID = 0x1ff
        }

        [DllImport(Lib.SHELL, CharSet = CharSet.Unicode)]
        public static extern uint SHGetPropertyStoreFromParsingName(
            string pszPath,
            IntPtr zeroWorks,
            GETPROPERTYSTOREFLAGS flags,
            ref Guid iIdPropStore,
            [Out] out IPropertyStore propertyStore);

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid(Sys.IdIObjectArray)]
        [ComImport]
        internal interface IObjectArray
        {
            uint GetCount();

            [return: MarshalAs(UnmanagedType.IUnknown)]
            object GetAt([In] uint uiIndex, [In] ref Guid riid);
        }

        [Flags]
        public enum SICHINT : uint
        {
            DISPLAY = 0U,
            ALLFIELDS = 2147483648U,
            CANONICAL = 268435456U,
            TEST_FILESYSPATH_IF_NOT_EQUAL = 536870912U,
        }

        [Guid(Sys.IdIShellItem)]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        public interface IShellItem
        {
            [return: MarshalAs(UnmanagedType.Interface)]
            object BindToHandler(IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid);

            IShellItem GetParent();

            [return: MarshalAs(UnmanagedType.LPWStr)]
            string GetDisplayName(SIGDN sigdnName);

            uint GetAttributes(SFGAO sfgaoMask);

            int Compare(IShellItem psi, SICHINT hint);
        }

        internal enum ADLT
        {
            RECENT,
            FREQUENT,
        }

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid(Sys.IdIApplicationDocumentLists)]
        [ComImport]
        internal interface IApplicationDocumentLists
        {
            void SetAppID([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);

            [return: MarshalAs(UnmanagedType.IUnknown)]
            object GetList(ADLT listtype, uint cItemsDesired, [In] ref Guid riid);
        }

        [ComImport, ClassInterface(ClassInterfaceType.None)]
        [Guid(Sys.IdCApplicationDocumentLists)]
        public class ApplicationDocumentLists
        {
        }

        [DllImport(Lib.SHELL)]
        public static extern uint SHGetIDListFromObject([MarshalAs(UnmanagedType.IUnknown)] object iUnknown,
                                                        out IntPtr ppidl);


        //Loading native resources=================================================================
        [Flags]
        public enum LLF : uint
        {
            AS_REGULAR_LOAD_LIBRARY = 0,
            DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
            LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
            LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008,
            LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
            LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
            LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
            LOAD_LIBRARY_REQUIRE_SIGNED_TARGET = 0x00000080
        }

        [DllImport(Lib.KERNEL, CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryExW")]
        public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName, IntPtr hFile,
                                                LLF dwFlags);

        [DllImport(Lib.KERNEL)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport(Lib.KERNEL)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        [DllImport(Lib.USER)]
        public static extern int LoadString(IntPtr hInstance, uint resourceID, StringBuilder lpBuffer, int nBufferMax);

        [DllImport(Lib.USER, CharSet = CharSet.Unicode, EntryPoint = "LoadIconW")]
        public static extern IntPtr LoadIcon(IntPtr hInstance, string lpIconName);

        [DllImport(Lib.USER, CharSet = CharSet.Unicode, EntryPoint = "LoadIconW")]
        public static extern IntPtr LoadIcon(IntPtr hInstance, uint zeroHiWordIdLoWord);


        //HotKey===================================================================================
        [DllImport(Lib.USER)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, fsModifiers fsModifiers,
                                                 System.Windows.Forms.Keys vk);

        public enum fsModifiers
        {
            MOD_NULL = 0,
            MOD_ALT = 1,
            MOD_CONTROL = 2,
            MOD_SHIFT = 4,
            MOD_WIN = 8,
        }


        //Undocumented shell API===================================================================
        [Flags]
        public enum RFF
        {
            /// <summary>
            /// No changes to run dialog
            /// </summary>
            NORMAL = 0,

            /// <summary>
            /// Removes the browse button.
            /// </summary>
            NOBROWSE = 1,

            /// <summary>
            /// No MRU item selected.
            /// </summary>
            NODEFAULT = 2,

            /// <summary>
            /// Calculates the working directory from the file name.
            /// </summary>
            CALCDIRECTORY = 4,

            /// <summary>
            /// Removes the edit box label.
            /// </summary>
            NOLABEL = 8,

            /// <summary>
            /// Removes the Separate Memory Space check box (Windows NT only).
            /// </summary>
            NOSEPARATEMEM = 16 //14 originally at http://www.swissdelphicenter.ch/en/showcode.php?id=1181
        }

        [DllImport(Lib.SHELL, EntryPoint = "#61")]
        public static extern
        void SHRunDialog(IntPtr hWnd, IntPtr hIcon, string sDir, string szTitle, string szPrompt, RFF uFlags);


        //Shell namespaces, known folders, etc=====================================================
        public enum KFF : uint
        {
            NO_APPCONTAINER_REDIRECTION = 0x00010000,
            CREATE = 0x00008000,
            DONT_VERIFY = 0x00004000,
            DONT_UNEXPAND = 0x00002000,
            NO_ALIAS = 0x00001000,
            INIT = 0x00000800,
            DEFAULT_PATH = 0x00000400,
            NOT_PARENT_RELATIVE = 0x00000200,
            SIMPLE_IDLIST = 0x00000100,
            ALIAS_ONLY = 0x80000000,
            NORMAL = 0
        }

        [DllImport(Lib.SHELL, CharSet = CharSet.Unicode)]
        public static extern
        void SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, KFF dwFlags, IntPtr hToken,
                                  out IntPtr ppwszPath);

        [DllImport(Lib.SHELL, CharSet = CharSet.Unicode)]
        public static extern
        uint SHParseDisplayName(string pszName, IntPtr zero, [Out] out IntPtr ppidl,
                                SFGAO sfgaoIn, [Out] out SFGAO psfgaoOut);

        [DllImport(Lib.SHELL, SetLastError = true)]
        public static extern int SHGetSpecialFolderLocation(IntPtr hwndOwner, Csidl nFolder, ref IntPtr ppidl);

        [DllImport(Lib.SHELL, SetLastError = true, 
        EntryPoint = "SHGetSpecialFolderPathW", CharSet = CharSet.Unicode)]
        public static extern 
        uint SHGetSpecialFolderPath(IntPtr hwndOwner, StringBuilder buffer, Csidl nFolder, bool fCreate);

        public enum SIGDN : uint
        {
            NORMALDISPLAY = 0x00000000,
            PARENTRELATIVEPARSING = 0x80018001,
            DESKTOPABSOLUTEPARSING = 0x80028000,
            PARENTRELATIVEEDITING = 0x80031001,
            DESKTOPABSOLUTEEDITING = 0x8004c000,
            FILESYSPATH = 0x80058000,
            URL = 0x80068000,
            PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
            PARENTRELATIVE = 0x80080001
        }

        [DllImport(Lib.SHELL, CharSet = CharSet.Unicode)]
        public static extern uint SHGetNameFromIDList(IntPtr pidl, SIGDN sigdnName, ref IntPtr ppszName);

        [DllImport(Lib.SHELL, CharSet = CharSet.Unicode,
        EntryPoint = "SHGetPathFromIDListW", PreserveSig = true)]
        public static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder ppszName);

        [Flags]
        public enum SFGAO : uint
        {
            NULL = 0x00000000,
            CANCOPY = 0x00000001, // Objects can be copied    (0x1)
            CANMOVE = 0x00000002, // Objects can be moved     (0x2)
            CANLINK = 0x00000004, // Objects can be linked    (0x4)
            STORAGE = 0x00000008, // supports BindToObject(IdIStorage)
            CANRENAME = 0x00000010, // Objects can be renamed
            CANDELETE = 0x00000020, // Objects can be deleted
            HASPROPSHEET = 0x00000040, // Objects have property sheets
            DROPTARGET = 0x00000100, // Objects are drop target
            CAPABILITYMASK = 0x00000177,
            SYSTEM = 0x00001000, // System object
            ENCRYPTED = 0x00002000, // Object is encrypted (use alt color)
            ISSLOW = 0x00004000, // 'Slow' object
            GHOSTED = 0x00008000, // Ghosted icon
            LINK = 0x00010000, // Shortcut (link)
            SHARE = 0x00020000, // Shared
            READONLY = 0x00040000, // Read-only
            HIDDEN = 0x00080000, // Hidden object
            DISPLAYATTRMASK = 0x000FC000,
            FILESYSANCESTOR = 0x10000000, // May contain children with SFGAO_FILESYSTEM
            FOLDER = 0x20000000, // Support BindToObject(IdIShellFolder)
            FILESYSTEM = 0x40000000, // Is a win32 file system object (file/folder/root)
            HASSUBFOLDER = 0x80000000, // May contain children with SFGAO_FOLDER (may be slow)
            CONTENTSMASK = 0x80000000,
            VALIDATE = 0x01000000, // Invalidate cached information (may be slow)
            REMOVABLE = 0x02000000, // Is this removeable media?
            COMPRESSED = 0x04000000, // Object is compressed (use alt color)
            BROWSABLE = 0x08000000, // Supports IShellFolder, but only implements CreateViewObject() (non-folder view)
            NONENUMERATED = 0x00100000, // Is a non-enumerated object (should be hidden)
            NEWCONTENT = 0x00200000, // Should show bold in explorer tree
            CANMONIKER = 0x00400000, // Obsolete
            HASSTORAGE = 0x00400000, // Obsolete
            STREAM = 0x00400000, // Supports BindToObject(IdIStream)
            STORAGEANCESTOR = 0x00800000, // May contain children with SFGAO_STORAGE or SFGAO_STREAM
            STORAGECAPMASK = 0x70C50008, // For determining storage capabilities, ie for open/save semantics
            PKEYSFGAOMASK = 0x81044000 // Attributes that are masked out for PKEY_SFGAOFlags because they are considered to
            //cause slow calculations or lack context (SFGAO_VALIDATE | SFGAO_ISSLOW | SFGAO_HASSUBFOLDER and others)
        }

        [Flags]
        public enum SHCONTF
        {
            SHCONTF_CHECKING_FOR_CHILDREN = 0x10,
            SHCONTF_FOLDERS = 0x20,
            SHCONTF_NONFOLDERS = 0x40,
            SHCONTF_INCLUDEHIDDEN = 0x80,
            SHCONTF_INIT_ON_FIRST_NEXT = 0x100,
            SHCONTF_NETPRINTERSRCH = 0x200,
            SHCONTF_SHAREABLE = 0x400,
            SHCONTF_STORAGE = 0x800,
            SHCONTF_NAVIGATION_ENUM = 0x1000,
            SHCONTF_FASTITEMS = 0x2000,
            SHCONTF_FLATLIST = 0x4000,
            SHCONTF_ENABLE_ASYNC = 0x8000,
            SHCONTF_INCLUDESUPERHIDDEN = 0x10000
        }

        [Flags]
        public enum SHGNO
        {
            NORMAL = 0x0000,
            INFOLDER = 0x0001,
            FOREDITING = 0x1000,
            FORADDRESSBAR = 0x4000,
            FORPARSING = 0x8000
        }

        [DllImport(Lib.SHELL, CharSet=CharSet.Unicode)]
        public static extern
        int SHGetDesktopFolder(out IShellFolder ppShFldr);

        [ComImport, Guid(Sys.IdIShellFolder)]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellFolder
        {
            /// <summary>
            /// Translates a file object's or folder's display name into an item identifier list.
            /// </summary>
            /// <returns>Error code, if any</returns>
            [PreserveSig]
            int ParseDisplayName(
                IntPtr hwnd,
                IntPtr pbc,
                [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName,
                ref uint pchEaten,
                out IntPtr ppidl,
                ref SFGAO pdwAttributes);

            /// <summary>
            /// Allows a client to determine the contents of a folder by creating an item
            /// identifier enumeration object and returning its IEnumIDList interface.
            /// </summary>
            /// <returns>Error code, if any</returns>
            [PreserveSig]
            int EnumObjects(IntPtr hwnd, SHCONTF grfFlags, out IntPtr enumIDList);

            /// <summary>
            /// Retrieves an IShellFolder object for a subfolder.
            /// </summary>
            /// <returns>Error code, if any</returns>
            [PreserveSig]
            int BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);

            /// <summary>
            /// Requests a pointer to an object's storage interface. 
            /// </summary>
            /// <returns>Error code, if any</returns>
            [PreserveSig]
            int BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);

            /// <summary>
            /// Determines the relative order of two file objects or folders, given their item identifier lists.
            /// </summary>
            /// <returns>If this method is successful, the CODE field of the HRESULT contains one of the following
            /// values (the code can be retrived using the helper function GetHResultCode): 
            /// <ul>
            /// <li>negative return value indicates that the first item should precedethe second (pidl1 &lt; pidl2).</li>
            /// <li>positive return value indicates that the first item should follow the second (pidl1 &gt; pidl2).</li>
            /// <li>return value of zero indicates that the two items are the same (pidl1 = pidl2).</li>
            /// </ul></returns> 
            [PreserveSig]
            int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);

            /// <summary>
            /// Requests an object that can be used to obtain information from or interact with a folder object.
            /// </summary>
            /// <returns>Error code, if any</returns>
            [PreserveSig]
            int CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv);

            /// <summary>
            /// Retrieves the attributes of one or more file objects or subfolders.
            /// </summary>
            /// <returns> error code, if any</returns>
            [PreserveSig]
            int GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, ref SFGAO rgfInOut);

            /// <summary>
            /// Retrieves an OLE interface that can be used to carry out actions on the specified file objects or folders.
            /// </summary>
            /// <returns>error code, if any</returns>
            [PreserveSig]
            int GetUIObjectOf(
                IntPtr hwndOwner,
                uint cidl,
                [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
                ref Guid riid,
                IntPtr rgfReserved,
                out IntPtr ppv);

            /// <summary>
            /// Retrieves the display name for the specified file object or subfolder. 
            /// </summary>
            /// <returns>Return value: error code, if any</returns>
            [PreserveSig]
            int GetDisplayNameOf(IntPtr pidl, SHGNO uFlags, IntPtr lpName);

            /// <summary>
            /// Sets the display name of a file object or subfolder, changing the item identifier in the process.
            /// </summary>
            /// <returns>error code, if any</returns>
            [PreserveSig]
            int SetNameOf(
                IntPtr hwnd,
                IntPtr pidl,
                [MarshalAs(UnmanagedType.LPWStr)] string pszName,
                SHGNO uFlags,
                out IntPtr ppidlOut);
        }

        public enum SVUIA : uint
        {
            DEACTIVATE = 0,
            ACTIVATE_NOFOCUS = 1,
            ACTIVATE_FOCUS = 2,
            INPLACEACTIVATE = 3
        }

        public enum SVSIF : uint
        {
            SVSI_DESELECT = 0,
            SVSI_SELECT = 0x1,
            SVSI_EDIT = 0x3,
            SVSI_DESELECTOTHERS = 0x4,
            SVSI_ENSUREVISIBLE = 0x8,
            SVSI_FOCUSED = 0x10,
            SVSI_TRANSLATEPT = 0x20,
            SVSI_SELECTIONMARK = 0x40,
            SVSI_POSITIONITEM = 0x80,
            SVSI_CHECK = 0x100,
            SVSI_CHECK2 = 0x200,
            SVSI_KEYBOARDSELECT = 0x401,
            SVSI_NOTAKEFOCUS = 0x40000000
        }

        public enum FOLDERVIEWMODE
        {
            FVM_AUTO = -1,
            FVM_FIRST = 1,
            FVM_ICON = 1,
            FVM_SMALLICON = 2,
            FVM_LIST = 3,
            FVM_DETAILS = 4,
            FVM_THUMBNAIL = 5,
            FVM_TILE = 6,
            FVM_THUMBSTRIP = 7,
            FVM_CONTENT = 8,
            FVM_LAST = 8
        }

        public enum FOLDERFLAGS : uint
        {
            FWF_NONE = 0x00000000,
            FWF_AUTOARRANGE = 0x00000001,
            FWF_ABBREVIATEDNAMES = 0x00000002,
            FWF_SNAPTOGRID = 0x00000004,
            FWF_OWNERDATA = 0x00000008,
            FWF_BESTFITWINDOW = 0x00000010,
            FWF_DESKTOP = 0x00000020,
            FWF_SINGLESEL = 0x00000040,
            FWF_NOSUBFOLDERS = 0x00000080,
            FWF_TRANSPARENT = 0x00000100,
            FWF_NOCLIENTEDGE = 0x00000200,
            FWF_NOSCROLL = 0x00000400,
            FWF_ALIGNLEFT = 0x00000800,
            FWF_NOICONS = 0x00001000,
            FWF_SHOWSELALWAYS = 0x00002000,
            FWF_NOVISIBLE = 0x00004000,
            FWF_SINGLECLICKACTIVATE = 0x00008000,
            FWF_NOWEBVIEW = 0x00010000,
            FWF_HIDEFILENAMES = 0x00020000,
            FWF_CHECKSELECT = 0x00040000,
            FWF_NOENUMREFRESH = 0x00080000,
            FWF_NOGROUPING = 0x00100000,
            FWF_FULLROWSELECT = 0x00200000,
            FWF_NOFILTERS = 0x00400000,
            FWF_NOCOLUMNHEADER = 0x00800000,
            FWF_NOHEADERINALLVIEWS = 0x01000000,
            FWF_EXTENDEDTILES = 0x02000000,
            FWF_TRICHECKSELECT = 0x04000000,
            FWF_AUTOCHECKSELECT = 0x08000000,
            FWF_NOBROWSERVIEWSTATE = 0x10000000,
            FWF_SUBSETGROUPS = 0x20000000,
            FWF_USESEARCHFOLDER = 0x40000000,
            FWF_ALLOWRTLREADING = 0x80000000
        }

        public struct FOLDERSETTINGS
        {
            public FOLDERVIEWMODE viewMode;
            public FOLDERFLAGS vFlags;
        }

        [ComImport, Guid(Sys.IdIShellView)]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellView
        {//IShellView : public IOleWindow
            void TranslateAccelerator([In] MSG pmsg);
            void EnableModeless([In] bool fEnable);
            void UIActivate([In] SVUIA uState);
            void Refresh();
            [PreserveSig]
            int CreateViewWindow(
                [In] IShellView psvPrevious,
                [In] ref FOLDERSETTINGS pfs,
                [In] IShellBrowser psb,
                [In] ref RECT prcView,
                [Out] out IntPtr phWnd);
            void DestroyViewWindow();
            void GetCurrentInfo([Out] out FOLDERSETTINGS pfs);
            void AddPropertySheetPages(
                [In] uint dwReserved,
                [In, MarshalAs(UnmanagedType.FunctionPtr)] IntPtr pfn,
                [In] IntPtr lparam);
            void SaveViewState();
            void SelectItem([In] IntPtr pidlItem, [In] SVSIF uFlags);
            void GetItemObject([In] uint uItem, [In] ref Guid riid, [Out] out IntPtr ppv);
        };

        [ComImport, Guid(Sys.IdIShellBrowser)]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellBrowser
        {
            [PreserveSig]
            int GetWindow(out IntPtr hwnd);
            [PreserveSig]
            int ContextSensitiveHelp(int fEnterMode);
            [PreserveSig]
            int InsertMenusSB(IntPtr hmenuShared, IntPtr lpMenuWidths);
            [PreserveSig]
            int SetMenuSB(IntPtr hmenuShared, IntPtr holemenuRes, IntPtr hwndActiveObject);
            [PreserveSig]
            int RemoveMenusSB(IntPtr hmenuShared);
            [PreserveSig]
            int SetStatusTextSB(IntPtr pszStatusText);
            [PreserveSig]
            int EnableModelessSB(bool fEnable);
            [PreserveSig]
            int TranslateAcceleratorSB(IntPtr pmsg, short wID);
            [PreserveSig]
            int BrowseObject(IntPtr pidl, SBSP wFlags);
            [PreserveSig]
            int GetViewStateStream(uint grfMode, IntPtr ppStrm);
            [PreserveSig]
            int GetControlWindow(uint id, out IntPtr phwnd);
            [PreserveSig]
            int SendControlMsg(uint id, uint uMsg, uint wParam, uint lParam, IntPtr pret);
            [PreserveSig]
            int QueryActiveShellView([MarshalAs(UnmanagedType.Interface)] ref IShellView ppshv);
            [PreserveSig]
            int OnViewWindowActive([MarshalAs(UnmanagedType.Interface)] IShellView pshv);
            [PreserveSig]
            int SetToolbarItems(IntPtr lpButtons, uint nButtons, uint uFlags);
        }

        [ComImport, Guid(Sys.IdIServiceProvider)]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IServiceProvider
        {
            [PreserveSig]
            int QueryService(ref Guid guidService, 
                             ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellBrowser ppvObject);
        }

        public enum SWC
        {
            EXPLORER = 0x0,
            BROWSER = 0x00000001,
            THIRDPARTY = 0x00000002,
            CALLBACK = 0x00000004,
            DESKTOP = 0x00000008
        }

        public enum SWFO
        {
            NEEDDISPATCH = 0x00000001,
            INCLUDEPENDING = 0x00000002,
            COOKIEPASSED = 0x00000004
        }

        [ComImport, Guid(Sys.IdIShellWindows)]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        public interface IShellWindows
        {
            int Count { get; }
            object Item([In] int index);
            IntPtr _NewEnum();
            int Register([In] IntPtr pid, [In] IntPtr hwnd, [In] int swClass);
            int RegisterPending(/*[In] int lThreadId ????,*/ [In] ref IntPtr pidl, [In] IntPtr nullHere, [In] SWC swClass);
            void Revoke([In] int lCookie);
            void OnNavigate([In] int lCookie, [In] IntPtr pvarLoc);
            void OnActivated([In] int lCookie, [In] bool fActive);
            IntPtr FindWindowSW([In] ref IntPtr pidl, [In] IntPtr nullHere, [In] SWC swClass, [Out] out IntPtr phwnd,
                              [In] SWFO swfwOptions);
            IntPtr FindWindowSW([In] int cookie, [In] IntPtr nullHere, [In] SWC swClass, [Out] out IntPtr phwnd,
                              [In] SWFO swfwOptions);
            IntPtr OnCreated([In] int lCookie);
            [Obsolete("Returns true always")]
            void ProcessAttachDetach([In] bool fAttach);
        }

        [ComImport, ClassInterface(ClassInterfaceType.AutoDispatch)]
        [Guid(Sys.IdCShellWindows)]
        public class ShellWindows {}

        [Flags]
        public enum SBSP : uint
        {
            DEFBROWSER = 0x0000,
            SAMEBROWSER = 0x0001,
            NEWBROWSER = 0x0002,
            DEFMODE = 0x0000,
            OPENMODE = 0x0010,
            EXPLOREMODE = 0x0020,
            HELPMODE = 0x0040,
            NOTRANSFERHIST = 0x0080,
            ABSOLUTE = 0x0000,
            RELATIVE = 0x1000,
            PARENT = 0x2000,
            NAVIGATEBACK = 0x4000,
            NAVIGATEFORWARD = 0x8000,
            ALLOW_AUTONAVIGATE = 0x00010000,
            KEEPSAMETEMPLATE = 0x00020000,
            KEEPWORDWHEELTEXT = 0x00040000,
            ACTIVATE_NOFOCUS = 0x00080000,
            CREATENOHISTORY = 0x00100000,
            PLAYNOSOUND = 0x00200000,
            CALLERUNTRUSTED = 0x00800000,
            TRUSTFIRSTDOWNLOAD = 0x01000000,
            UNTRUSTEDFORDOWNLOAD = 0x02000000,
            NOAUTOSELECT = 0x04000000,
            WRITENOHISTORY = 0x08000000,
            TRUSTEDFORACTIVEX = 0x10000000,
            FEEDNAVIGATION = 0x20000000,
            REDIRECT = 0x40000000,
            INITIATEDBYHLINKFRAME = 0x80000000,
        }

        [ComImport, ClassInterface(ClassInterfaceType.None)]
        [Guid(Sys.IdCExplorerBrowser)]
        public class ExplorerBrowser {}

        [ComImport, Guid(Sys.IdIExplorerBrowser)]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IExplorerBrowser
        {//todo: create commented structures and ensure parameters passed well to describe interface better
            void Initialize([In] IntPtr hwndParent, [In] ref RECT prc, [In] ref FOLDERSETTINGS pfs);
            void Destroy();
            void SetRect(/*[In] [Out] ref HDWP*/ IntPtr phdwp, [In] RECT rcBrowser);
            void SetPropertyBag([MarshalAs(UnmanagedType.LPWStr)] string pszPropertyBag);
            void SetEmptyText([MarshalAs(UnmanagedType.LPWStr)] string pszEmptyText);
            void SetFolderSettings([In] ref FOLDERSETTINGS pfs);
            void Advise([In] /*IExplorerBrowserEvents*/ IntPtr psbe, [Out] out uint pdwCookie);
            void Unadvise([In] uint dwCookie);
            void SetOptions([In] /*EXPLORER_BROWSER_OPTIONS*/ IntPtr dwFlag);
            void GetOptions([In][Out] ref /*EXPLORER_BROWSER_OPTIONS*/ IntPtr pdwFlag);
            void BrowseToIDList([In] IntPtr pidl, [In] SBSP uFlags);
            void BrowseToObject([In] IntPtr punk, [In] SBSP uFlags);
            void FillFromObject([In] IntPtr punk, [In] /*EXPLORER_BROWSER_FILL_FLAGS*/ IntPtr dwFlags);
            void RemoveAll();
            void GetCurrentView([In] ref Guid riid, [Out] out IntPtr ppv);
        }


        //CPLs ====================================================================================
        public delegate int CplAppletProc (IntPtr hwndCpl, CplMsg msg, IntPtr lParam1, IntPtr lParam2);

        public enum CplMsg : uint
        {
            /// <summary>
            /// This message is sent to indicate CPlApplet() was found. 
            /// lParam1 and lParam2 are not defined. 
            /// Return TRUE or FALSE indicating whether the control panel should proceed.
            /// </summary>
            INIT = 1,

            /// <summary>
            /// This message is sent to determine the number of applets to be displayed.
            /// lParam1 and lParam2 are not defined. 
            /// Return the number of applets you wish to display in the control panel window. 
            /// </summary>
            GETCOUNT = 2,

            /// <summary>
            /// This message is sent for information about each applet. 
            /// The return value is ignored. 
            /// lParam1 is the applet number to register, a value from 0 to 
            /// (CPL_GETCOUNT - 1).  lParam2 is a pointer to a CPLINFO structure. 
            /// Fill in CPLINFO's idIcon, idName, idInfo and lData fields with 
            /// the resource id for an icon to display, name and description string ids, 
            /// and a long data item associated with applet #lParam1.  This information 
            /// may be cached by the caller at runtime and/or across sessions. 
            /// To prevent caching, see CPL_DYNAMIC_RES, above.  If the icon, name, and description
            /// are not dynamic then CPL_DYNAMIC_RES should not be used and the CPL_NEWINQURE message
            /// should be ignored 
            /// </summary>
            INQUIRE = 3,

            /// <summary>
            /// Not used
            /// </summary>
            SELECT = 4,

            /// <summary>
            /// This message is sent when the applet's icon has been double-clicked.
            /// lParam1 is the applet number which was selected. 
            /// lParam2 is the applet's lData value. 
            /// This message should initiate the applet's dialog box. 
            /// </summary>
            DBLCLK = 5,

            /// <summary>
            /// This message is sent for each applet when the control panel is exiting.
            /// lParam1 is the applet number.  lParam2 is the applet's lData value. 
            /// Do applet specific cleaning up here. 
            /// </summary>
            STOP = 6,

            /// <summary>
            /// This message is sent just before the control panel calls FreeLibrary. 
            /// lParam1 and lParam2 are not defined. Do non-applet specific cleaning up here. 
            /// </summary>
            EXIT = 7,

            /// <summary>
            /// Same as CPL_INQUIRE execpt lParam2 is a pointer to a NEWCPLINFO struct. 
            /// The return value is ignored. 
            /// A CPL should NOT respond to the CPL_NEWINQURE message unless CPL_DYNAMIC_RES 
            /// is used in CPL_INQUIRE.  CPLs which respond to CPL_NEWINQUIRE cannot be cached 
            /// and slow the loading of the Control Panel window. 
            /// </summary>
            NEWINQUIRE = 8,

            /// <summary>
            /// <see cref="STARTWPARMSW"/>
            /// </summary>
            STARTWPARMSA = 9,

            /// <summary>
            /// This message parallels CPL_DBLCLK in that the applet should initiate
            /// its dialog box.  Where it differs is that this invocation is coming
            /// out of RUNDLL, and there may be some extra directions for execution.
            /// lParam1: the applet number.
            /// lParam2: an LPSTR to any extra directions that might exist.
            /// returns: TRUE if the message was handled; FALSE if not.
            /// </summary>
            STARTWPARMSW = 10,

            /// <summary>
            /// This message is internal to the Control Panel and MAIN applets.
            /// It is only sent when an applet is invoked from the command line
            /// during system installation.                                    
            /// </summary>
            SETUP = 200
        }

        [StructLayout(LayoutKind.Sequential)]
        public class CplInfo
        {
            public uint idIcon;     /* icon resource id, provided by CPlApplet() */
            public uint idName;     /* display name string resource id, provided by CPlApplet() */
            public uint idInfo;     /* description/tooltip/status bar string resource id, provided by CPlApplet() */
            public IntPtr lData;    /* user defined data */
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        public class NewCplInfoW
        {
            public uint dwSize;    /* size, in bytes, of the structure */
            public uint dwFlags;
            public uint dwHelpContext; /* help context to use */
            public IntPtr lData;   /* user defined data */
            public IntPtr hIcon;   /* icon to use, this is owned by the Control Panel window (may be deleted) */
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] 
            public string szName;  /* display name */
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] 
            public string szInfo;  /* description/tooltip/status bar string */
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] 
            public string szHelpFile; /* path to help file to use */
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public class NewCplInfoA
        {
            public uint dwSize;    /* size, in bytes, of the structure */
            public uint dwFlags;
            public uint dwHelpContext; /* help context to use */
            public IntPtr lData;   /* user defined data */
            public IntPtr hIcon;   /* icon to use, this is owned by the Control Panel window (may be deleted) */
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szName;  /* display name */
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfo;  /* description/tooltip/status bar string */
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szHelpFile; /* path to help file to use */
        }

// ReSharper restore InconsistentNaming
    }
}