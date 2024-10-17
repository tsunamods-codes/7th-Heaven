using AppCore;
using CG.Web.MegaApiClient;
using AppUI.Classes;
using AppUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppUI.ViewModels
{
    class MegaLinkGenViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _folderIDInput;
        private string _linkOutput;
        private bool _isGenerating;

        public string FolderIDInput
        {
            get
            {
                return _folderIDInput;
            }
            set
            {
                _folderIDInput = value;
                NotifyPropertyChanged();
            }
        }

        public string LinkOutput
        {
            get
            {
                return _linkOutput;
            }
            set
            {
                _linkOutput = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsGenerating
        {
            get
            {
                return _isGenerating;
            }
            set
            {
                _isGenerating = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotGenerating));
            }
        }

        public bool IsNotGenerating
        {
            get
            {
                return !_isGenerating;
            }
        }

        public MegaLinkGenViewModel()
        {
            FolderIDInput = "";
            LinkOutput = "";
        }

        public Task GenerateLinksAsync()
        {
            IsGenerating = true;
            LinkOutput = ResourceHelper.Get(StringKey.GeneratingLinks);

            Task t = Task.Factory.StartNew(() =>
            {
                GenerateLinks();
            });

            t.ContinueWith((result) =>
            {
                IsGenerating = false;

                if (result.IsFaulted)
                {
                    LinkOutput = $"{ResourceHelper.Get(StringKey.FailedToGenerateLinks)}: {result.Exception.GetBaseException()?.Message}.";
                    Logger.Warn(result.Exception.GetBaseException());
                }
            });

            return t;
        }

        public void GenerateLinks()
        {
            string megaFolderId = FolderIDInput;

            if (string.IsNullOrWhiteSpace(megaFolderId))
            {
                LinkOutput = ResourceHelper.Get(StringKey.EnterMegaFolderIdToGenerateLinks);
                return;
            }

            MegaApiClient client = new MegaApiClient();

            client.LoginAnonymous();

            IEnumerable<INode> nodes = client.GetNodesFromLink(new Uri($"https://mega.nz/{megaFolderId}"));

            if (nodes?.Any() == false)
            {
                LinkOutput = $"{ResourceHelper.Get(StringKey.NoLinksFoundInFolder)}: {megaFolderId}";
                client.Logout();
                return;
            }

            LinkOutput = "";
            foreach (INode node in nodes.Where(x => x.Type == NodeType.File))
            {
                LinkOutput += String.Format("iros://MegaSharedFolder/{0},{1},{2}\r\n", megaFolderId, node.Id, node.Name);
            }

            client.Logout();
        }
    }
}
