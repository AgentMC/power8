using System;
using System.Drawing;
using System.Windows;
using Power8.Properties;

namespace Power8.Views
{
    /// <summary>
    /// Window to get user confirmation of PowerAction.
    /// </summary>
    public partial class ConfirmActionWindow
    {
        public ConfirmActionWindow(object action)
        {
            InitializeComponent();

            QuestionIcon.Source =
                ImageManager.GetImageContainerForIconSync("Question", SystemIcons.Question.Handle).LargeBitmap;
            Title = NoLoc.Stg_AppShortName;
            CawConfirmButton.Content = action;
        }


        public ConfirmActionWindow(string confirmMessage, string confirmButtonText, string cancelButtonText)
            : this(confirmButtonText)
        {
            CawCancelButton.Content =  cancelButtonText;
            CawMessageTextBlock.Text = confirmMessage;
        }

        private void ConfirmButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ConfirmWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var ceil = Math.Ceiling(LayoutRoot.RowDefinitions[0].ActualHeight);
            ceil += ceil%2; //getting first even number more than ActualHeight of Row
            LayoutRoot.RowDefinitions[0].MinHeight = ceil;
        }
    }
}