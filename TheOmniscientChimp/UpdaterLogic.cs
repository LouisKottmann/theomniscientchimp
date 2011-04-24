using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomXaml;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Threading;
using System.Net.NetworkInformation;
using System.Net;
using System.Xml;
using System.ComponentModel;

namespace TheOmniscientChimp
{
    public class UpdaterLogic
    {
        public UpdaterLogic()
        {
            if(!Directory.Exists(m_TemporaryNewVersionFolder))
            {
                Directory.CreateDirectory(m_TemporaryNewVersionFolder);
            }
        }

        public enum LabelStates
        {
            Error,
            InProgress,
            Validated,
        }

        private void StartSc2ReplayMonkey()
        {
            Process newProcess = new Process();
            newProcess.StartInfo = new ProcessStartInfo(Directory.GetCurrentDirectory() + @"\Sc2ReplayMonkey.exe");
            newProcess.Start();

        }

        public void StartUpdate(MainWindow main)
        {
            BackgroundWorker updateWorker = new BackgroundWorker();
            
            updateWorker.DoWork += new DoWorkEventHandler(updateWorker_DoWork);
            updateWorker.WorkerReportsProgress = true;
            updateWorker.ProgressChanged += new ProgressChangedEventHandler(updateWorker_ProgressChanged);
            updateWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(updateWorker_RunWorkerCompleted);
            updateWorker.RunWorkerAsync(main);
        }

        void updateWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker thisWorker = sender as BackgroundWorker;
            MainWindow main = e.Argument as MainWindow;
            e.Result = main;

            thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.InProgress, main.labelConnected));

            if (CheckConnectivity())
            {
                thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.Validated, main.labelConnected));
                thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.InProgress, main.labelNewVersion));

                if (CheckNewVersion())
                {
                    thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.Validated, main.labelNewVersion));
                    thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.InProgress, main.labelDownloading));

                    if (DownloadNewVersion())
                    {
                        thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.Validated, main.labelDownloading));
                        thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.InProgress, main.labelUnpack));

                        if (UnpackNewVersion())
                        {
                            thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.Validated, main.labelUnpack));
                            thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.InProgress, main.labelOverwriting));

                            if (OverWriteOldVersion())
                            {
                                thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.Validated, main.labelOverwriting));
                                thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.InProgress, main.labelStartMainApp));
                                StartSc2ReplayMonkey();
                                return;
                            }
                            else
                            {
                                thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.Error, main.labelOverwriting));
                                thisWorker.ReportProgress(100, new MainWorkerHelper(main, LabelStates.InProgress, main.labelStartMainApp));
                                return;
                            }
                        }
                        else
                        {
                            thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.Error, main.labelUnpack));
                            thisWorker.ReportProgress(100, new MainWorkerHelper(main, LabelStates.InProgress, main.labelStartMainApp));
                            return;
                        }
                    }
                    else
                    {
                        thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.Error, main.labelDownloading));
                        thisWorker.ReportProgress(100, new MainWorkerHelper(main, LabelStates.InProgress, main.labelStartMainApp));
                        return;
                    }
                }
                else
                {
                    thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.Error, main.labelNewVersion));
                    thisWorker.ReportProgress(100, new MainWorkerHelper(main, LabelStates.InProgress, main.labelStartMainApp));
                    return;
                }
            }
            else
            {
                thisWorker.ReportProgress(0, new MainWorkerHelper(main, LabelStates.Error, main.labelConnected));
                thisWorker.ReportProgress(100, new MainWorkerHelper(main, LabelStates.InProgress, main.labelStartMainApp));
                return;
            }
        }

        void updateWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            MainWorkerHelper helper = e.UserState as MainWorkerHelper;
            SetLabelState(helper.Text, helper.LabelState);
        }

        void updateWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MainWindow main = e.Result as MainWindow;
            main.AsynchronousExit();
        }

        private Boolean OverWriteOldVersion()
        {
            try
            {         
                //Update the files by deleting the old ones then copying the new ones.
                foreach (String filePath in Directory.GetFiles(m_TemporaryNewVersionFolder))
                {
                    String oldFilePath = Directory.GetCurrentDirectory() + "\\" + Path.GetFileName(filePath);                    
                    File.Copy(filePath, oldFilePath, true);
                }

                //Updating maps directory.
                String mapsFolder = m_TemporaryNewVersionFolder + @"\SC2 Official Maps";
                if (Directory.Exists(mapsFolder))
                {
                    foreach(String filePath in Directory.GetFiles(mapsFolder))
                    {
                        String oldMapPath = Directory.GetCurrentDirectory() + "\\SC2 Official Maps\\" + Path.GetFileName(filePath);                        
                        File.Copy(filePath, oldMapPath, true);
                    }
                }

                //Updating workers directory.
                String workersFolder = m_TemporaryNewVersionFolder + @"\SC2 Workers";
                if (Directory.Exists(workersFolder))
                {
                    foreach (String filePath in Directory.GetFiles(workersFolder))
                    {
                        String oldWorkerPath = Directory.GetCurrentDirectory() + "\\SC2 Workers\\" + Path.GetFileName(filePath);                        
                        File.Copy(filePath, oldWorkerPath, true);
                    }
                }

                //Updating the current installed version number.
                XmlDocument doc = new XmlDocument();
                doc.Load(m_CurrentVersionXMLPath);
                XmlNode rootNode = doc.SelectSingleNode("Root");
                rootNode.SelectSingleNode("CurrentVersion").InnerXml = m_LatestVersionNumber.ToString();
                doc.Save(m_CurrentVersionXMLPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private Boolean UnpackNewVersion()
        {
            try
            {
                SevenZip.SevenZipExtractor.SetLibraryPath(m_7zLibraryPath);
                SevenZip.SevenZipExtractor extract = new SevenZip.SevenZipExtractor(m_LatestVersionArchivePath);
                extract.ExtractArchive(m_TemporaryNewVersionFolder);

                File.Delete(m_LatestVersionArchivePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SetLabelState(OutlinedText text, LabelStates state)
        {
            switch(state)
            {
                case LabelStates.Error:
                    text.Fill = Brushes.Red;
                    text.Stroke = Brushes.DarkRed;
                    StartSc2ReplayMonkey();
                    break;
                case LabelStates.InProgress:
                    text.Fill = Brushes.Orange;
                    text.Stroke = Brushes.OrangeRed;
                    break;
                case LabelStates.Validated:
                    text.Stroke = Brushes.Green;
                    text.Fill = Brushes.LightGreen;
                    break;
            }
        }

        private Boolean CheckConnectivity()
        {
            Ping pinger = new Ping();
            PingReply reply = pinger.Send("www.google.fr", 1000);

            if (reply.Status != IPStatus.Success)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private Boolean CheckNewVersion()
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(@"http://starcraft2replaymonkey.googlecode.com/files/VersionStatus.xml", m_VersionCheckXMLPath);
            }

            XmlDocument doc = new XmlDocument();            
            doc.Load(m_VersionCheckXMLPath);
            XmlNode rootNode = doc.SelectSingleNode("Root");
            m_LatestVersionNumber = Convert.ToInt32(rootNode.SelectSingleNode("CurrentVersion").InnerXml);
            m_LatestVersionUrl = rootNode.SelectSingleNode("CurrentVersionDownloadUrl").InnerXml;
            File.Delete(m_VersionCheckXMLPath);
            
            doc.Load(m_CurrentVersionXMLPath);
            XmlNode rootNode2 = doc.SelectSingleNode("Root");
            Int32 versionInstalled = Convert.ToInt32(rootNode2.SelectSingleNode("CurrentVersion").InnerXml);

            if (m_LatestVersionNumber > versionInstalled)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private Boolean DownloadNewVersion()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(m_LatestVersionUrl, m_LatestVersionArchivePath);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public String m_TemporaryNewVersionFolder = Directory.GetCurrentDirectory() + @"\Temp";
        String m_VersionCheckXMLPath = Directory.GetCurrentDirectory() + @"\VersionCheck.xml";
        String m_CurrentVersionXMLPath = Directory.GetCurrentDirectory() + @"\CurrentVersion.xml";
        String m_LatestVersionArchivePath = Directory.GetCurrentDirectory() + @"\LatestVersion.rar";        
        String m_LatestVersionUrl = String.Empty;
        String m_7zLibraryPath = Directory.GetCurrentDirectory() + @"\7z.dll";
        Int32 m_LatestVersionNumber = -1;
    }
}
