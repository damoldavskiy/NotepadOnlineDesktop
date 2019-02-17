﻿using System.ComponentModel;
using System.Windows;

namespace CloudExtension
{
    /// <summary>
    /// Логика взаимодействия для InputPage.xaml
    /// </summary>
    public partial class InputNameWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string text;

        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        public bool Canceled;

        public InputNameWindow()
        {
            InitializeComponent();
            DataContext = this;

            Text = "New file";
        }

        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Canceled = true;
            Close();
        }
    }
}