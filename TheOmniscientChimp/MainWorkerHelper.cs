using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomXaml;

namespace TheOmniscientChimp
{
    public class MainWorkerHelper
    {
        public MainWorkerHelper(MainWindow _Main, TheOmniscientChimp.UpdaterLogic.LabelStates _State, OutlinedText _Text)
        {
            Main = _Main;
            LabelState = _State;
            Text = _Text;
        }

        public MainWindow Main = null;
        public TheOmniscientChimp.UpdaterLogic.LabelStates LabelState = UpdaterLogic.LabelStates.Error;
        public OutlinedText Text = null;
    }
}
