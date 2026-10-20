using System.Windows;

namespace NovaGames.UI.Views
{
    public partial class InputWindow : Window
    {
        public string InputText { get; private set; } = string.Empty;

        public InputWindow(string promptText, string defaultText = "")
        {
            InitializeComponent();
            txtPrompt.Text = promptText;
            txtInput.Text = defaultText;
            txtInput.SelectAll();
            txtInput.Focus();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            InputText = txtInput.Text.Trim();
            DialogResult = true; // Закриває вікно і повертає true
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Закриває вікно і повертає false
        }
    }
}