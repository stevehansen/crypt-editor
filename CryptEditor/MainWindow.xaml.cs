using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace CryptEditor
{
    public partial class MainWindow
    {
        // TODO: Remember last window position

        public static readonly DependencyProperty CurrentTextProperty = DependencyProperty.Register("CurrentText", typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty CurrentFileProperty = DependencyProperty.Register("CurrentFile", typeof(File), typeof(MainWindow), new PropertyMetadata(null, OnCurrentFileChanged));

        public static readonly DependencyProperty HasFileProperty = DependencyProperty.Register("HasFile", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        private readonly DirectoryInfo currentDir;

        public MainWindow()
        {
            currentDir = new DirectoryInfo(".");
            Files = new ObservableCollection<File> { File.Null };

            NewCommand = new ActionCommand(OnNew);
            SaveCommand = new ActionCommand(OnSave, () => !string.IsNullOrEmpty(passwordBox.Password) && HasFile);

            InitializeComponent();

            var encryptedPassword = Properties.Settings.Default.EncryptedPassword;
            if (!string.IsNullOrEmpty(encryptedPassword))
            {
                var decryptedPassword = ProtectedData.Unprotect(Convert.FromBase64String(encryptedPassword), null, DataProtectionScope.CurrentUser);
                passwordBox.Password = Encoding.UTF8.GetString(decryptedPassword);
            }

            InputBindings.Add(new KeyBinding(NewCommand, Key.N, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(SaveCommand, Key.S, ModifierKeys.Control));

            foreach (var file in currentDir.GetFiles("*.enc"))
                Files.Add(new File(file));

            passwordBox.PasswordChanged += (_, __) => SaveCommand.TriggerChanged();

            if (Files.Count > 1)
                CurrentFile = Files[1];
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            var encryptedPassword = ProtectedData.Protect(Encoding.UTF8.GetBytes(passwordBox.Password), null, DataProtectionScope.CurrentUser);
            Properties.Settings.Default.EncryptedPassword = Convert.ToBase64String(encryptedPassword);
            Properties.Settings.Default.Save();
        }

        public File CurrentFile
        {
            get { return (File)GetValue(CurrentFileProperty); }
            set { SetValue(CurrentFileProperty, value); }
        }

        public string CurrentText
        {
            get { return (string)GetValue(CurrentTextProperty); }
            set { SetValue(CurrentTextProperty, value); }
        }

        public bool HasFile
        {
            get { return (bool)GetValue(HasFileProperty); }
            set { SetValue(HasFileProperty, value); }
        }

        public ActionCommand NewCommand { get; private set; }

        public ActionCommand SaveCommand { get; private set; }

        public ObservableCollection<File> Files { get; private set; }

        public void OnNew()
        {
            var file = new File("New Document");
            Files.Add(file);
            CurrentFile = file;
        }

        public void OnSave()
        {
            if (CurrentFile == null)
                return;

            if (CurrentFile.FullName == null)
            {
                var fullName = Dialog.Prompt("Save document...", "Enter a document name:", "document");
                if (string.IsNullOrEmpty(fullName))
                    return;

                CurrentFile.FullName = Path.Combine(currentDir.FullName, fullName + ".enc");
                CurrentFile.Name = fullName;
            }

            CurrentFile.Save(passwordBox.Password, CurrentText);
        }

        private static void OnCurrentFileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as MainWindow;
            if (window == null)
                return;

            window.HasFile = false;
            window.Title = "Crypt Editor";
            window.CurrentText = string.Empty;

            var currentFile = window.CurrentFile;
            if (currentFile != File.Null)
            {
                try
                {
                    window.CurrentText = currentFile.Load(window.passwordBox.Password);
                    window.HasFile = true;
                    window.Title = "Crypt Editor - " + currentFile.Name;

                    window.currentTextBox.Focus();
                }
                catch (Exception ex)
                {
                    window.CurrentFile = null;

                    window.Title = "Crypt Editor - Error";
                    window.CurrentText = "Error" + Environment.NewLine + ex.Message;
                }
            }

            window.SaveCommand.TriggerChanged();
        }
    }
}