using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;

namespace TheOmniscientChimp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            BitmapImage splashScreenImageSource = new BitmapImage();
            splashScreenImageSource.BeginInit();
            splashScreenImageSource.UriSource = new Uri("SC2_Replay_Monkey.png", UriKind.Relative);
            splashScreenImageSource.EndInit();

            splashScreenImage.Source = splashScreenImageSource;

            //Start the update process asynchronously.
            m_Logic.StartUpdate(this);
        }

        public void AsynchronousExit()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            //Delete temporary folder.
            if (Directory.Exists(m_Logic.m_TemporaryNewVersionFolder))
            {
                Directory.Delete(m_Logic.m_TemporaryNewVersionFolder, true);
            }
            Thread.Sleep(3000);
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Environment.Exit(0);
        }

        UpdaterLogic m_Logic = new UpdaterLogic();
    }
}
