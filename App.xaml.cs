using System.Threading;
using System.Windows;
using System;

namespace Power8
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        
        public App()
        {
            PowerItemTree.MainDisp = Dispatcher;
            new Thread(PowerItemTree.InitTree){Name = "InitTree"}.Start();
        }
    }
}
