using System;
using System.Threading;
using System.Windows.Interop;

namespace Power8
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        
        public App()
        {
            Util.MainDisp = Dispatcher;
            new Thread(PowerItemTree.InitTree){Name = "InitTree"}.Start();
            ComponentDispatcher.ThreadFilterMessage += WndProc;
        }

        private void WndProc(ref MSG msg, ref bool handled)
        {
            if (msg.message == (int)API.WM.DWMCOMPOSITIONCHANGED)
            {
                OnDwmCompositionChanged();
                handled = true;
                return;
            }
            handled = false;
        }

        public event EventHandler DwmCompositionChanged ;

        protected virtual void OnDwmCompositionChanged()
        {
            var handler = DwmCompositionChanged;
            if (handler != null) handler(this, null);
        }
    }
}
