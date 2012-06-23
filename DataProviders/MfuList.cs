using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Power8
{
    public static class MfuList
    {
        private static ObservableCollection<PowerItem> _startMfu;
        public static ObservableCollection<PowerItem> StartMfu
        {
            get
            {
                if(_startMfu == null)
                {
                    _startMfu = new ObservableCollection<PowerItem>();
                    UpdateStartMfu();
                }
                return _startMfu;
            }
        }
        
        public static void UpdateStartMfu()
        {
            
        }
    }
}
