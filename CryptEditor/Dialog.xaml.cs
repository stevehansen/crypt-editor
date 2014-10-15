using System.Windows;

namespace CryptEditor
{
    public partial class Dialog
    {
        public Dialog()
        {
            InitializeComponent();
        }

        public Dialog(string title, string question, string input = "")
            : this()
        {
            Title = title;
            QuestionText = question;
            InputText = input;
        }

        public string QuestionText
        {
            get { return QuestionTextBox.Text; }
            set { QuestionTextBox.Text = value; }
        }

        public string InputText
        {
            get { return InputTextBox.Text; }
            set { InputTextBox.Text = value; }
        }

        private void OkClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static string Prompt(string title, string question, string input = "")
        {
            var dialog = new Dialog(title, question, input);
            return dialog.ShowDialog() == true ? dialog.InputText : null;
        }
    }
}