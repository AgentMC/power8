using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace Power8.Views
{
    public class DisposableWindow : Window, IComponent
    {
        ~DisposableWindow()
        {
            Dispose();
        }

        private bool _disposing;
        public void Dispose()
        {
#if DEBUG
            Debug.WriteLine("Dispose called for " + GetType().FullName);
#endif
            lock (this)
            {
                if (_disposing)
                    return;
                _disposing = true;
            }
            Util.Send(() =>
                          {
                              if(IsVisible)
                                  Close();
                              var handler = Disposed;
                              if (handler != null)
                                  handler(this, null);
                          });
        }
        public event EventHandler Disposed;

        public ISite Site { get; set; }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Dispose();
        }
    }
}
