using System;
using System.ComponentModel;
using System.Windows;
using Power8.Helpers;

namespace Power8.Views
{
    /// <summary>
    /// Use this class to create a singleton window to be used with 
    /// <code>Util.InstantiateClass()</code>. Except inheritance, you need explicit 
    /// parameterless constructor with call to <code>InitializeComponent()</code>
    /// to be used by VS designer.
    /// </summary>
    public class DisposableWindow : Window, IComponent
    {
        /// <summary>
        /// The destructor will fire the disposition event, and thus will remove
        /// unusable <code>this</code> from the type cache of 
        /// <code>InstantiateClass()</code>
        /// </summary>
        ~DisposableWindow()
        {
            Dispose();
        }

        private bool _disposing;
        /// <summary>
        /// Call this to close the DisposableWindow and stop usage of it.
        /// </summary>
        public void Dispose()
        {
            Log.Raw("called", GetType().FullName);
            lock (this)
            {
                if (_disposing)
                    return;
                _disposing = true;
            }
            Util.Send(() => //The Dispose() might be called from background
                          {
                              if(IsVisible)
                                  Close();
                              var handler = Disposed;
                              if (handler != null)
                                  handler(this, null);
                          });
        }
        /// <summary>
        /// Occurs when the DisposableWindow is closed or in other way finalized
        /// </summary>
        public event EventHandler Disposed;

        /// <summary>
        /// Do not use.
        /// </summary>
        public ISite Site { get; set; }

        /// <summary>
        /// When override, always use base call. The disposable window
        /// always disposes itself on closing.
        /// </summary>
        /// <param name="e">EventArgs instance of any kind</param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Dispose();
        }
    }
}
