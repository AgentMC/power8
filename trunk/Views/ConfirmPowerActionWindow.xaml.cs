using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Power8
{
	/// <summary>
	/// Window to get user confirmation of PowerAction.
	/// </summary>
	public partial class ConfirmPowerActionWindow : Window
	{
		public ConfirmPowerActionWindow(string action)
		{
			this.InitializeComponent();
		    this.ConfirmButton.Content = action;
		    this.MessageTextBlock.Text = string.Format("Are you sure you want to {0}?", action);
		}

        public ConfirmPowerActionWindow(string confirmMessage, string confirmButtonText, string cancelButtonText)
        {
            this.InitializeComponent();
           
            this.ConfirmButton.Content = confirmButtonText;
            this.CancelButton.Content = cancelButtonText;
            this.MessageTextBlock.Text = string.Format(confirmMessage);
        }

        private void ConfirmButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
	}
}